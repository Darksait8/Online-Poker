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
        
        // Пытаемся получить адрес сервера из ConnectionManager или PlayerPrefs
        TryLoadServerAddress();
        
        authClient.SetServerAddress(serverHost, serverPort);
        
        // Подписываемся на события
        authClient.OnRegisterResponse += HandleRegisterResponse;
        authClient.OnLoginResponse += HandleLoginResponse;
        authClient.OnProfileResponse += HandleProfileResponse;
        authClient.OnUpdateResponse += HandleUpdateResponse;
        authClient.OnFriendRequestNotification += HandleFriendRequestNotification;
        authClient.OnFriendDataUpdate += HandleFriendDataUpdate;
    }
    
    private void Start()
    {
        // Не подключаемся автоматически - подключение произойдет при необходимости
        // Адрес сервера будет синхронизирован через ConnectionManager
        
        // Если пользователь уже авторизован, подключаемся для получения уведомлений
        if (useServerAuth && AuthManager.IsLoggedIn && AuthManager.CurrentUser != null)
        {
            StartCoroutine(ConnectForNotifications());
        }
    }
    
    private System.Collections.IEnumerator ConnectForNotifications()
    {
        // Ждем немного, чтобы все компоненты инициализировались
        yield return new WaitForSeconds(1f);
        
        if (!authClient.IsConnected())
        {
            Debug.Log("AuthServerSync: Подключаюсь к серверу для получения уведомлений...");
            authClient.Connect();
            
            // Ждем подключения
            float timeout = 5f;
            float elapsed = 0f;
            while (!authClient.IsConnected() && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (authClient.IsConnected())
            {
                Debug.Log("AuthServerSync: Подключен для получения уведомлений");
            }
            else
            {
                Debug.LogWarning("AuthServerSync: Не удалось подключиться для получения уведомлений");
            }
        }
    }
    
    /// <summary>
    /// Пытается загрузить адрес сервера из ConnectionManager или PlayerPrefs
    /// </summary>
    private void TryLoadServerAddress()
    {
        // Пытаемся найти ConnectionManager и получить адрес оттуда
        ConnectionManager connManager = FindObjectOfType<ConnectionManager>();
        if (connManager != null)
        {
            string host = connManager.GetServerHost();
            int port = connManager.GetServerPort();
            if (!string.IsNullOrEmpty(host) && host != "localhost")
            {
                serverHost = host;
                serverPort = port;
                Debug.Log($"✅ Адрес сервера авторизации загружен из ConnectionManager: {host}:{port}");
                return;
            }
        }
        
        // Если не нашли, пытаемся загрузить из PlayerPrefs
        string savedHost = PlayerPrefs.GetString("ServerHost", "");
        int savedPort = PlayerPrefs.GetInt("ServerPort", 8888);
        if (!string.IsNullOrEmpty(savedHost))
        {
            serverHost = savedHost;
            serverPort = savedPort;
            Debug.Log($"✅ Адрес сервера авторизации загружен из PlayerPrefs: {savedHost}:{savedPort}");
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
        authClient.OnFriendRequestNotification -= HandleFriendRequestNotification;
        authClient.OnFriendDataUpdate -= HandleFriendDataUpdate;
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
        
        // Убеждаемся, что подключены к серверу
        if (!authClient.IsConnected())
        {
            authClient.Connect();
            // Ждем подключения асинхронно
            StartCoroutine(WaitForConnectionAndRegister(username, email, password, confirmPassword, callback));
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
        
        // Убеждаемся, что подключены к серверу
        if (!authClient.IsConnected())
        {
            authClient.Connect();
            // Ждем подключения асинхронно
            StartCoroutine(WaitForConnectionAndLogin(username, password, callback));
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
                
                // Синхронизируем заявки в друзья с сервера
                if (data.ContainsKey("friends"))
                {
                    string friendsStr = data["friends"].ToString();
                    if (!string.IsNullOrEmpty(friendsStr))
                    {
                        profile.friends = new List<string>(friendsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                
                if (data.ContainsKey("incoming_requests"))
                {
                    string incomingStr = data["incoming_requests"].ToString();
                    if (!string.IsNullOrEmpty(incomingStr))
                    {
                        profile.incomingFriendRequests = ParseFriendRequests(incomingStr);
                    }
                }
                
                if (data.ContainsKey("outgoing_requests"))
                {
                    string outgoingStr = data["outgoing_requests"].ToString();
                    if (!string.IsNullOrEmpty(outgoingStr))
                    {
                        profile.outgoingFriendRequests = ParseFriendRequests(outgoingStr);
                    }
                }
                
                AuthManager.SetCurrentUser(profile);
                UserDataManager.SaveUserProfile(profile);
                
                // Убеждаемся, что соединение остается активным для получения уведомлений
                if (!authClient.IsConnected())
                {
                    Debug.LogWarning("AuthServerSync: Соединение потеряно после логина, переподключаюсь...");
                    authClient.Connect();
                }
                else
                {
                    Debug.Log("AuthServerSync: Соединение активно, готов к получению уведомлений");
                }
                
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
                
                // Синхронизируем заявки в друзья с сервера
                if (data.ContainsKey("friends"))
                {
                    string friendsStr = data["friends"].ToString();
                    if (!string.IsNullOrEmpty(friendsStr))
                    {
                        profile.friends = new List<string>(friendsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                
                if (data.ContainsKey("incoming_requests"))
                {
                    string incomingStr = data["incoming_requests"].ToString();
                    if (!string.IsNullOrEmpty(incomingStr))
                    {
                        profile.incomingFriendRequests = ParseFriendRequests(incomingStr);
                    }
                }
                
                if (data.ContainsKey("outgoing_requests"))
                {
                    string outgoingStr = data["outgoing_requests"].ToString();
                    if (!string.IsNullOrEmpty(outgoingStr))
                    {
                        profile.outgoingFriendRequests = ParseFriendRequests(outgoingStr);
                    }
                }
                
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
    
    private List<FriendRequestData> ParseFriendRequests(string requestsStr)
    {
        var requests = new List<FriendRequestData>();
        if (string.IsNullOrEmpty(requestsStr))
            return requests;
        
        string[] requestParts = requestsStr.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in requestParts)
        {
            string[] fields = part.Split('|');
            if (fields.Length >= 2)
            {
                DateTime createdAt = DateTime.Now;
                if (fields.Length >= 3)
                {
                    // Пытаемся распарсить дату в разных форматах
                    string dateStr = fields[2];
                    // Убираем временную зону если есть
                    if (dateStr.Contains("+"))
                    {
                        dateStr = dateStr.Substring(0, dateStr.IndexOf("+"));
                    }
                    if (!DateTime.TryParse(dateStr, out createdAt))
                    {
                        createdAt = DateTime.Now;
                    }
                }
                
                requests.Add(new FriendRequestData
                {
                    from = fields[0],
                    to = fields[1],
                    createdAtTicks = createdAt.Ticks
                });
            }
        }
        
        Debug.Log($"AuthServerSync: Распарсено {requests.Count} заявок из строки: {requestsStr}");
        return requests;
    }
    
    private void HandleFriendRequestNotification(Dictionary<string, object> data)
    {
        Debug.Log("AuthServerSync: Получено уведомление о новой заявке в друзья");
        
        var currentUser = AuthManager.CurrentUser;
        if (currentUser == null)
            return;
        
        // Обновляем заявки
        if (data.ContainsKey("incoming_requests"))
        {
            string incomingStr = data["incoming_requests"].ToString();
            if (!string.IsNullOrEmpty(incomingStr))
            {
                currentUser.incomingFriendRequests = ParseFriendRequests(incomingStr);
            }
        }
        
        if (data.ContainsKey("outgoing_requests"))
        {
            string outgoingStr = data["outgoing_requests"].ToString();
            if (!string.IsNullOrEmpty(outgoingStr))
            {
                currentUser.outgoingFriendRequests = ParseFriendRequests(outgoingStr);
            }
        }
        
        if (data.ContainsKey("friends"))
        {
            string friendsStr = data["friends"].ToString();
            if (!string.IsNullOrEmpty(friendsStr))
            {
                currentUser.friends = new List<string>(friendsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }
        
        // Сохраняем обновленный профиль
        UserDataManager.SaveUserProfile(currentUser);
        AuthManager.NotifySocialChanged();
        
        Debug.Log($"AuthServerSync: Заявки обновлены. Входящих: {currentUser.incomingFriendRequests?.Count ?? 0}");
    }
    
    private void HandleFriendDataUpdate(Dictionary<string, object> data)
    {
        Debug.Log("AuthServerSync: Получено обновление данных о друзьях");
        
        var currentUser = AuthManager.CurrentUser;
        if (currentUser == null)
            return;
        
        // Обновляем данные
        if (data.ContainsKey("friends"))
        {
            string friendsStr = data["friends"].ToString();
            if (!string.IsNullOrEmpty(friendsStr))
            {
                currentUser.friends = new List<string>(friendsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }
        
        if (data.ContainsKey("incoming_requests"))
        {
            string incomingStr = data["incoming_requests"].ToString();
            if (!string.IsNullOrEmpty(incomingStr))
            {
                currentUser.incomingFriendRequests = ParseFriendRequests(incomingStr);
            }
        }
        
        if (data.ContainsKey("outgoing_requests"))
        {
            string outgoingStr = data["outgoing_requests"].ToString();
            if (!string.IsNullOrEmpty(outgoingStr))
            {
                currentUser.outgoingFriendRequests = ParseFriendRequests(outgoingStr);
            }
        }
        
        // Сохраняем обновленный профиль
        UserDataManager.SaveUserProfile(currentUser);
        AuthManager.NotifySocialChanged();
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
    
    /// <summary>
    /// Корутина для ожидания подключения и регистрации
    /// </summary>
    private System.Collections.IEnumerator WaitForConnectionAndRegister(string username, string email, 
        string password, string confirmPassword, System.Action<bool, string> callback)
    {
        float timeout = 5f;
        float elapsed = 0f;
        
        while (!authClient.IsConnected() && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (!authClient.IsConnected())
        {
            callback?.Invoke(false, "Не удалось подключиться к серверу авторизации");
            yield break;
        }
        
        // Теперь выполняем регистрацию (но избегаем рекурсии)
        // Вызываем внутреннюю логику регистрации напрямую
        System.Action<bool, string, Dictionary<string, object>> registerHandler = null;
        registerHandler = (success, message, data) =>
        {
            authClient.OnRegisterResponse -= registerHandler;
            
            if (success)
            {
                var profile = new UserProfile
                {
                    username = data.ContainsKey("username") ? data["username"].ToString() : username,
                    email = email,
                    passwordHash = "",
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
    /// Корутина для ожидания подключения и входа
    /// </summary>
    private System.Collections.IEnumerator WaitForConnectionAndLogin(string username, string password, 
        System.Action<bool, string> callback)
    {
        float timeout = 5f;
        float elapsed = 0f;
        
        while (!authClient.IsConnected() && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (!authClient.IsConnected())
        {
            callback?.Invoke(false, "Не удалось подключиться к серверу авторизации");
            yield break;
        }
        
        // Теперь выполняем вход
        System.Action<bool, string, Dictionary<string, object>> loginHandler = null;
        loginHandler = (success, message, data) =>
        {
            authClient.OnLoginResponse -= loginHandler;
            
            if (success)
            {
                var profile = new UserProfile
                {
                    username = data.ContainsKey("username") ? data["username"].ToString() : username,
                    email = data.ContainsKey("email") ? data["email"].ToString() : "",
                    passwordHash = "",
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
}

