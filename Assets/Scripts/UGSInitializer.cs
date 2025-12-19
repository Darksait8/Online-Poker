using UnityEngine;

/// <summary>
/// Автоматически создает и инициализирует все UGS менеджеры при старте игры
/// </summary>
public class UGSInitializer : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool createManagersOnStart = true;
    [SerializeField] private bool logInitialization = true;
    
    private static bool hasInitialized = false;
    
    private void Awake()
    {
        // Singleton pattern - только один инициализатор
        if (hasInitialized)
        {
            Destroy(gameObject);
            return;
        }
        
        hasInitialized = true;
        DontDestroyOnLoad(gameObject);
        
        if (createManagersOnStart)
        {
            CreateUGSManagers();
        }
    }
    
    /// <summary>
    /// Создает все необходимые UGS менеджеры
    /// </summary>
    private void CreateUGSManagers()
    {
        // Создаем UGSServiceManager
        if (UGSServiceManager.Instance == null)
        {
            GameObject serviceManagerObj = new GameObject("UGSServiceManager");
            serviceManagerObj.transform.SetParent(transform);
            UGSServiceManager serviceManager = serviceManagerObj.AddComponent<UGSServiceManager>();
            // Настройки уже установлены по умолчанию в классе UGSServiceManager
            // Если нужно изменить, можно использовать SerializeField через Inspector
            
            if (logInitialization)
                Debug.Log("UGSInitializer: Создан UGSServiceManager");
        }
        
        // Создаем UGSManagers GameObject для остальных менеджеров
        GameObject ugsManagersObj = new GameObject("UGSManagers");
        ugsManagersObj.transform.SetParent(transform);
        
        // Добавляем UGSFriendsManager
        if (UGSFriendsManager.Instance == null)
        {
            ugsManagersObj.AddComponent<UGSFriendsManager>();
            if (logInitialization)
                Debug.Log("UGSInitializer: Создан UGSFriendsManager");
        }
        
        // Добавляем UGSLeaderboardManager
        if (UGSLeaderboardManager.Instance == null)
        {
            ugsManagersObj.AddComponent<UGSLeaderboardManager>();
            if (logInitialization)
                Debug.Log("UGSInitializer: Создан UGSLeaderboardManager");
        }
        
        // Добавляем UGSCloudSaveManager
        if (UGSCloudSaveManager.Instance == null)
        {
            ugsManagersObj.AddComponent<UGSCloudSaveManager>();
            if (logInitialization)
                Debug.Log("UGSInitializer: Создан UGSCloudSaveManager");
        }
        
        if (logInitialization)
            Debug.Log("UGSInitializer: Все UGS менеджеры успешно созданы!");
    }
    
    /// <summary>
    /// Публичный метод для ручного создания менеджеров (если нужно)
    /// </summary>
    public void InitializeManagers()
    {
        CreateUGSManagers();
    }
}

