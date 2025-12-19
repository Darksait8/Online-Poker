using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

/// <summary>
/// Главный менеджер для инициализации и управления Unity Gaming Services
/// Должен быть добавлен на GameObject в сцене (DontDestroyOnLoad)
/// </summary>
public class UGSServiceManager : MonoBehaviour
{
    public static UGSServiceManager Instance { get; private set; }
    
    [Header("Настройки")]
    [SerializeField] private bool autoInitializeOnStart = true;
    [SerializeField] private bool autoSignInAnonymous = true;
    
    public bool IsInitialized { get; private set; }
    public bool IsSignedIn { get; private set; }
    
    public static event System.Action OnInitialized;
    public static event System.Action OnSignedIn;
    public static event System.Action<string> OnInitializationFailed;
    public static event System.Action<string> OnSignInFailed;
    
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
    
    private async void Start()
    {
        if (autoInitializeOnStart)
        {
            await InitializeAsync();
            
            if (IsInitialized && autoSignInAnonymous)
            {
                await SignInAnonymousAsync();
            }
        }
    }
    
    /// <summary>
    /// Инициализация Unity Gaming Services
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        if (IsInitialized)
        {
            Debug.Log("UGS уже инициализирован");
            return true;
        }
        
        try
        {
            Debug.Log("Инициализация Unity Gaming Services...");
            await UnityServices.InitializeAsync();
            
            IsInitialized = true;
            Debug.Log("Unity Gaming Services успешно инициализирован!");
            OnInitialized?.Invoke();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка инициализации UGS: {e.Message}");
            OnInitializationFailed?.Invoke(e.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Анонимный вход (для быстрого тестирования)
    /// </summary>
    public async Task<bool> SignInAnonymousAsync()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("UGS не инициализирован! Сначала вызовите InitializeAsync()");
            return false;
        }
        
        if (IsSignedIn)
        {
            Debug.Log("Уже авторизован");
            return true;
        }
        
        try
        {
            Debug.Log("Анонимный вход...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            IsSignedIn = true;
            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"Успешный вход! Player ID: {playerId}");
            OnSignedIn?.Invoke();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка входа: {e.Message}");
            OnSignInFailed?.Invoke(e.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Вход с логином и паролем (Unity Player Accounts)
    /// </summary>
    public async Task<bool> SignInWithUsernamePasswordAsync(string username, string password)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("UGS не инициализирован!");
            return false;
        }
        
        try
        {
            // Если уже залогинен (например, анонимно), сначала выходим
            if (IsSignedIn)
            {
                Debug.Log("Выход из текущей сессии перед входом...");
                SignOut();
                // Небольшая задержка для завершения выхода
                await System.Threading.Tasks.Task.Delay(500);
            }
            
            Debug.Log($"Вход с логином: {username}");
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            
            IsSignedIn = true;
            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"Успешный вход! Player ID: {playerId}");
            OnSignedIn?.Invoke();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка входа: {e.Message}");
            OnSignInFailed?.Invoke(e.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public async Task<bool> RegisterWithUsernamePasswordAsync(string username, string password)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("UGS не инициализирован!");
            return false;
        }
        
        try
        {
            // Если уже залогинен (например, анонимно), сначала выходим
            if (IsSignedIn)
            {
                Debug.Log("Выход из текущей сессии перед регистрацией...");
                SignOut();
                // Небольшая задержка для завершения выхода
                await System.Threading.Tasks.Task.Delay(500);
            }
            
            Debug.Log($"Регистрация пользователя: {username}");
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            
            IsSignedIn = true;
            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"Успешная регистрация! Player ID: {playerId}");
            OnSignedIn?.Invoke();
            
            return true;
        }
        catch (System.Exception e)
        {
            // Детальное логирование ошибки
            Debug.LogError($"Ошибка регистрации: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            
            // Определяем тип ошибки для более понятного сообщения
            string errorMessage = e.Message;
            if (e.Message.Contains("already exists") || e.Message.Contains("already registered") || 
                e.Message.Contains("username") && e.Message.Contains("taken"))
            {
                errorMessage = "Пользователь с таким именем уже существует";
            }
            else if (e.Message.Contains("Username does not match requirements"))
            {
                errorMessage = "Имя пользователя не соответствует требованиям. Используйте только буквы, цифры и символы: . - _ @ (от 3 до 20 символов)";
            }
            else if (e.Message.Contains("Password does not match requirements"))
            {
                errorMessage = "Пароль не соответствует требованиям. Пароль должен содержать минимум 8 символов, включая: 1 заглавную букву, 1 строчную букву, 1 цифру и 1 символ";
            }
            else if (e.Message.Contains("invalid") || e.Message.Contains("Invalid"))
            {
                errorMessage = "Некорректные данные. Проверьте логин и пароль.";
            }
            else if (e.Message.Contains("network") || e.Message.Contains("connection"))
            {
                errorMessage = "Ошибка сети. Проверьте интернет-соединение.";
            }
            
            OnSignInFailed?.Invoke(errorMessage);
            return false;
        }
    }
    
    /// <summary>
    /// Выход из системы
    /// </summary>
    public void SignOut()
    {
        if (IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            IsSignedIn = false;
            Debug.Log("Выход выполнен");
        }
    }
    
    /// <summary>
    /// Получить Player ID текущего игрока
    /// </summary>
    public string GetPlayerId()
    {
        try
        {
            if (IsSignedIn && AuthenticationService.Instance != null)
            {
                return AuthenticationService.Instance.PlayerId;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Ошибка получения PlayerId: {e.Message}");
        }
        return "";
    }
}

