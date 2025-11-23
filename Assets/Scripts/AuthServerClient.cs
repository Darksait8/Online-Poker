using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Клиент для работы с сервером авторизации
/// Синхронизирует аккаунты через сервер
/// </summary>
public class AuthServerClient : MonoBehaviour
{
    [Header("Настройки подключения")]
    [SerializeField] private string serverHost = "localhost";
    [SerializeField] private int serverPort = 8888;
    
    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;
    
    // Очередь действий для выполнения в главном потоке
    private Queue<System.Action> mainThreadQueue = new Queue<System.Action>();
    private readonly object queueLock = new object();
    
    // События
    public System.Action<bool, string, Dictionary<string, object>> OnRegisterResponse;
    public System.Action<bool, string, Dictionary<string, object>> OnLoginResponse;
    public System.Action<bool, Dictionary<string, object>> OnProfileResponse;
    public System.Action<bool, string> OnUpdateResponse;
    public System.Action<bool, List<Dictionary<string, object>>> OnAllUsersResponse;
    public System.Action<Dictionary<string, object>> OnFriendRequestNotification;
    public System.Action<Dictionary<string, object>> OnFriendDataUpdate;
    
    private void Update()
    {
        // Выполняем действия из очереди в главном потоке
        lock (queueLock)
        {
            while (mainThreadQueue.Count > 0)
            {
                var action = mainThreadQueue.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Ошибка выполнения действия в главном потоке: {e.Message}");
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        Disconnect();
    }
    
    /// <summary>
    /// Добавляет действие в очередь для выполнения в главном потоке
    /// </summary>
    private void ExecuteOnMainThread(System.Action action)
    {
        if (action == null) return;
        
        lock (queueLock)
        {
            mainThreadQueue.Enqueue(action);
        }
    }
    
    /// <summary>
    /// Подключение к серверу
    /// </summary>
    public void Connect()
    {
        if (isConnected)
            return;
        
        try
        {
            tcpClient = new TcpClient();
            var connectResult = tcpClient.BeginConnect(serverHost, serverPort, null, null);
            if (!connectResult.AsyncWaitHandle.WaitOne(System.TimeSpan.FromSeconds(5)))
            {
                throw new System.TimeoutException("Превышено время ожидания");
            }
            tcpClient.EndConnect(connectResult);
            
            stream = tcpClient.GetStream();
            isConnected = true;
            
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            if (enableDebugLogs)
                Debug.Log($"✅ Подключен к серверу авторизации {serverHost}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Ошибка подключения к серверу авторизации: {e.Message}");
            isConnected = false;
        }
    }
    
    /// <summary>
    /// Отключение от сервера
    /// </summary>
    public void Disconnect()
    {
        isConnected = false;
        
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        
        try
        {
            stream?.Close();
            tcpClient?.Close();
        }
        catch { }
    }
    
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public void Register(string username, string email, string password)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "auth_register"},
            {"username", username},
            {"email", email},
            {"password", password}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Вход пользователя
    /// </summary>
    public void Login(string username, string password)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "auth_login"},
            {"username", username},
            {"password", password}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Получение профиля пользователя
    /// </summary>
    public void GetProfile(string username)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "auth_get_profile"},
            {"username", username}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Получение списка всех пользователей
    /// </summary>
    public void GetAllUsers()
    {
        if (enableDebugLogs)
            Debug.Log("AuthServerClient: GetAllUsers вызван");
        
        if (!EnsureConnected())
        {
            if (enableDebugLogs)
                Debug.LogWarning("AuthServerClient: Не удалось подключиться для GetAllUsers");
            return;
        }
        
        var message = new Dictionary<string, object>
        {
            {"type", "auth_get_all_users"}
        };
        
        if (enableDebugLogs)
            Debug.Log("AuthServerClient: Отправляю запрос auth_get_all_users");
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Обновление профиля пользователя
    /// </summary>
    public void UpdateProfile(string username, int? chips = null, int? xp = null, int? level = null)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "auth_update_profile"},
            {"username", username}
        };
        
        if (chips.HasValue)
            message["chips"] = chips.Value;
        if (xp.HasValue)
            message["xp"] = xp.Value;
        if (level.HasValue)
            message["level"] = level.Value;
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Отправка заявки в друзья
    /// </summary>
    public void SendFriendRequest(string fromUsername, string toUsername)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "friend_send_request"},
            {"from", fromUsername},
            {"to", toUsername}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Принятие заявки в друзья
    /// </summary>
    public void AcceptFriendRequest(string username, string requesterUsername)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "friend_accept_request"},
            {"username", username},
            {"requester", requesterUsername}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Отклонение заявки в друзья
    /// </summary>
    public void DeclineFriendRequest(string username, string requesterUsername)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "friend_decline_request"},
            {"username", username},
            {"requester", requesterUsername}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Отмена отправленной заявки
    /// </summary>
    public void CancelFriendRequest(string fromUsername, string toUsername)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "friend_cancel_request"},
            {"from", fromUsername},
            {"to", toUsername}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Получение данных о друзьях и заявках
    /// </summary>
    public void GetFriendData(string username)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "friend_get_data"},
            {"username", username}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Регистрация соединения для получения уведомлений
    /// </summary>
    public void RegisterForNotifications(string username)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "register_for_notifications"},
            {"username", username}
        };
        
        if (enableDebugLogs)
            Debug.Log($"AuthServerClient: Регистрирую соединение для получения уведомлений: {username}");
        
        SendMessage(message);
    }
    
    private bool EnsureConnected()
    {
        if (!isConnected || tcpClient == null || !tcpClient.Connected)
        {
            Connect();
            return isConnected;
        }
        return true;
    }
    
    private void SendMessage(Dictionary<string, object> message)
    {
        if (!isConnected || stream == null)
        {
            Debug.LogWarning("⚠️ Нет подключения к серверу авторизации");
            return;
        }
        
        try
        {
            string json = SimpleJSONParser.ToJSON(message);
            byte[] data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Ошибка отправки сообщения: {e.Message}");
        }
    }
    
    private void ReceiveMessages()
    {
        byte[] buffer = new byte[4096];
        
        while (isConnected && tcpClient != null && tcpClient.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;
                
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ProcessMessage(message);
            }
            catch (Exception ex)
            {
                if (isConnected)
                {
                    Debug.LogError($"❌ Ошибка получения сообщения: {ex.Message}");
                }
                break;
            }
        }
    }
    
    private void ProcessMessage(string message)
    {
        try
        {
            var data = SimpleJSONParser.FromJSON(message);
            string messageType = data.ContainsKey("type") ? data["type"].ToString() : "";
            
            if (enableDebugLogs)
                Debug.Log($"📨 Получено сообщение от сервера авторизации: {messageType}");
            
            // Сохраняем данные для передачи в главный поток
            Dictionary<string, object> messageData = new Dictionary<string, object>(data);
            
            switch (messageType)
            {
                case "auth_register_response":
                    bool regSuccess = data.ContainsKey("success") && Convert.ToBoolean(data["success"]);
                    string regMessage = data.ContainsKey("message") ? data["message"].ToString() : "";
                    // Выполняем в главном потоке
                    ExecuteOnMainThread(() => {
                        OnRegisterResponse?.Invoke(regSuccess, regMessage, messageData);
                    });
                    break;
                    
                case "auth_login_response":
                    bool loginSuccess = data.ContainsKey("success") && Convert.ToBoolean(data["success"]);
                    string loginMessage = data.ContainsKey("message") ? data["message"].ToString() : "";
                    // Выполняем в главном потоке
                    ExecuteOnMainThread(() => {
                        OnLoginResponse?.Invoke(loginSuccess, loginMessage, messageData);
                    });
                    break;
                    
                case "auth_profile_response":
                    bool profileSuccess = data.ContainsKey("success") && Convert.ToBoolean(data["success"]);
                    // Выполняем в главном потоке
                    ExecuteOnMainThread(() => {
                        OnProfileResponse?.Invoke(profileSuccess, messageData);
                    });
                    break;
                    
                case "auth_update_response":
                    bool updateSuccess = data.ContainsKey("success") && Convert.ToBoolean(data["success"]);
                    string updateMessage = data.ContainsKey("message") ? data["message"].ToString() : "";
                    // Выполняем в главном потоке
                    ExecuteOnMainThread(() => {
                        OnUpdateResponse?.Invoke(updateSuccess, updateMessage);
                    });
                    break;
                    
                case "friend_request_notification":
                    // Уведомление о новой заявке в друзья
                    if (enableDebugLogs)
                        Debug.Log("AuthServerClient: Получено уведомление friend_request_notification");
                    ExecuteOnMainThread(() => {
                        if (enableDebugLogs)
                            Debug.Log("AuthServerClient: Вызываю OnFriendRequestNotification");
                        OnFriendRequestNotification?.Invoke(messageData);
                    });
                    break;
                    
                case "friend_data_update":
                    // Обновление данных о друзьях
                    if (enableDebugLogs)
                        Debug.Log("AuthServerClient: Получено обновление friend_data_update");
                    ExecuteOnMainThread(() => {
                        if (enableDebugLogs)
                            Debug.Log("AuthServerClient: Вызываю OnFriendDataUpdate");
                        OnFriendDataUpdate?.Invoke(messageData);
                    });
                    break;
                    
                case "auth_all_users_response":
                    if (enableDebugLogs)
                        Debug.Log("AuthServerClient: Получен ответ auth_all_users_response");
                    
                    bool allUsersSuccess = data.ContainsKey("success") && Convert.ToBoolean(data["success"]);
                    List<Dictionary<string, object>> usersList = new List<Dictionary<string, object>>();
                    
                    if (enableDebugLogs)
                        Debug.Log($"AuthServerClient: Success: {allUsersSuccess}, Has users key: {data.ContainsKey("users")}");
                    
                    if (allUsersSuccess && data.ContainsKey("users"))
                    {
                        var usersData = data["users"];
                        
                        if (enableDebugLogs)
                            Debug.Log($"AuthServerClient: Users data type: {usersData?.GetType().Name}, Value: {usersData}");
                        
                        // Если это строка с разделителем |||
                        if (usersData is string usersString && !string.IsNullOrEmpty(usersString))
                        {
                            if (enableDebugLogs)
                                Debug.Log($"AuthServerClient: Парсинг строки пользователей, длина: {usersString.Length}");
                            
                            string[] userJsonStrings = usersString.Split(new[] { "|||" }, StringSplitOptions.None);
                            if (enableDebugLogs)
                                Debug.Log($"AuthServerClient: Разделено на {userJsonStrings.Length} пользователей");
                            
                            foreach (string userJson in userJsonStrings)
                            {
                                try
                                {
                                    if (enableDebugLogs)
                                        Debug.Log($"AuthServerClient: Парсинг пользователя: {userJson.Substring(0, Math.Min(50, userJson.Length))}...");
                                    
                                    var userDict = SimpleJSONParser.FromJSON(userJson);
                                    usersList.Add(userDict);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning($"AuthServerClient: Ошибка парсинга пользователя: {ex.Message}, JSON: {userJson.Substring(0, Math.Min(100, userJson.Length))}");
                                }
                            }
                        }
                        // Если это массив объектов (старый формат)
                        else if (usersData is List<object> usersListObj)
                        {
                            if (enableDebugLogs)
                                Debug.Log($"AuthServerClient: Парсинг массива объектов, количество: {usersListObj.Count}");
                            
                            foreach (var userObj in usersListObj)
                            {
                                if (userObj is Dictionary<string, object> userDict)
                                {
                                    usersList.Add(userDict);
                                }
                            }
                        }
                    }
                    
                    if (enableDebugLogs)
                        Debug.Log($"AuthServerClient: Обработано {usersList.Count} пользователей");
                    
                    // Выполняем в главном потоке
                    ExecuteOnMainThread(() => {
                        OnAllUsersResponse?.Invoke(allUsersSuccess, usersList);
                    });
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Ошибка обработки сообщения: {e.Message}");
        }
    }
    
    public void SetServerAddress(string host, int port)
    {
        serverHost = host;
        serverPort = port;
    }
    
    public bool IsConnected()
    {
        return isConnected && tcpClient != null && tcpClient.Connected;
    }
}

