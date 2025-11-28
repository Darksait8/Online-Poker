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
    private static AuthServerSync instance;
    
    /// <summary>
    /// Текущий экземпляр синхронизации (если отсутствует в сцене, выполняется поиск)
    /// </summary>
    public static AuthServerSync Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindObjectOfType<AuthServerSync>();
            }
            return instance;
        }
    }
    
    /// <summary>
    /// Гарантирует, что объект AuthServerSync существует в сцене.
    /// При отсутствии создаёт новый GameObject и возвращает ссылку на компонент.
    /// </summary>
    public static AuthServerSync EnsureInstance()
    {
        if (Instance != null)
            return Instance;
        
        var autoObject = new GameObject("AuthServerSync_AutoCreated");
        return autoObject.AddComponent<AuthServerSync>();
    }
    
    [Header("Настройки")]
    [SerializeField] private bool useServerAuth = true; // Использовать серверную авторизацию
    [SerializeField] private string serverHost = "localhost";
    [SerializeField] private int serverPort = 8888;
    
    [Header("Ссылки")]
    [SerializeField] private AuthServerClient authClient;
    
    public bool UseServerAuth => useServerAuth;
    public AuthServerClient Client => authClient;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
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
    
    private void OnDestroy()
    {
        Cleanup();
    }
    
    private void OnApplicationQuit()
    {
        Cleanup();
    }
    
    private void Cleanup()
    {
        if (instance == this)
        {
            instance = null;
        }
        
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
                // Синхронизируем заявки с сервера при старте
                var currentUser = AuthManager.CurrentUser;
                if (currentUser != null)
                {
                    Debug.Log($"AuthServerSync: Синхронизирую заявки для {currentUser.username} при старте");
                    SyncFriendDataFromServer(currentUser.username);
                }
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
                
                // ВАЖНО: Инициализируем списки заявок ПЕРЕД парсингом
                profile.incomingFriendRequests = new List<FriendRequestData>();
                profile.outgoingFriendRequests = new List<FriendRequestData>();
                profile.friends = new List<string>();
                
                profile.StartNewSession();
                
                // Синхронизируем заявки в друзья с сервера
                if (data.ContainsKey("friends"))
                {
                    string friendsStr = data["friends"].ToString();
                    if (!string.IsNullOrEmpty(friendsStr))
                    {
                        profile.friends = new List<string>(friendsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                        Debug.Log($"AuthServerSync: Синхронизировано {profile.friends.Count} друзей");
                    }
                    else
                    {
                        profile.friends = new List<string>();
                    }
                }
                else
                {
                    profile.friends = new List<string>();
                }
                
                if (data.ContainsKey("incoming_requests"))
                {
                    string incomingStr = data["incoming_requests"].ToString();
                    Debug.Log($"AuthServerSync: Получены входящие заявки при логине: '{incomingStr}'");
                    if (!string.IsNullOrEmpty(incomingStr))
                    {
                        profile.incomingFriendRequests = ParseFriendRequests(incomingStr);
                        Debug.Log($"AuthServerSync: Распарсено {profile.incomingFriendRequests?.Count ?? 0} входящих заявок");
                    }
                    else
                    {
                        profile.incomingFriendRequests = new List<FriendRequestData>();
                        Debug.Log("AuthServerSync: Входящие заявки пусты");
                    }
                }
                else
                {
                    profile.incomingFriendRequests = new List<FriendRequestData>();
                    Debug.LogWarning("AuthServerSync: Ключ 'incoming_requests' не найден в данных логина");
                }
                
                if (data.ContainsKey("outgoing_requests"))
                {
                    string outgoingStr = data["outgoing_requests"].ToString();
                    Debug.Log($"AuthServerSync: Получены исходящие заявки при логине: '{outgoingStr}'");
                    if (!string.IsNullOrEmpty(outgoingStr))
                    {
                        profile.outgoingFriendRequests = ParseFriendRequests(outgoingStr);
                        Debug.Log($"AuthServerSync: Распарсено {profile.outgoingFriendRequests?.Count ?? 0} исходящих заявок");
                    }
                    else
                    {
                        profile.outgoingFriendRequests = new List<FriendRequestData>();
                        Debug.Log("AuthServerSync: Исходящие заявки пусты");
                    }
                }
                else
                {
                    profile.outgoingFriendRequests = new List<FriendRequestData>();
                    Debug.LogWarning("AuthServerSync: Ключ 'outgoing_requests' не найден в данных логина");
                }
                
                // Проверяем заявки перед сохранением
                Debug.Log($"AuthServerSync: Перед SetCurrentUser - входящих: {profile.incomingFriendRequests?.Count ?? 0}, исходящих: {profile.outgoingFriendRequests?.Count ?? 0}");
                if (profile.incomingFriendRequests != null && profile.incomingFriendRequests.Count > 0)
                {
                    foreach (var req in profile.incomingFriendRequests)
                    {
                        Debug.Log($"AuthServerSync: Входящая заявка: {req.from} -> {req.to}");
                    }
                }
                
                // ВАЖНО: Убеждаемся, что списки не null перед сохранением
                if (profile.incomingFriendRequests == null)
                    profile.incomingFriendRequests = new List<FriendRequestData>();
                if (profile.outgoingFriendRequests == null)
                    profile.outgoingFriendRequests = new List<FriendRequestData>();
                if (profile.friends == null)
                    profile.friends = new List<string>();
                
                Debug.Log($"AuthServerSync: ФИНАЛЬНАЯ ПРОВЕРКА ПЕРЕД СОХРАНЕНИЕМ:");
                Debug.Log($"  - Входящих заявок: {profile.incomingFriendRequests.Count}");
                foreach (var req in profile.incomingFriendRequests)
                {
                    Debug.Log($"    * {req.from} -> {req.to}");
                }
                Debug.Log($"  - Исходящих заявок: {profile.outgoingFriendRequests.Count}");
                foreach (var req in profile.outgoingFriendRequests)
                {
                    Debug.Log($"    * {req.from} -> {req.to}");
                }
                
                AuthManager.SetCurrentUser(profile);
                UserDataManager.SaveUserProfile(profile);
                
                // Проверяем заявки после SetCurrentUser
                var currentUser = AuthManager.CurrentUser;
                Debug.Log($"AuthServerSync: После SetCurrentUser - входящих: {currentUser?.incomingFriendRequests?.Count ?? 0}, исходящих: {currentUser?.outgoingFriendRequests?.Count ?? 0}");
                
                // Уведомляем UI об обновлении заявок
                AuthManager.NotifySocialChanged();
                Debug.Log("AuthServerSync: Вызван NotifySocialChanged для обновления UI");
                
                // Проверяем заявки через AuthManager
                var incomingFromManager = AuthManager.GetIncomingFriendRequests();
                var outgoingFromManager = AuthManager.GetOutgoingFriendRequests();
                Debug.Log($"AuthServerSync: GetIncomingFriendRequests вернул {incomingFromManager.Count} заявок");
                Debug.Log($"AuthServerSync: GetOutgoingFriendRequests вернул {outgoingFromManager.Count} заявок");
                
                foreach (var req in incomingFromManager)
                {
                    Debug.Log($"AuthServerSync: Входящая заявка из AuthManager: {req.from} -> {req.to}");
                }
                foreach (var req in outgoingFromManager)
                {
                    Debug.Log($"AuthServerSync: Исходящая заявка из AuthManager: {req.from} -> {req.to}");
                }
                
                // Убеждаемся, что соединение остается активным для получения уведомлений
                if (!authClient.IsConnected())
                {
                    Debug.LogWarning("AuthServerSync: Соединение потеряно после логина, переподключаюсь...");
                    authClient.Connect();
                    
                    // Ждем подключения
                    StartCoroutine(WaitAndRegisterForNotifications(profile.username));
                }
                else
                {
                    Debug.Log("AuthServerSync: Соединение активно, регистрирую для получения уведомлений");
                    // Регистрируем соединение на сервере для получения уведомлений
                    RegisterConnectionForNotifications(profile.username);
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
        
        if (authClient == null)
        {
            authClient = GetComponent<AuthServerClient>() ?? gameObject.AddComponent<AuthServerClient>();
        }
        
        if (authClient == null)
        {
            Debug.LogWarning("AuthServerSync: Не удалось получить AuthServerClient для синхронизации профиля");
            return;
        }
        
        if (!authClient.IsConnected())
        {
            authClient.Connect();
        }
        
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
        {
            Debug.Log("AuthServerSync: ParseFriendRequests - строка пуста");
            return requests;
        }
        
        Debug.Log($"AuthServerSync: ParseFriendRequests - парсю строку: '{requestsStr}'");
        
        string[] requestParts = requestsStr.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries);
        Debug.Log($"AuthServerSync: ParseFriendRequests - найдено {requestParts.Length} частей");
        
        foreach (string part in requestParts)
        {
            Debug.Log($"AuthServerSync: ParseFriendRequests - обрабатываю часть: '{part}'");
            string[] fields = part.Split('|');
            Debug.Log($"AuthServerSync: ParseFriendRequests - разделено на {fields.Length} полей");
            
            if (fields.Length >= 2)
            {
                DateTime createdAt = DateTime.Now;
                if (fields.Length >= 3)
                {
                    // Пытаемся распарсить дату в разных форматах
                    string dateStr = fields[2];
                    Debug.Log($"AuthServerSync: ParseFriendRequests - дата: '{dateStr}'");
                    
                    // Убираем временную зону если есть
                    if (dateStr.Contains("+"))
                    {
                        int plusIndex = dateStr.IndexOf("+");
                        dateStr = dateStr.Substring(0, plusIndex);
                        Debug.Log($"AuthServerSync: ParseFriendRequests - дата после удаления временной зоны: '{dateStr}'");
                    }
                    
                    // Пробуем разные форматы
                    if (!DateTime.TryParse(dateStr, out createdAt))
                    {
                        // Пробуем формат yyyy-MM-ddTHH:mm:ss
                        if (!DateTime.TryParseExact(dateStr, "yyyy-MM-ddTHH:mm:ss", null, System.Globalization.DateTimeStyles.None, out createdAt))
                        {
                            Debug.LogWarning($"AuthServerSync: ParseFriendRequests - не удалось распарсить дату '{dateStr}', использую текущую");
                            createdAt = DateTime.Now;
                        }
                    }
                    
                    Debug.Log($"AuthServerSync: ParseFriendRequests - распарсенная дата: {createdAt}");
                }
                else
                {
                    Debug.LogWarning($"AuthServerSync: ParseFriendRequests - нет третьего поля (дата), использую текущую дату");
                }
                
                var request = new FriendRequestData
                {
                    from = fields[0],
                    to = fields[1],
                    createdAtTicks = createdAt.Ticks
                };
                
                requests.Add(request);
                Debug.Log($"AuthServerSync: ParseFriendRequests - добавлена заявка: {request.from} -> {request.to}");
            }
            else
            {
                Debug.LogWarning($"AuthServerSync: ParseFriendRequests - недостаточно полей в части '{part}'");
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
    
    private void RegisterConnectionForNotifications(string username)
    {
        if (authClient == null)
        {
            Debug.LogError("AuthServerSync: authClient == null, не могу зарегистрироваться для уведомлений");
            return;
        }
        
        if (!authClient.IsConnected())
        {
            Debug.LogWarning($"AuthServerSync: authClient не подключен для {username}, пытаюсь подключиться...");
            authClient.Connect();
            
            // Ждем подключения и регистрируемся
            StartCoroutine(WaitAndRegisterForNotifications(username));
            return;
        }
        
        Debug.Log($"AuthServerSync: Отправляю запрос на регистрацию для уведомлений: {username} (соединение активно)");
        authClient.RegisterForNotifications(username);
        Debug.Log($"AuthServerSync: Запрос на регистрацию отправлен для {username}");
    }
    
    private System.Collections.IEnumerator WaitAndRegisterForNotifications(string username)
    {
        float timeout = 5f;
        float elapsed = 0f;
        
        while (!authClient.IsConnected() && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (authClient.IsConnected())
        {
            RegisterConnectionForNotifications(username);
        }
        else
        {
            Debug.LogWarning("AuthServerSync: Не удалось подключиться для регистрации уведомлений");
        }
    }
    
    /// <summary>
    /// Синхронизирует данные о друзьях и заявках с сервера
    /// </summary>
    private void SyncFriendDataFromServer(string username)
    {
        if (!authClient.IsConnected())
        {
            Debug.LogWarning("AuthServerSync: Не могу синхронизировать заявки - нет соединения");
            return;
        }
        
        Debug.Log($"AuthServerSync: Запрашиваю данные о друзьях для {username}");
        
        // Подписываемся на ответ
        System.Action<bool, Dictionary<string, object>> handler = null;
        handler = (success, data) =>
        {
            authClient.OnFriendDataResponse -= handler;
            
            if (success && data != null)
            {
                Debug.Log("AuthServerSync: Получены данные о друзьях с сервера, обновляю профиль");
                HandleFriendDataUpdate(data);
            }
            else
            {
                Debug.LogWarning("AuthServerSync: Не удалось получить данные о друзьях с сервера");
            }
        };
        
        authClient.OnFriendDataResponse += handler;
        authClient.GetFriendData(username);
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

