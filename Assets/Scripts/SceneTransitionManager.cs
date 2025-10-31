using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Названия сцен")]
    [SerializeField] private string authSceneName = "Auth";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Main";
    
    [Header("Настройки")]
    [SerializeField] private bool checkAuthOnStart = true;
    [SerializeField] private float transitionDelay = 0.5f;
    
    public static SceneTransitionManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
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
        
        // Подписываемся на события авторизации
        AuthManager.OnUserLoggedOut += OnUserLoggedOut;
    }
    
    private void Start()
    {
        if (checkAuthOnStart)
        {
            CheckAuthenticationStatus();
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            AuthManager.OnUserLoggedOut -= OnUserLoggedOut;
        }
    }
    
    /// <summary>
    /// Проверяет статус авторизации и перенаправляет на нужную сцену
    /// </summary>
    public void CheckAuthenticationStatus()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (AuthManager.IsLoggedIn)
        {
            // Пользователь авторизован
            if (currentScene == authSceneName)
            {
                // Если мы на сцене авторизации, но пользователь уже авторизован - переходим в меню
                LoadMainMenu();
            }
        }
        else
        {
            // Пользователь не авторизован
            if (currentScene != authSceneName)
            {
                // Если мы не на сцене авторизации, но пользователь не авторизован - переходим к авторизации
                LoadAuthScene();
            }
        }
    }
    
    /// <summary>
    /// Загружает сцену авторизации
    /// </summary>
    public void LoadAuthScene()
    {
        LoadScene(authSceneName);
    }
    
    /// <summary>
    /// Загружает главное меню
    /// </summary>
    public void LoadMainMenu()
    {
        if (!AuthManager.IsLoggedIn)
        {
            Debug.LogWarning("Попытка загрузить главное меню без авторизации. Перенаправляем к авторизации.");
            LoadAuthScene();
            return;
        }
        
        LoadScene(mainMenuSceneName);
    }
    
    /// <summary>
    /// Загружает игровую сцену
    /// </summary>
    public void LoadGameScene()
    {
        if (!AuthManager.IsLoggedIn)
        {
            Debug.LogWarning("Попытка загрузить игровую сцену без авторизации. Перенаправляем к авторизации.");
            LoadAuthScene();
            return;
        }
        
        LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// Загружает указанную сцену с задержкой
    /// </summary>
    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Название сцены не может быть пустым");
            return;
        }
        
        // Сохраняем название сцены для загрузки
        SetSceneToLoad(sceneName);
        
        if (transitionDelay > 0)
        {
            Invoke(nameof(LoadSceneImmediate), transitionDelay);
        }
        else
        {
            LoadSceneImmediate();
        }
    }
    
    /// <summary>
    /// Немедленно загружает сцену (для Invoke)
    /// </summary>
    private void LoadSceneImmediate()
    {
        // Получаем название сцены из текущего контекста
        string sceneName = GetCurrentSceneToLoad();
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    
    private string currentSceneToLoad = "";
    
    /// <summary>
    /// Устанавливает сцену для загрузки
    /// </summary>
    private void SetSceneToLoad(string sceneName)
    {
        currentSceneToLoad = sceneName;
    }
    
    /// <summary>
    /// Получает текущую сцену для загрузки
    /// </summary>
    private string GetCurrentSceneToLoad()
    {
        return currentSceneToLoad;
    }
    
    /// <summary>
    /// Выход из игры
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Выход из игры");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Обработчик выхода пользователя
    /// </summary>
    private void OnUserLoggedOut()
    {
        // При выходе пользователя перенаправляем на сцену авторизации
        LoadAuthScene();
    }
    
    /// <summary>
    /// Получает название текущей сцены
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
    
    /// <summary>
    /// Проверяет, находится ли игрок на сцене авторизации
    /// </summary>
    public bool IsOnAuthScene()
    {
        return GetCurrentSceneName() == authSceneName;
    }
    
    /// <summary>
    /// Проверяет, находится ли игрок на главном меню
    /// </summary>
    public bool IsOnMainMenuScene()
    {
        return GetCurrentSceneName() == mainMenuSceneName;
    }
    
    /// <summary>
    /// Проверяет, находится ли игрок на игровой сцене
    /// </summary>
    public bool IsOnGameScene()
    {
        return GetCurrentSceneName() == gameSceneName;
    }
}
