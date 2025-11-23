using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Unity клиент для подключения к покерному серверу
/// </summary>
public class PokerClient : MonoBehaviour
{
    [Header("Настройки подключения")]
    [SerializeField] private string serverHost = "localhost";
    [SerializeField] private int serverPort = 8888;
    [SerializeField] private string playerName = "Unity Player";
    [SerializeField] private int startingStack = 1000;
    [SerializeField] private bool autoConnectOnStart = false; // Не подключаться автоматически
    
    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;
    private string clientId;
    
    // События для UI
    public System.Action<string> OnConnectionStatusChanged;
    public System.Action<string[]> OnPlayersListUpdated;
    public System.Action<bool> OnHandStateChanged;
    public System.Action<string, string, int> OnPlayerAction;
    public System.Action<string, string> OnHoleCardsReceived; // card1, card2
    public System.Action<string[]> OnCommunityCardsReceived; // массив карт
    public System.Action<Dictionary<string, object>> OnGameStateReceived; // полное состояние игры
    
    private void Start()
    {
        // Подключаемся только если включено автоматическое подключение
        if (autoConnectOnStart)
        {
            ConnectToServer();
        }
    }
    
    private void OnDestroy()
    {
        DisconnectFromServer();
    }
    
    [ContextMenu("Connect to Server")]
    public void ConnectToServer()
    {
        // Проверяем, не подключены ли уже
        if (isConnected && tcpClient != null && tcpClient.Connected)
        {
            Debug.LogWarning("⚠️ Уже подключен к серверу");
            return;
        }
        
        // Отключаемся перед новым подключением
        if (tcpClient != null)
        {
            try
            {
                tcpClient.Close();
            }
            catch { }
        }
        
        if (enableDebugLogs)
            Debug.Log($"🔌 Попытка подключения к {serverHost}:{serverPort}...");
        
        try
        {
            tcpClient = new TcpClient();
            
            // Используем синхронное подключение с таймаутом
            // Это более надежно, чем async для Unity
            var connectResult = tcpClient.BeginConnect(serverHost, serverPort, null, null);
            var success = connectResult.AsyncWaitHandle.WaitOne(System.TimeSpan.FromSeconds(5));
            
            if (!success)
            {
                throw new System.TimeoutException("Превышено время ожидания подключения");
            }
            
            tcpClient.EndConnect(connectResult);
            stream = tcpClient.GetStream();
            isConnected = true;
            
            if (enableDebugLogs)
                Debug.Log($"✅ Подключен к серверу {serverHost}:{serverPort}");
            
            OnConnectionStatusChanged?.Invoke("Подключен");
            
            // Запускаем поток для получения сообщений
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true; // Фоновый поток
            receiveThread.Start();
            
            // Отправляем запрос на присоединение
            SendJoinRequest();
            
        }
        catch (System.TimeoutException)
        {
            string errorMessage = $"Превышено время ожидания подключения к {serverHost}:{serverPort}.\n\n" +
                                "Проверьте:\n" +
                                "1. Запущен ли сервер (dotnet run в папке Server)\n" +
                                "2. Правильный ли IP-адрес (localhost для локального сервера)\n" +
                                "3. Не блокирует ли файрвол порт 8888";
            Debug.LogError($"❌ Ошибка подключения: {errorMessage}");
            OnConnectionStatusChanged?.Invoke($"Ошибка: Превышено время ожидания");
            
            // Закрываем соединение при ошибке
            try { tcpClient?.Close(); } catch { }
            isConnected = false;
        }
        catch (System.Net.Sockets.SocketException e)
        {
            string errorMessage = "";
            
            // Более понятные сообщения об ошибках по коду
            if (e.ErrorCode == 10061 || e.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
            {
                errorMessage = $"Сервер не запущен или недоступен на {serverHost}:{serverPort}.\n\n" +
                              "РЕШЕНИЕ:\n" +
                              "1. Откройте терминал и выполните: cd Server && dotnet run\n" +
                              "2. Убедитесь, что сервер показывает 'Ожидание подключений...'\n" +
                              "3. Используйте 'localhost' если сервер на том же компьютере\n" +
                              "4. Проверьте, что порт 8888 не занят другим приложением";
            }
            else if (e.ErrorCode == 10060 || e.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
            {
                errorMessage = $"Превышено время ожидания подключения к {serverHost}:{serverPort}.\n\n" +
                              "Проверьте IP-адрес и доступность сервера";
            }
            else if (e.ErrorCode == 10049 || e.SocketErrorCode == System.Net.Sockets.SocketError.AddressNotAvailable)
            {
                errorMessage = $"Неверный IP-адрес {serverHost}:{serverPort}.\n\n" +
                              "Используйте 'localhost' для локального сервера";
            }
            else
            {
                errorMessage = $"Ошибка подключения: {e.Message} (код: {e.ErrorCode})";
            }
            
            Debug.LogError($"❌ Ошибка подключения: {errorMessage}");
            OnConnectionStatusChanged?.Invoke($"Ошибка: Сервер недоступен");
            
            // Закрываем соединение при ошибке
            try { tcpClient?.Close(); } catch { }
            isConnected = false;
        }
        catch (System.AggregateException aggEx)
        {
            // Обрабатываем AggregateException (возникает при async операциях)
            Exception innerEx = aggEx.GetBaseException() ?? aggEx.InnerException ?? aggEx;
            string errorMessage = "";
            
            if (innerEx is System.Net.Sockets.SocketException socketEx)
            {
                if (socketEx.ErrorCode == 10061 || socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
                {
                    errorMessage = $"Сервер не запущен или недоступен на {serverHost}:{serverPort}.\n\n" +
                                  "РЕШЕНИЕ:\n" +
                                  "1. Откройте терминал и выполните: cd Server && dotnet run\n" +
                                  "2. Убедитесь, что сервер показывает 'Ожидание подключений...'\n" +
                                  "3. Используйте 'localhost' если сервер на том же компьютере\n" +
                                  "4. Проверьте файрвол (может блокировать порт 8888)";
                }
                else
                {
                    errorMessage = $"Ошибка подключения: {socketEx.Message} (код: {socketEx.ErrorCode})";
                }
            }
            else
            {
                errorMessage = $"Ошибка подключения: {innerEx.Message}";
            }
            
            Debug.LogError($"❌ Ошибка подключения: {errorMessage}");
            Debug.LogError($"Внутренняя ошибка: {innerEx.GetType().Name}: {innerEx.Message}");
            OnConnectionStatusChanged?.Invoke($"Ошибка: Сервер недоступен");
            
            // Закрываем соединение при ошибке
            try { tcpClient?.Close(); } catch { }
            isConnected = false;
        }
        catch (Exception e)
        {
            // Извлекаем внутреннее исключение если есть
            Exception innerEx = e;
            if (e is System.AggregateException aggEx)
            {
                innerEx = aggEx.GetBaseException() ?? aggEx.InnerException ?? aggEx;
            }
            
            string errorMessage = $"Ошибка подключения к {serverHost}:{serverPort}.\n" +
                                $"Тип: {innerEx.GetType().Name}\n" +
                                $"Сообщение: {innerEx.Message}";
            
            // Проверяем, не является ли это ошибкой подключения
            if (innerEx.Message.Contains("отверг запрос") || 
                innerEx.Message.Contains("connection refused") ||
                innerEx.Message.Contains("Connection refused"))
            {
                errorMessage = $"Сервер не запущен или недоступен на {serverHost}:{serverPort}.\n\n" +
                              "РЕШЕНИЕ:\n" +
                              "1. Запустите сервер: cd Server && dotnet run\n" +
                              "2. Убедитесь, что сервер работает (должно быть 'Ожидание подключений...')\n" +
                              "3. Используйте 'localhost' для локального сервера\n" +
                              "4. Проверьте файрвол";
            }
            
            Debug.LogError($"❌ Ошибка подключения: {errorMessage}");
            OnConnectionStatusChanged?.Invoke($"Ошибка: Не удалось подключиться");
            
            // Закрываем соединение при ошибке
            try { tcpClient?.Close(); } catch { }
            isConnected = false;
        }
    }
    
    [ContextMenu("Disconnect from Server")]
    public void DisconnectFromServer()
    {
        isConnected = false;
        
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        
        if (stream != null)
        {
            stream.Close();
        }
        
        if (tcpClient != null)
        {
            tcpClient.Close();
        }
        
        if (enableDebugLogs)
            Debug.Log("🔌 Отключен от сервера");
        
        OnConnectionStatusChanged?.Invoke("Отключен");
    }
    
    private void SendJoinRequest()
    {
        // Используем имя из авторизованного пользователя, если доступно
        string nameToUse = playerName;
        int stackToUse = startingStack;
        
        // Пытаемся получить имя и баланс из авторизованного пользователя
        if (AuthManager.IsLoggedIn && AuthManager.CurrentUser != null)
        {
            nameToUse = AuthManager.CurrentUser.username;
            stackToUse = AuthManager.CurrentUser.chips;
            
            if (enableDebugLogs)
                Debug.Log($"✅ Используется имя авторизованного пользователя: {nameToUse}, баланс: {stackToUse}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"⚠️ Пользователь не авторизован, используется имя из настроек: {nameToUse}");
        }
        
        var message = new Dictionary<string, object>
        {
            {"type", "join"},
            {"name", nameToUse},
            {"stack", stackToUse}
        };
        
        SendMessage(message);
    }
    
    public void SendPlayerAction(string action, int amount = 0)
    {
        var message = new Dictionary<string, object>
        {
            {"type", "action"},
            {"action", action},
            {"amount", amount}
        };
        
        SendMessage(message);
        
        if (enableDebugLogs)
            Debug.Log($"🎯 Отправлено действие: {action} (сумма: {amount})");
    }
    
    public void RequestGameState()
    {
        var message = new Dictionary<string, object>
        {
            {"type", "get_state"}
        };
        
        SendMessage(message);
    }
    
    private void SendMessage(Dictionary<string, object> message)
    {
        if (!isConnected || stream == null)
        {
            Debug.LogWarning("⚠️ Нет подключения к серверу");
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
        byte[] buffer = new byte[1024];
        
        while (isConnected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ProcessServerMessage(message);
                }
            }
            catch (Exception e)
            {
                if (isConnected)
                {
                    Debug.LogError($"❌ Ошибка получения сообщения: {e.Message}");
                }
                break;
            }
        }
    }
    
    private void ProcessServerMessage(string message)
    {
        try
        {
            var data = SimpleJSONParser.FromJSON(message);
            string messageType = data.ContainsKey("type") ? data["type"].ToString() : "";
            
            if (enableDebugLogs)
                Debug.Log($"📨 Получено сообщение: {messageType}");
            
            switch (messageType)
            {
                case "join_success":
                    clientId = data.ContainsKey("client_id") ? data["client_id"].ToString() : "";
                    Debug.Log($"✅ Успешно присоединился как {data["player_name"]}");
                    break;
                    
                case "players_update":
                    ProcessPlayersUpdate(data);
                    break;
                    
                case "hand_start":
                    Debug.Log($"🃏 Началась новая раздача");
                    OnHandStateChanged?.Invoke(true);
                    break;
                    
                case "player_action":
                    string playerName = data.ContainsKey("player_name") ? data["player_name"].ToString() : "";
                    string action = data.ContainsKey("action") ? data["action"].ToString() : "";
                    int amount = data.ContainsKey("amount") ? Convert.ToInt32(data["amount"]) : 0;
                    OnPlayerAction?.Invoke(playerName, action, amount);
                    break;
                    
                case "game_state":
                    bool handActive = data.ContainsKey("hand_active") ? Convert.ToBoolean(data["hand_active"]) : false;
                    OnHandStateChanged?.Invoke(handActive);
                    OnGameStateReceived?.Invoke(data);
                    break;
                    
                case "hole_cards":
                    string card1 = data.ContainsKey("card1") ? data["card1"].ToString() : "";
                    string card2 = data.ContainsKey("card2") ? data["card2"].ToString() : "";
                    OnHoleCardsReceived?.Invoke(card1, card2);
                    if (enableDebugLogs)
                        Debug.Log($"🃏 Получены карты: {card1}, {card2}");
                    break;
                    
                case "error":
                    Debug.LogError($"❌ Ошибка сервера: {data["message"]}");
                    break;
                    
                default:
                    Debug.LogWarning($"⚠️ Неизвестный тип сообщения: {messageType}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Ошибка обработки сообщения: {e.Message}");
        }
    }
    
    private void ProcessPlayersUpdate(Dictionary<string, object> data)
    {
        if (!data.ContainsKey("players"))
        {
            OnPlayersListUpdated?.Invoke(new string[0]);
            return;
        }
        
        // Простая обработка списка игроков
        var playersData = data["players"];
        string[] playerNames = new string[0];
        
        if (playersData != null)
        {
            // Если это строка с разделителями
            if (playersData is string playersString)
            {
                playerNames = playersString.Split(',');
            }
        }
        
        OnPlayersListUpdated?.Invoke(playerNames);
        
        if (enableDebugLogs)
            Debug.Log($"👥 Обновлен список игроков: {string.Join(", ", playerNames)}");
    }
    
    // Публичные методы для UI
    public void Fold()
    {
        SendPlayerAction("fold");
    }
    
    public void Call()
    {
        SendPlayerAction("call");
    }
    
    public void Raise(int amount)
    {
        SendPlayerAction("raise", amount);
    }
    
    public void Check()
    {
        SendPlayerAction("check");
    }
    
    public bool IsConnected()
    {
        return isConnected;
    }
    
    public string GetClientId()
    {
        return clientId;
    }
}