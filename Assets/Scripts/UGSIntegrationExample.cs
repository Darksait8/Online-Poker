using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Пример интеграции UGS с существующим AuthManager
/// Показывает, как использовать UGS вместе с локальной системой
/// </summary>
public class UGSIntegrationExample : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool useUGSForFriends = true;
    [SerializeField] private bool useUGSForLeaderboards = true;
    [SerializeField] private bool useUGSForCloudSave = true;
    
    private async void Start()
    {
        // Ждем инициализации UGS
        if (UGSServiceManager.Instance != null)
        {
            await UGSServiceManager.Instance.InitializeAsync();
            
            // Если не авторизован, делаем анонимный вход
            if (!UGSServiceManager.Instance.IsSignedIn)
            {
                await UGSServiceManager.Instance.SignInAnonymousAsync();
            }
            
            // После авторизации можно использовать другие сервисы
            if (useUGSForCloudSave && UGSServiceManager.Instance.IsSignedIn)
            {
                // Загружаем профиль из облака
                var cloudProfile = await UGSCloudSaveManager.Instance.LoadPlayerProfileAsync();
                
                if (cloudProfile != null)
                {
                    // Обновляем локальный профиль данными из облака
                    Debug.Log($"Профиль загружен из облака: {cloudProfile.username}");
                }
            }
            
            if (useUGSForFriends && UGSServiceManager.Instance.IsSignedIn)
            {
                // Обновляем список друзей из UGS
                await UGSFriendsManager.Instance.RefreshFriendsAsync();
            }
        }
    }
    
    /// <summary>
    /// Пример: Сохранение профиля после игры
    /// </summary>
    public async void OnGameEnded(UserProfile profile)
    {
        // Сохраняем локально (как раньше)
        UserDataManager.SaveUserProfile(profile);
        
        // Сохраняем в облако через UGS
        if (useUGSForCloudSave && UGSServiceManager.Instance.IsSignedIn)
        {
            await UGSCloudSaveManager.Instance.SavePlayerProfileAsync(profile);
        }
        
        // Обновляем рейтинг в таблице лидеров
        if (useUGSForLeaderboards && UGSServiceManager.Instance.IsSignedIn)
        {
            await UGSLeaderboardManager.Instance.UpdatePlayerRatingFromProfile(profile);
        }
    }
    
    /// <summary>
    /// Пример: Отправка заявки в друзья через UGS
    /// </summary>
    public async void SendFriendRequestExample(string playerId)
    {
        if (useUGSForFriends && UGSServiceManager.Instance.IsSignedIn)
        {
            bool success = await UGSFriendsManager.Instance.SendFriendRequestAsync(playerId);
            if (success)
            {
                Debug.Log($"Заявка отправлена через UGS: {playerId}");
            }
        }
        else
        {
            // Fallback на локальную систему
            AuthManager.TrySendFriendRequest(playerId, out string error);
        }
    }
}

