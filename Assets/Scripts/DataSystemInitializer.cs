using UnityEngine;

public class DataSystemInitializer : MonoBehaviour
{
    [Header("Настройки инициализации")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool showDebugInfo = true;
    
    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeDataSystem();
        }
    }
    
    /// <summary>
    /// Инициализирует систему данных
    /// </summary>
    public void InitializeDataSystem()
    {
        try
        {
            // Инициализируем менеджер данных
            UserDataManager.Initialize();
            
            // Инициализируем систему авторизации
            AuthManager.Initialize();
            
            if (showDebugInfo)
            {
                Debug.Log("✅ Data System initialized successfully");
                Debug.Log($"📊 Data Info: {AuthManager.GetDataInfo()}");
                
                if (AuthManager.IsLoggedIn)
                {
                    var user = AuthManager.CurrentUser;
                    Debug.Log($"👤 User logged in: {user.username}");
                    Debug.Log($"💰 Chips: {user.chips}");
                    Debug.Log($"🎮 Games played: {user.totalGamesPlayed}");
                }
                else
                {
                    Debug.Log("👤 No user logged in");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to initialize data system: {e.Message}");
        }
    }
    
    /// <summary>
    /// Сохраняет все данные пользователя
    /// </summary>
    public void SaveAllData()
    {
        AuthManager.SaveCurrentUser();
        Debug.Log("💾 All user data saved");
    }
    
    /// <summary>
    /// Очищает все данные (для тестирования)
    /// </summary>
    public void ClearAllData()
    {
        AuthManager.ClearAllUserData();
        Debug.Log("🗑️ All user data cleared");
    }
    
    /// <summary>
    /// Показывает информацию о системе данных
    /// </summary>
    public void ShowDataInfo()
    {
        Debug.Log($"📊 Data System Info: {AuthManager.GetDataInfo()}");
    }
    
    /// <summary>
    /// Создает тестового пользователя
    /// </summary>
    public void CreateTestUser()
    {
        AuthManager.Register("testuser", "test@example.com", "password123", "password123");
        Debug.Log("🧪 Test user created: testuser / password123");
    }
    
    /// <summary>
    /// Обновляет игровую статистику (для тестирования)
    /// </summary>
    public void UpdateTestGameStats()
    {
        if (AuthManager.IsLoggedIn)
        {
            AuthManager.UpdateGameStats(true, 1000, 0); // Выиграл 1000 фишек
            Debug.Log("🎮 Test game stats updated");
        }
        else
        {
            Debug.LogWarning("⚠️ No user logged in to update stats");
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveAllData();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveAllData();
        }
    }
    
    private void OnDestroy()
    {
        SaveAllData();
    }
}
