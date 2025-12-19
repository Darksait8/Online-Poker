using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

/// <summary>
/// Менеджер для сохранения и загрузки данных игроков в облако через Unity Gaming Services Cloud Save
/// </summary>
public class UGSCloudSaveManager : MonoBehaviour
{
    public static UGSCloudSaveManager Instance { get; private set; }
    
    public static event System.Action OnSaveCompleted;
    public static event System.Action<string> OnSaveFailed;
    public static event System.Action OnLoadCompleted;
    public static event System.Action<string> OnLoadFailed;
    
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
    /// Сохранить профиль игрока в облако
    /// </summary>
    public async Task<bool> SavePlayerProfileAsync(UserProfile profile)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return false;
        }
        
        if (profile == null)
        {
            Debug.LogWarning("Профиль пуст!");
            return false;
        }
        
        try
        {
            var data = new Dictionary<string, object>
            {
                // Основная информация
                { "username", profile.username ?? "" },
                { "email", profile.email ?? "" },
                
                // Игровые данные
                { "chips", profile.chips },
                { "xp", profile.XP },
                { "level", profile.Level },
                
                // Статистика игр
                { "totalGamesPlayed", profile.totalGamesPlayed },
                { "gamesWon", profile.gamesWon },
                { "gamesLost", profile.gamesLost },
                { "totalWinnings", profile.totalWinnings },
                { "totalLosses", profile.totalLosses },
                { "biggestWin", profile.biggestWin },
                { "biggestLoss", profile.biggestLoss },
                { "winRate", profile.winRate },
                
                // Статистика рук
                { "handsPlayed", profile.handsPlayed },
                { "handsWon", profile.handsWon },
                { "handsLost", profile.handsLost },
                { "handsFolded", profile.handsFolded },
                { "handsRaised", profile.handsRaised },
                { "handsCalled", profile.handsCalled },
                { "handsChecked", profile.handsChecked },
                
                // Даты
                { "registrationDateTicks", profile.registrationDate.Ticks },
                { "lastLoginDateTicks", profile.lastLoginDate.Ticks },
                
                // Аватар
                { "avatarId", profile.avatarId ?? "default" },
                
                // Достижения (сериализуем как JSON строку)
                { "achievements", string.Join(",", profile.achievements ?? new List<string>()) },
                { "unlockedAvatars", string.Join(",", profile.unlockedAvatars ?? new List<string>()) }
            };
            
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            
            Debug.Log("Профиль сохранен в облако!");
            OnSaveCompleted?.Invoke();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка сохранения профиля: {e.Message}");
            OnSaveFailed?.Invoke(e.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Загрузить профиль игрока из облака
    /// </summary>
    public async Task<UserProfile> LoadPlayerProfileAsync()
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return null;
        }
        
        try
        {
            Dictionary<string, Item> savedDataItems = null;
            string loadMethodUsed = "неизвестно";
            
            // Пробуем прямой вызов LoadAllAsync (если доступен)
            try
            {
                var loadAllTask = CloudSaveService.Instance.Data.Player.LoadAllAsync();
                savedDataItems = await loadAllTask;
                loadMethodUsed = "LoadAllAsync (прямой)";
                Debug.Log($"UGSCloudSaveManager: Загрузка через {loadMethodUsed} - получено {savedDataItems?.Count ?? 0} ключей");
            }
            catch (System.Exception ex1)
            {
                Debug.Log($"UGSCloudSaveManager: LoadAllAsync (прямой) не доступен: {ex1.Message}. Пробуем LoadAsync...");
                
                // Fallback на LoadAsync с пустым HashSet (загружает все)
                try
                {
                    var loadTask = CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>());
                    savedDataItems = await loadTask;
                    loadMethodUsed = "LoadAsync (прямой)";
                    Debug.Log($"UGSCloudSaveManager: Загрузка через {loadMethodUsed} - получено {savedDataItems?.Count ?? 0} ключей");
                }
                catch (System.Exception ex2)
                {
                    Debug.LogError($"UGSCloudSaveManager: Не удалось загрузить данные из Cloud Save. LoadAllAsync: {ex1.Message}, LoadAsync: {ex2.Message}");
                    return null;
                }
            }
            
            if (savedDataItems == null)
            {
                Debug.LogWarning($"UGSCloudSaveManager: savedDataItems == null после загрузки через {loadMethodUsed}");
                return null;
            }
            
            if (savedDataItems.Count == 0)
            {
                Debug.Log($"UGSCloudSaveManager: savedDataItems пуст (0 ключей) после загрузки через {loadMethodUsed}");
                return null;
            }
            
            Debug.Log($"UGSCloudSaveManager: Успешно загружено {savedDataItems.Count} ключей через {loadMethodUsed}. Ключи: {string.Join(", ", savedDataItems.Keys)}");
            
            // Конвертируем Dictionary<string, Item> в Dictionary<string, object> для удобства
            var savedData = new Dictionary<string, object>();
            foreach (var kvp in savedDataItems)
            {
                savedData[kvp.Key] = kvp.Value;
            }
            
            var profile = new UserProfile();
            
            // Вспомогательная функция для получения значений из CloudSave
            T GetValue<T>(object value)
            {
                if (value == null) return default(T);
                
                // Если value это Item из Cloud Save, используем свойство Value
                if (value is Item item)
                {
                    try
                    {
                        var itemValue = item.Value;
                        if (itemValue == null) return default(T);
                        
                        // Прямое приведение типов
                        if (itemValue is T directItemValue)
                            return directItemValue;
                        
                        // Попытка конвертации
                        return (T)System.Convert.ChangeType(itemValue, typeof(T));
                    }
                    catch
                    {
                        // Если прямое приведение не работает, пробуем через рефлексию получить Value
                        try
                        {
                            var valueProperty = typeof(Item).GetProperty("Value");
                            if (valueProperty != null)
                            {
                                var itemValue = valueProperty.GetValue(item);
                                if (itemValue is T directReflectedValue)
                                    return directReflectedValue;
                                return (T)System.Convert.ChangeType(itemValue, typeof(T));
                            }
                        }
                        catch
                        {
                            return default(T);
                        }
                    }
                }
                
                // Fallback: прямое приведение типов
                if (value is T directValue)
                    return directValue;
                
                // Попытка конвертации
                try
                {
                    return (T)System.Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default(T);
                }
            }
            
            // Основная информация
            if (savedData.TryGetValue("username", out var username))
                profile.username = GetValue<string>(username);
            
            if (savedData.TryGetValue("email", out var email))
                profile.email = GetValue<string>(email);
            
            // Игровые данные
            if (savedData.TryGetValue("chips", out var chips))
                profile.chips = GetValue<int>(chips);
            
            if (savedData.TryGetValue("xp", out var xp))
                profile.XP = GetValue<int>(xp);
            
            // Статистика игр
            if (savedData.TryGetValue("totalGamesPlayed", out var totalGames))
                profile.totalGamesPlayed = GetValue<int>(totalGames);
            
            if (savedData.TryGetValue("gamesWon", out var gamesWon))
                profile.gamesWon = GetValue<int>(gamesWon);
            
            if (savedData.TryGetValue("gamesLost", out var gamesLost))
                profile.gamesLost = GetValue<int>(gamesLost);
            
            if (savedData.TryGetValue("totalWinnings", out var totalWinnings))
                profile.totalWinnings = GetValue<int>(totalWinnings);
            
            if (savedData.TryGetValue("totalLosses", out var totalLosses))
                profile.totalLosses = GetValue<int>(totalLosses);
            
            if (savedData.TryGetValue("biggestWin", out var biggestWin))
                profile.biggestWin = GetValue<int>(biggestWin);
            
            if (savedData.TryGetValue("biggestLoss", out var biggestLoss))
                profile.biggestLoss = GetValue<int>(biggestLoss);
            
            if (savedData.TryGetValue("winRate", out var winRate))
                profile.winRate = GetValue<float>(winRate);
            
            // Статистика рук
            if (savedData.TryGetValue("handsPlayed", out var handsPlayed))
                profile.handsPlayed = GetValue<int>(handsPlayed);
            
            if (savedData.TryGetValue("handsWon", out var handsWon))
                profile.handsWon = GetValue<int>(handsWon);
            
            if (savedData.TryGetValue("handsLost", out var handsLost))
                profile.handsLost = GetValue<int>(handsLost);
            
            if (savedData.TryGetValue("handsFolded", out var handsFolded))
                profile.handsFolded = GetValue<int>(handsFolded);
            
            if (savedData.TryGetValue("handsRaised", out var handsRaised))
                profile.handsRaised = GetValue<int>(handsRaised);
            
            if (savedData.TryGetValue("handsCalled", out var handsCalled))
                profile.handsCalled = GetValue<int>(handsCalled);
            
            if (savedData.TryGetValue("handsChecked", out var handsChecked))
                profile.handsChecked = GetValue<int>(handsChecked);
            
            // Даты
            if (savedData.TryGetValue("registrationDateTicks", out var regDate))
            {
                long ticks = GetValue<long>(regDate);
                if (ticks > 0)
                    profile.registrationDate = new System.DateTime(ticks);
            }
            
            if (savedData.TryGetValue("lastLoginDateTicks", out var loginDate))
            {
                long ticks = GetValue<long>(loginDate);
                if (ticks > 0)
                    profile.lastLoginDate = new System.DateTime(ticks);
            }
            
            // Аватар
            if (savedData.TryGetValue("avatarId", out var avatarId))
                profile.avatarId = GetValue<string>(avatarId);
            
            // Достижения
            if (savedData.TryGetValue("achievements", out var achievements))
            {
                string achievementsStr = GetValue<string>(achievements);
                if (!string.IsNullOrEmpty(achievementsStr))
                {
                    profile.achievements = new List<string>(achievementsStr.Split(','));
                }
            }
            
            // Разблокированные аватары
            if (savedData.TryGetValue("unlockedAvatars", out var unlockedAvatars))
            {
                string avatarsStr = GetValue<string>(unlockedAvatars);
                if (!string.IsNullOrEmpty(avatarsStr))
                {
                    profile.unlockedAvatars = new List<string>(avatarsStr.Split(','));
                }
            }
            
            profile.isLoggedIn = true;
            
            Debug.Log("Профиль загружен из облака!");
            OnLoadCompleted?.Invoke();
            return profile;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка загрузки профиля: {e.Message}");
            OnLoadFailed?.Invoke(e.Message);
            return null;
        }
    }
    
    /// <summary>
    /// Сохранить конкретное значение в облако
    /// </summary>
    public async Task<bool> SaveValueAsync(string key, object value)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return false;
        }
        
        try
        {
            var data = new Dictionary<string, object> { { key, value } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка сохранения значения: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Загрузить конкретное значение из облака
    /// </summary>
    public async Task<T> LoadValueAsync<T>(string key)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return default(T);
        }
        
        try
        {
            Dictionary<string, Item> savedDataItems = null;
            
            // Пробуем прямой вызов LoadAllAsync
            try
            {
                var loadAllTask = CloudSaveService.Instance.Data.Player.LoadAllAsync();
                savedDataItems = await loadAllTask;
            }
            catch
            {
                // Fallback на LoadAsync
                try
                {
                    var loadTask = CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>());
                    savedDataItems = await loadTask;
                }
                catch
                {
                    Debug.LogWarning("Не удалось загрузить данные из Cloud Save.");
                    return default(T);
                }
            }
            
            if (savedDataItems != null && savedDataItems.TryGetValue(key, out var item))
            {
                if (item == null) return default(T);
                
                // Используем свойство Value из Item
                try
                {
                    var itemValue = item.Value;
                    if (itemValue == null) return default(T);
                    
                    // Прямое приведение типов
                    if (itemValue is T directItemValue)
                        return directItemValue;
                    
                    // Попытка конвертации
                    return (T)System.Convert.ChangeType(itemValue, typeof(T));
                }
                catch
                {
                    // Fallback через рефлексию
                    try
                    {
                        var valueProperty = typeof(Item).GetProperty("Value");
                        if (valueProperty != null)
                        {
                            var itemValue = valueProperty.GetValue(item);
                            if (itemValue is T directReflectedValue)
                                return directReflectedValue;
                            return (T)System.Convert.ChangeType(itemValue, typeof(T));
                        }
                    }
                    catch
                    {
                        return default(T);
                    }
                    return default(T);
                }
            }
            return default(T);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка загрузки значения: {e.Message}");
            return default(T);
        }
    }
}

