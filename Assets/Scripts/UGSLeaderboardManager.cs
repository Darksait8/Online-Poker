using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Менеджер для работы с таблицами лидеров через Unity Gaming Services Leaderboards API
/// </summary>
public class UGSLeaderboardManager : MonoBehaviour
{
    public static UGSLeaderboardManager Instance { get; private set; }
    
    [Header("Настройки")]
    [SerializeField] private string defaultLeaderboardId = "Poker_Leaderboard"; // ID вашей таблицы лидеров из Unity Dashboard
    [SerializeField] private int defaultLimit = 10;
    
    public static event System.Action<List<Unity.Services.Leaderboards.Models.LeaderboardEntry>> OnLeaderboardUpdated;
    public static event System.Action<Unity.Services.Leaderboards.Models.LeaderboardEntry> OnPlayerScoreUpdated;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Добавить очки игрока в таблицу лидеров
    /// </summary>
    public async Task<bool> AddPlayerScoreAsync(string leaderboardId, double score)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return false;
        }
        
        try
        {
            var result = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            Debug.Log($"Очки добавлены в таблицу {leaderboardId}: {score}");
            if (result != null)
                OnPlayerScoreUpdated?.Invoke(result);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка добавления очков: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Добавить очки в таблицу по умолчанию
    /// </summary>
    public async Task<bool> AddPlayerScoreAsync(double score)
    {
        return await AddPlayerScoreAsync(defaultLeaderboardId, score);
    }
    
    /// <summary>
    /// Получить топ игроков из таблицы лидеров
    /// </summary>
    public async Task<List<Unity.Services.Leaderboards.Models.LeaderboardEntry>> GetTopScoresAsync(string leaderboardId, int limit = 10)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return new List<Unity.Services.Leaderboards.Models.LeaderboardEntry>();
        }
        
        try
        {
            var options = new GetScoresOptions
            {
                Limit = limit
            };
            
            var response = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);
            var scores = new List<Unity.Services.Leaderboards.Models.LeaderboardEntry>(response.Results);
            
            OnLeaderboardUpdated?.Invoke(scores);
            return scores;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка получения таблицы лидеров: {e.Message}");
            return new List<Unity.Services.Leaderboards.Models.LeaderboardEntry>();
        }
    }
    
    /// <summary>
    /// Получить топ игроков из таблицы по умолчанию
    /// </summary>
    public async Task<List<Unity.Services.Leaderboards.Models.LeaderboardEntry>> GetTopScoresAsync(int? limit = null)
    {
        int limitToUse = limit ?? defaultLimit;
        return await GetTopScoresAsync(defaultLeaderboardId, limitToUse);
    }
    
    /// <summary>
    /// Получить позицию текущего игрока в таблице лидеров
    /// </summary>
    public async Task<Unity.Services.Leaderboards.Models.LeaderboardEntry> GetPlayerScoreAsync(string leaderboardId)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return default(Unity.Services.Leaderboards.Models.LeaderboardEntry);
        }
        
        try
        {
            var response = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
            return response;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка получения очков игрока: {e.Message}");
            return default(Unity.Services.Leaderboards.Models.LeaderboardEntry);
        }
    }
    
    /// <summary>
    /// Получить позицию текущего игрока из таблицы по умолчанию
    /// </summary>
    public async Task<Unity.Services.Leaderboards.Models.LeaderboardEntry> GetPlayerScoreAsync()
    {
        return await GetPlayerScoreAsync(defaultLeaderboardId);
    }
    
    /// <summary>
    /// Обновить рейтинг игрока на основе его профиля
    /// </summary>
    public async Task<bool> UpdatePlayerRatingFromProfile(UserProfile profile)
    {
        if (profile == null)
            return false;
        
        // Используем XP как рейтинг (можно изменить на другую метрику)
        double rating = profile.XP;
        
        return await AddPlayerScoreAsync(rating);
    }
    
    /// <summary>
    /// Обновить рейтинг игрока на основе чипсов
    /// </summary>
    public async Task<bool> UpdatePlayerRatingFromChips(int chips)
    {
        return await AddPlayerScoreAsync((double)chips);
    }
}

