using UnityEngine;

public class SceneTransitionChecker : MonoBehaviour
{
    [Header("Проверка SceneTransitionManager")]
    [SerializeField] private bool checkOnStart = true;
    [SerializeField] private bool autoCreateIfMissing = true;
    
    private void Start()
    {
        if (checkOnStart)
        {
            CheckSceneTransitionManager();
        }
    }
    
    /// <summary>
    /// Проверяет наличие SceneTransitionManager в сцене
    /// </summary>
    public void CheckSceneTransitionManager()
    {
        SceneTransitionManager manager = SceneTransitionManager.Instance;
        
        if (manager == null)
        {
            Debug.LogWarning("⚠️ SceneTransitionManager.Instance is null!");
            
            if (autoCreateIfMissing)
            {
                CreateSceneTransitionManager();
            }
            else
            {
                Debug.LogError("❌ SceneTransitionManager не найден! Создайте GameObject с компонентом SceneTransitionManager.");
            }
        }
        else
        {
            Debug.Log("✅ SceneTransitionManager найден и работает корректно");
        }
    }
    
    /// <summary>
    /// Создает SceneTransitionManager если его нет
    /// </summary>
    private void CreateSceneTransitionManager()
    {
        GameObject managerGO = new GameObject("SceneTransitionManager");
        SceneTransitionManager manager = managerGO.AddComponent<SceneTransitionManager>();
        
        Debug.Log("✅ SceneTransitionManager создан автоматически");
    }
    
    /// <summary>
    /// Тестирует переходы между сценами
    /// </summary>
    public void TestSceneTransitions()
    {
        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log("🧪 Тестирование переходов сцен...");
            
            // Тестируем переход к авторизации
            Debug.Log("Тест: переход к авторизации");
            SceneTransitionManager.Instance.LoadAuthScene();
            
            // Через секунду тестируем переход к игре
            Invoke(nameof(TestGameSceneTransition), 1f);
        }
        else
        {
            Debug.LogError("❌ SceneTransitionManager не найден для тестирования");
        }
    }
    
    private void TestGameSceneTransition()
    {
        Debug.Log("Тест: переход к игровой сцене");
        SceneTransitionManager.Instance.LoadGameScene();
    }
    
    /// <summary>
    /// Показывает информацию о текущей сцене
    /// </summary>
    public void ShowSceneInfo()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"📍 Текущая сцена: {currentScene}");
        
        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log($"🎮 SceneTransitionManager активен");
            Debug.Log($"🔐 Пользователь авторизован: {AuthManager.IsLoggedIn}");
        }
        else
        {
            Debug.LogWarning("⚠️ SceneTransitionManager не найден");
        }
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label("Scene Transition Checker", GUI.skin.label);
        
        if (GUILayout.Button("Check SceneTransitionManager"))
            CheckSceneTransitionManager();
        
        if (GUILayout.Button("Test Scene Transitions"))
            TestSceneTransitions();
        
        if (GUILayout.Button("Show Scene Info"))
            ShowSceneInfo();
        
        GUILayout.EndArea();
    }
}
