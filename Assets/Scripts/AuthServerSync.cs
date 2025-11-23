using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Синхронизация AuthManager с сервером
/// Позволяет использовать глобальные аккаунты
/// </summary>
public class AuthServerSync : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool useServerAuth = true; // Использовать серверную авторизацию
    [SerializeField] private string serverHost = "localhost";
    [SerializeField] private int serverPort = 8888;
    
    [Header("Ссылки")]
    [SerializeField] private AuthServerClient authClient;
    
    private void Awake()
    {
        if (authClient == null)
        {
            authClient = gameObject.AddComponent<AuthServerClient>();
        }
        
        authClient.SetServerAddress(serverHost, serverPort);
        
        // Подписываемся на события
        authClient.OnRegisterResponse += HandleRegisterResponse;
        authClient.OnLoginResponse += HandleLoginResponse;
        authClient.OnProfileResponse += HandleProfileResponse;
        authClient.OnUpdateResponse += HandleUpdateResponse;
    }
    
    private void Start()
    {
        if (useServerAuth)
        {
            // Подключаемся к серверу при старте
            authClient.Connect();
        }
    }
    
    private void OnDestroy()
    {
        if (authClient != null)
        {
            authClient.OnRegisterResponse -= HandleRegisterResponse;
            authClient.OnLoginResponse -= HandleLoginResponse;
            authClient.OnProfileResponse -= HandleProfileResponse;
            authClient.OnUpdateResponse -= HandleUpdateResponse;
        }
    }
    
    /// <summary>
    /// Регистрация через сервер
    /// </summary>
    public void RegisterOnServer(string username, string email, string password, string confirmPassword, 
        System.Action<bool, string> callback)
    {
        if (!useServerAuth)
        {
            // Используем локальную авторизацию
            AuthManager.Register(username, email, password, confirmPassword);
            callback?.Invoke(true, "Регистрация выполнена локально");
            return;
        }
        
        if (password != confirmPassword)
        {
            callback?.Invoke(false, "Пароли не совпадают");
            return;
        }
        
        if (password.Length < 6)
        {
            callback?.Invoke(false, "Пароль должен содержать минимум 6 символов");
            return;
        }
        
        // Временно сохраняем callback
        System.Action<bool, string, Dictionary<string, object>> registerHandler = null;
        registerHandler = (success, message, data) =>
        {
            authClient.OnRegisterResponse -= registerHandler;
            
            if (success)
            {
                // Создаем локальный профиль из данных сервера
                var profile = new UserProfile
                {
                    username = data.ContainsKey("username") ? data["username"].ToString() : username,
                    email = email,
                    passwordHash = "", // Не храним пароль локально
                    registrationDate = DateTime.Now,
                    lastLoginDate = DateTime.Now,
                    isLoggedIn = true,
                    chips = data.ContainsKey("chips") ? Convert.ToInt32(data["chips"]) : 1000,
                    XP = data.ContainsKey("xp") ? Convert.ToInt32(data["xp"]) : 0
                };
                
                profile.StartNewSession();
                AuthManager.SetCurrentUser(profile);
                UserDataManager.SaveUserProfile(profile);
                
                callback?.Invoke(true, message);
            }
            else
            {
                callback?.Invoke(false, message);
            }
        };
        
        authClient.OnRegisterResponse += registerHandler;
        authClient.Register(username, email, password);
    }
    
    /// <summary>
    /// Вход через сервер
    /// </summary>
    public void LoginOnServer(string username, string password, System.Action<bool, string> callback)
    {
        if (!useServerAuth)
        {
            // Используем локальную авторизацию
            AuthManager.Login(username, password);
            callback?.Invoke(true, "Вход выполнен локально");
            return;
        }
        
        // Временно сохраняем callback
        System.Action<bool, string, Dictionary<string, object>> loginHandler = null;
        loginHandler = (success, message, data) =>
        {
            authClient.OnLoginResponse -= loginHandler;
            
            if (success)
            {
                // Создаем локальный профиль из данных сервера
                var profile = new UserProfile
                {
                    username = data.ContainsKey("username") ? data["username"].ToString() : username,
                    email = data.ContainsKey("email") ? data["email"].ToString() : "",
                    passwordHash = "", // Не храним пароль локально
                    registrationDate = DateTime.Parse(data.ContainsKey("registration_date") ? data["registration_date"].ToString() : DateTime.Now.ToString()),
                    lastLoginDate = DateTime.Now,
                    isLoggedIn = true,
                    chips = data.ContainsKey("chips") ? Convert.ToInt32(data["chips"]) : 1000,
                    XP = data.ContainsKey("xp") ? Convert.ToInt32(data["xp"]) : 0
                };
                
                profile.StartNewSession();
                AuthManager.SetCurrentUser(profile);
                UserDataManager.SaveUserProfile(profile);
                
                callback?.Invoke(true, message);
            }
            else
            {
                callback?.Invoke(false, message);
            }
        };
        
        authClient.OnLoginResponse += loginHandler;
        authClient.Login(username, password);
    }
    
    /// <summary>
    /// Синхронизация профиля с сервером
    /// </summary>
    public void SyncProfileToServer(UserProfile profile)
    {
        if (!useServerAuth || profile == null)
            return;
        
        authClient.UpdateProfile(profile.username, profile.chips, profile.XP, profile.Level);
    }
    
    /// <summary>
    /// Загрузка профиля с сервера
    /// </summary>
    public void LoadProfileFromServer(string username, System.Action<UserProfile> callback)
    {
        if (!useServerAuth)
        {
            var profile = UserDataManager.LoadUserProfile(username);
            callback?.Invoke(profile);
            return;
        }
        
        System.Action<bool, Dictionary<string, object>> profileHandler = null;
        profileHandler = (success, data) =>
        {
            authClient.OnProfileResponse -= profileHandler;
            
            if (success && data != null)
            {
                var profile = new UserProfile
                {
                    username = data.ContainsKey("username") ? data["username"].ToString() : username,
                    email = data.ContainsKey("email") ? data["email"].ToString() : "",
                    chips = data.ContainsKey("chips") ? Convert.ToInt32(data["chips"]) : 1000,
                    XP = data.ContainsKey("xp") ? Convert.ToInt32(data["xp"]) : 0
                };
                
                callback?.Invoke(profile);
            }
            else
            {
                callback?.Invoke(null);
            }
        };
        
        authClient.OnProfileResponse += profileHandler;
        authClient.GetProfile(username);
    }
    
    private void HandleRegisterResponse(bool success, string message, Dictionary<string, object> data)
    {
        Debug.Log($"Регистрация: {(success ? "Успешно" : "Ошибка")} - {message}");
    }
    
    private void HandleLoginResponse(bool success, string message, Dictionary<string, object> data)
    {
        Debug.Log($"Вход: {(success ? "Успешно" : "Ошибка")} - {message}");
    }
    
    private void HandleProfileResponse(bool success, Dictionary<string, object> data)
    {
        Debug.Log($"Профиль: {(success ? "Загружен" : "Ошибка")}");
    }
    
    private void HandleUpdateResponse(bool success, string message)
    {
        Debug.Log($"Обновление профиля: {(success ? "Успешно" : "Ошибка")} - {message}");
    }
    
    public void SetServerAddress(string host, int port)
    {
        serverHost = host;
        serverPort = port;
        if (authClient != null)
        {
            authClient.SetServerAddress(host, port);
        }
    }
}

