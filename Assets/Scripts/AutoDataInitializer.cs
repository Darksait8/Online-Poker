using UnityEngine;

[System.Serializable]
public class AutoDataInitializer : MonoBehaviour
{
    [Header("Автоматическая инициализация")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool showDebugMessages = true;
    
    private void Awake()
    {
        if (initializeOnAwake)
        {
            InitializeDataSystem();
        }
    }
    
    private void Start()
    {
        if (!initializeOnAwake)
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
            
            if (showDebugMessages)
            {
                Debug.Log("✅ Data System auto-initialized successfully");
                Debug.Log($"📊 Data Info: {AuthManager.GetDataInfo()}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to auto-initialize data system: {e.Message}");
        }
    }
    
    /// <summary>
    /// Принудительная инициализация (можно вызвать из кода)
    /// </summary>
    [ContextMenu("Initialize Data System")]
    public void ForceInitialize()
    {
        InitializeDataSystem();
    }
}
