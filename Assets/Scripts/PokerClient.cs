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
        ConnectToServer();
    }
    
    private void OnDestroy()
    {
        DisconnectFromServer();
    }
    
    [ContextMenu("Connect to Server")]
    public void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(serverHost, serverPort);
            stream = tcpClient.GetStream();
            isConnected = true;
            
            if (enableDebugLogs)
                Debug.Log($"🔌 Подключен к серверу {serverHost}:{serverPort}");
            
            OnConnectionStatusChanged?.Invoke("Подключен");
            
            // Запускаем поток для получения сообщений
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();
            
            // Отправляем запрос на присоединение
            SendJoinRequest();
            
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Ошибка подключения: {e.Message}");
            OnConnectionStatusChanged?.Invoke($"Ошибка: {e.Message}");
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
        var message = new Dictionary<string, object>
        {
            {"type", "join"},
            {"name", playerName},
            {"stack", startingStack}
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