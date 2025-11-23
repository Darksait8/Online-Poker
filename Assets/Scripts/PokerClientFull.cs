using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Полноценный покерный клиент с поддержкой всех функций
/// </summary>
public class PokerClientFull : MonoBehaviour
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
    
    // Игровое состояние
    private GameState gameState = new GameState();
    
    // События для UI
    public System.Action<string> OnConnectionStatusChanged;
    public System.Action<List<PlayerInfo>> OnPlayersListUpdated;
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<string, string, int> OnPlayerAction;
    public System.Action<string> OnPhaseChanged;
    public System.Action OnHandStart;
    public System.Action OnHandEnd;
    
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
                    ProcessHandStart(data);
                    break;
                    
                case "phase_change":
                    ProcessPhaseChange(data);
                    break;
                    
                case "hole_cards_dealt":
                    ProcessHoleCardsDealt();
                    break;
                    
                case "community_cards_dealt":
                    ProcessCommunityCardsDealt(data);
                    break;
                    
                case "showdown":
                    ProcessShowdown(data);
                    break;
                    
                case "hand_end":
                    ProcessHandEnd();
                    break;
                    
                case "game_state":
                    ProcessGameState(data);
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
        var players = new List<PlayerInfo>();
        
        if (data.ContainsKey("players"))
        {
            // Простая обработка списка игроков
            var playersData = data["players"];
            if (playersData is string playersString)
            {
                var playerNames = playersString.Split(',');
                for (int i = 0; i < playerNames.Length; i++)
                {
                    players.Add(new PlayerInfo
                    {
                        name = playerNames[i].Trim(),
                        stack = 1000, // По умолчанию
                        position = i,
                        bet = 0,
                        folded = false
                    });
                }
            }
        }
        
        OnPlayersListUpdated?.Invoke(players);
        
        if (enableDebugLogs)
            Debug.Log($"👥 Обновлен список игроков: {players.Count} игроков");
    }
    
    private void ProcessHandStart(Dictionary<string, object> data)
    {
        gameState.phase = "preflop";
        gameState.pot = 0;
        gameState.currentBet = 0;
        
        if (data.ContainsKey("small_blind"))
            gameState.smallBlind = Convert.ToInt32(data["small_blind"]);
        if (data.ContainsKey("big_blind"))
            gameState.bigBlind = Convert.ToInt32(data["big_blind"]);
        if (data.ContainsKey("dealer_position"))
            gameState.dealerPosition = Convert.ToInt32(data["dealer_position"]);
        
        OnHandStart?.Invoke();
        OnGameStateChanged?.Invoke(gameState);
        
        Debug.Log($"🃏 Началась новая раздача");
    }
    
    private void ProcessPhaseChange(Dictionary<string, object> data)
    {
        if (data.ContainsKey("phase"))
            gameState.phase = data["phase"].ToString();
        if (data.ContainsKey("pot"))
            gameState.pot = Convert.ToInt32(data["pot"]);
        if (data.ContainsKey("current_bet"))
            gameState.currentBet = Convert.ToInt32(data["current_bet"]);
        
        OnPhaseChanged?.Invoke(gameState.phase);
        OnGameStateChanged?.Invoke(gameState);
        
        Debug.Log($"🔄 Фаза изменена: {gameState.phase}");
    }
    
    private void ProcessHoleCardsDealt()
    {
        Debug.Log($"🃏 Разданы карманные карты");
        // Здесь можно добавить логику отображения карт
    }
    
    private void ProcessCommunityCardsDealt(Dictionary<string, object> data)
    {
        if (data.ContainsKey("phase"))
            gameState.phase = data["phase"].ToString();
        
        Debug.Log($"🃏 Разданы общие карты: {gameState.phase}");
    }
    
    private void ProcessShowdown(Dictionary<string, object> data)
    {
        if (data.ContainsKey("pot"))
            gameState.pot = Convert.ToInt32(data["pot"]);
        
        Debug.Log($"🏆 SHOWDOWN! Банк: {gameState.pot}");
    }
    
    private void ProcessHandEnd()
    {
        gameState.phase = "waiting";
        gameState.pot = 0;
        gameState.currentBet = 0;
        
        OnHandEnd?.Invoke();
        OnGameStateChanged?.Invoke(gameState);
        
        Debug.Log($"🔄 Раздача завершена");
    }
    
    private void ProcessGameState(Dictionary<string, object> data)
    {
        if (data.ContainsKey("phase"))
            gameState.phase = data["phase"].ToString();
        if (data.ContainsKey("pot"))
            gameState.pot = Convert.ToInt32(data["pot"]);
        if (data.ContainsKey("current_bet"))
            gameState.currentBet = Convert.ToInt32(data["current_bet"]);
        if (data.ContainsKey("dealer_position"))
            gameState.dealerPosition = Convert.ToInt32(data["dealer_position"]);
        
        OnGameStateChanged?.Invoke(gameState);
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
    
    public GameState GetGameState()
    {
        return gameState;
    }
}

/// <summary>
/// Информация об игроке
/// </summary>
[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int stack;
    public int position;
    public int bet;
    public bool folded;
}

/// <summary>
/// Состояние игры
/// </summary>
[System.Serializable]
public class GameState
{
    public string phase = "waiting"; // waiting, preflop, flop, turn, river, showdown
    public int pot = 0;
    public int currentBet = 0;
    public int smallBlind = 10;
    public int bigBlind = 20;
    public int dealerPosition = 0;
    public int currentPlayer = 0;
}
