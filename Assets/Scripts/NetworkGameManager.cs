using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;

/// <summary>
/// Менеджер для синхронизации игровых действий через Photon Network
/// Использует гибридный подход: Events для гибкости и RPC для критичных операций
/// </summary>
public class NetworkGameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static NetworkGameManager Instance { get; private set; }
    
    private GameManager gameManager;
    private new PhotonView photonView;
    
    // События для синхронизации
    public const byte EVENT_PLAYER_ACTION = 1;
    public const byte EVENT_GAME_STATE = 2;
    public const byte EVENT_PLAYER_JOINED = 3;
    public const byte EVENT_PLAYER_LEFT = 4;
    public const byte EVENT_COMMUNITY_CARDS = 5;
    public const byte EVENT_PLAYER_CARDS = 6;
    public const byte EVENT_GAME_STARTED = 7;
    public const byte EVENT_GAME_ENDED = 8;
    public const byte EVENT_ROUND_STARTED = 9;
    public const byte EVENT_PLAYER_TURN = 10;
    
    private bool isOnlineGame = false;
    private Dictionary<int, string> actorToPlayerName = new Dictionary<int, string>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Добавляем PhotonView для использования RPC
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
                photonView.ViewID = 1; // Фиксированный ID для NetworkGameManager
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        FindGameManager();
    }
    
    private void FindGameManager()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }
    
    private void Update()
    {
        // Периодически ищем GameManager, если его еще нет
        if (gameManager == null && isOnlineGame)
        {
            FindGameManager();
        }
    }
    
    public override void OnEnable()
    {
        // Вызываем базовый метод для регистрации callbacks
        base.OnEnable();
        
        // Подписываемся на события Photon
        if (PhotonNetwork.NetworkingClient != null)
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
        }
    }
    
    public override void OnDisable()
    {
        // Отписываемся от событий Photon
        if (PhotonNetwork.NetworkingClient != null)
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
        }
        
        // Вызываем базовый метод для отмены регистрации callbacks
        base.OnDisable();
    }
    
    private void Start()
    {
        // Проверяем, является ли это онлайн игрой
        isOnlineGame = TableRuntimeConfig.HasConfig && TableRuntimeConfig.IsOnlineTable;
        
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // Уведомляем других игроков о присоединении
            NotifyPlayerJoined();
            
            // Если это онлайн игра, синхронизируем существующих игроков
            if (isOnlineGame && gameManager != null)
            {
                SyncExistingPlayers();
            }
        }
    }
    
    private void SyncExistingPlayers()
    {
        // Синхронизируем список игроков с мастер-клиентом
        if (PhotonNetwork.IsMasterClient)
        {
            // Мастер-клиент отправляет список всех игроков новым участникам
            SendAllPlayersToNewcomer();
        }
    }
    
    private void SendAllPlayersToNewcomer()
    {
        if (gameManager == null) return;
        
        var players = gameManager.Players;
        if (players == null || players.Count == 0) return;
        
        Hashtable playersData = new Hashtable();
        playersData["players"] = new object[players.Count];
        
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            Hashtable playerInfo = new Hashtable();
            playerInfo["name"] = player.Name;
            playerInfo["stack"] = player.Stack;
            playerInfo["seatIndex"] = i;
            playerInfo["status"] = (int)player.Status;
            ((object[])playersData["players"])[i] = playerInfo;
        }
        
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.DoNotCache
        };
        
        PhotonNetwork.RaiseEvent(EVENT_PLAYER_JOINED, playersData, raiseEventOptions, SendOptions.SendReliable);
    }
    
    /// <summary>
    /// Отправляет действие игрока другим клиентам
    /// Использует RPC для гарантированной доставки критичных действий
    /// </summary>
    public void SendPlayerAction(string playerName, string actionType, int amount, int seatIndex)
    {
        if (!isOnlineGame || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            return;
        
        // Используем RPC для критичных действий (более надежно)
        if (photonView != null)
        {
            photonView.RPC("RPC_PlayerAction", RpcTarget.Others, playerName, actionType, amount, seatIndex, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            // Fallback на Events, если PhotonView недоступен
            Hashtable actionData = new Hashtable();
            actionData["playerName"] = playerName;
            actionData["actionType"] = actionType;
            actionData["amount"] = amount;
            actionData["seatIndex"] = seatIndex;
            actionData["actorNumber"] = PhotonNetwork.LocalPlayer.ActorNumber;
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.DoNotCache
            };
            
            PhotonNetwork.RaiseEvent(EVENT_PLAYER_ACTION, actionData, raiseEventOptions, SendOptions.SendReliable);
        }
    }
    
    /// <summary>
    /// RPC метод для получения действия игрока (вызывается автоматически Photon)
    /// </summary>
    [PunRPC]
    private void RPC_PlayerAction(string playerName, string actionType, int amount, int seatIndex, int actorNumber)
    {
        Debug.Log($"RPC: Player {playerName} (Actor {actorNumber}) performed action {actionType} with amount {amount}");
        
        // Игнорируем действия от локального игрока
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            return;
        
        // Обрабатываем действие
        SyncPlayerAction(playerName, actionType, amount, seatIndex);
    }
    
    /// <summary>
    /// Отправляет состояние игры другим клиентам
    /// Использует RPC для синхронизации состояния
    /// </summary>
    public void SendGameState(int currentPhase, int currentPlayerIndex, int currentBet, int pot)
    {
        if (!isOnlineGame || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            return;
        
        // Используем RPC для синхронизации состояния
        if (photonView != null)
        {
            photonView.RPC("RPC_GameState", RpcTarget.Others, currentPhase, currentPlayerIndex, currentBet, pot);
        }
        else
        {
            // Fallback на Events
            Hashtable stateData = new Hashtable();
            stateData["phase"] = currentPhase;
            stateData["currentPlayerIndex"] = currentPlayerIndex;
            stateData["currentBet"] = currentBet;
            stateData["pot"] = pot;
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.DoNotCache
            };
            
            PhotonNetwork.RaiseEvent(EVENT_GAME_STATE, stateData, raiseEventOptions, SendOptions.SendReliable);
        }
    }
    
    /// <summary>
    /// RPC метод для получения состояния игры
    /// </summary>
    [PunRPC]
    private void RPC_GameState(int currentPhase, int currentPlayerIndex, int currentBet, int pot)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            SyncGameState((GamePhase)currentPhase, currentPlayerIndex, currentBet, pot);
        }
    }
    
    /// <summary>
    /// Отправляет общие карты другим клиентам
    /// Использует RPC с буферизацией для новых игроков
    /// </summary>
    public void SendCommunityCards(Card[] cards)
    {
        if (!isOnlineGame || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            return;
        
        if (cards == null || cards.Length == 0)
            return;
        
        int[] serializedCards = SerializeCards(cards);
        
        // Используем RPC с буферизацией, чтобы новые игроки получили карты
        if (photonView != null)
        {
            photonView.RPC("RPC_CommunityCards", RpcTarget.OthersBuffered, serializedCards, cards.Length);
        }
        else
        {
            // Fallback на Events
            Hashtable cardsData = new Hashtable();
            cardsData["count"] = cards.Length;
            cardsData["cards"] = serializedCards;
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.AddToRoomCache // Кэшируем для новых игроков
            };
            
            PhotonNetwork.RaiseEvent(EVENT_COMMUNITY_CARDS, cardsData, raiseEventOptions, SendOptions.SendReliable);
        }
    }
    
    /// <summary>
    /// RPC метод для получения общих карт
    /// </summary>
    [PunRPC]
    private void RPC_CommunityCards(int[] serializedCards, int count)
    {
        Card[] cards = DeserializeCards(serializedCards);
        if (cards != null && cards.Length == count)
        {
            Debug.Log($"RPC: Received {count} community cards");
            SyncCommunityCards(cards);
        }
    }
    
    /// <summary>
    /// Отправляет карты игрока (только мастер-клиент видит все карты)
    /// Использует RPC для синхронизации карт
    /// </summary>
    public void SendPlayerCards(string playerName, int seatIndex, Card[] cards, bool isLocalPlayer)
    {
        if (!isOnlineGame || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            return;
        
        if (cards == null || cards.Length < 2)
            return;
        
        int[] serializedCards = SerializeCards(cards);
        
        // Используем RPC для отправки карт
        if (photonView != null)
        {
            // Отправляем карты только если это локальный игрок или мастер-клиент
            if (isLocalPlayer || PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_PlayerCards", RpcTarget.Others, playerName, seatIndex, serializedCards, isLocalPlayer);
            }
            else
            {
                // Отправляем только информацию о том, что карты разданы (без самих карт)
                photonView.RPC("RPC_PlayerCardsDealt", RpcTarget.Others, playerName, seatIndex, isLocalPlayer);
            }
        }
        else
        {
            // Fallback на Events
            Hashtable cardsData = new Hashtable();
            cardsData["playerName"] = playerName;
            cardsData["seatIndex"] = seatIndex;
            cardsData["isLocalPlayer"] = isLocalPlayer;
            
            if (isLocalPlayer || PhotonNetwork.IsMasterClient)
            {
                cardsData["cards"] = serializedCards;
            }
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.DoNotCache
            };
            
            PhotonNetwork.RaiseEvent(EVENT_PLAYER_CARDS, cardsData, raiseEventOptions, SendOptions.SendReliable);
        }
    }
    
    /// <summary>
    /// RPC метод для получения карт игрока
    /// </summary>
    [PunRPC]
    private void RPC_PlayerCards(string playerName, int seatIndex, int[] serializedCards, bool isLocalPlayer)
    {
        Card[] cards = DeserializeCards(serializedCards);
        if (cards != null)
        {
            Debug.Log($"RPC: Received cards for player {playerName} at seat {seatIndex}");
            SyncPlayerCards(playerName, seatIndex, cards);
        }
    }
    
    /// <summary>
    /// RPC метод для уведомления о раздаче карт (без самих карт)
    /// </summary>
    [PunRPC]
    private void RPC_PlayerCardsDealt(string playerName, int seatIndex, bool isLocalPlayer)
    {
        Debug.Log($"RPC: Player {playerName} at seat {seatIndex} received cards (showing backs)");
        // Показываем рубашку карт для других игроков
    }
    
    private int[] SerializeCards(Card[] cards)
    {
        int[] serialized = new int[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            // Кодируем карту: suit * 13 + (rank - 2), так как Rank начинается с 2
            int suit = (int)cards[i].Suit;
            int rank = (int)cards[i].Rank - 2; // Rank начинается с 2 (Two = 2)
            serialized[i] = suit * 13 + rank;
        }
        return serialized;
    }
    
    private Card[] DeserializeCards(int[] serialized)
    {
        if (serialized == null || serialized.Length == 0)
            return null;
        
        Card[] cards = new Card[serialized.Length];
        for (int i = 0; i < serialized.Length; i++)
        {
            int suit = serialized[i] / 13;
            int rank = serialized[i] % 13;
            // Rank начинается с 2 (Two = 2), поэтому добавляем 2
            cards[i] = new Card((Suit)suit, (Rank)(rank + 2));
        }
        return cards;
    }
    
    private void NotifyPlayerJoined()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            return;
        
        // Устанавливаем Custom Properties игрока для синхронизации профиля
        if (AuthManager.IsLoggedIn && AuthManager.CurrentUser != null)
        {
            SetPlayerCustomProperties();
        }
        
        Hashtable playerData = new Hashtable();
        playerData["playerName"] = PhotonNetwork.NickName;
        playerData["actorNumber"] = PhotonNetwork.LocalPlayer.ActorNumber;
        
        if (AuthManager.IsLoggedIn && AuthManager.CurrentUser != null)
        {
            playerData["chips"] = AuthManager.CurrentUser.chips;
            playerData["xp"] = AuthManager.CurrentUser.XP;
            playerData["level"] = AuthManager.CurrentUser.Level;
            playerData["totalGamesPlayed"] = AuthManager.CurrentUser.totalGamesPlayed;
            playerData["gamesWon"] = AuthManager.CurrentUser.gamesWon;
        }
        
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.DoNotCache
        };
        
        PhotonNetwork.RaiseEvent(EVENT_PLAYER_JOINED, playerData, raiseEventOptions, SendOptions.SendReliable);
    }
    
    /// <summary>
    /// Устанавливает Custom Properties игрока для синхронизации профиля через Photon
    /// </summary>
    private void SetPlayerCustomProperties()
    {
        if (!AuthManager.IsLoggedIn || AuthManager.CurrentUser == null)
            return;
        
        var user = AuthManager.CurrentUser;
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        
        // Основная информация
        props["username"] = user.username;
        props["chips"] = user.chips;
        props["xp"] = user.XP;
        props["level"] = user.Level;
        
        // Статистика
        props["totalGamesPlayed"] = user.totalGamesPlayed;
        props["gamesWon"] = user.gamesWon;
        props["gamesLost"] = user.gamesLost;
        props["winRate"] = user.winRate;
        props["totalWinnings"] = user.totalWinnings;
        
        // Устанавливаем свойства игрока
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    
    /// <summary>
    /// Обновляет Custom Properties игрока при изменении профиля
    /// </summary>
    public void UpdatePlayerProfile()
    {
        if (isOnlineGame && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            SetPlayerCustomProperties();
        }
    }
    
    private void OnPhotonEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case EVENT_PLAYER_ACTION:
                HandlePlayerActionEvent(photonEvent);
                break;
            case EVENT_GAME_STATE:
                HandleGameStateEvent(photonEvent);
                break;
            case EVENT_PLAYER_JOINED:
                HandlePlayerJoinedEvent(photonEvent);
                break;
            case EVENT_PLAYER_LEFT:
                HandlePlayerLeftEvent(photonEvent);
                break;
            case EVENT_COMMUNITY_CARDS:
                HandleCommunityCardsEvent(photonEvent);
                break;
            case EVENT_PLAYER_CARDS:
                HandlePlayerCardsEvent(photonEvent);
                break;
            case EVENT_GAME_STARTED:
                HandleGameStartedEvent(photonEvent);
                break;
            case EVENT_GAME_ENDED:
                HandleGameEndedEvent(photonEvent);
                break;
            case EVENT_ROUND_STARTED:
                HandleRoundStartedEvent(photonEvent);
                break;
            case EVENT_PLAYER_TURN:
                HandlePlayerTurnEvent(photonEvent);
                break;
        }
    }
    
    private void HandleCommunityCardsEvent(EventData photonEvent)
    {
        if (!isOnlineGame || gameManager == null) return;
        
        Hashtable data = (Hashtable)photonEvent.CustomData;
        int count = (int)data["count"];
        int[] serializedCards = (int[])data["cards"];
        
        Card[] cards = DeserializeCards(serializedCards);
        if (cards != null && cards.Length == count)
        {
            Debug.Log($"Network: Received {count} community cards");
            // Синхронизируем общие карты
            SyncCommunityCards(cards);
        }
    }
    
    private void HandlePlayerCardsEvent(EventData photonEvent)
    {
        if (!isOnlineGame || gameManager == null) return;
        
        Hashtable data = (Hashtable)photonEvent.CustomData;
        string playerName = (string)data["playerName"];
        int seatIndex = (int)data["seatIndex"];
        bool isLocalPlayer = (bool)data["isLocalPlayer"];
        
        // Если это не локальный игрок и не мастер-клиент, показываем рубашку карт
        if (!isLocalPlayer && !PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"Network: Player {playerName} received cards (showing backs)");
            // Показываем рубашку карт для других игроков
        }
        else if (data.ContainsKey("cards"))
        {
            int[] serializedCards = (int[])data["cards"];
            Card[] cards = DeserializeCards(serializedCards);
            if (cards != null)
            {
                Debug.Log($"Network: Received cards for player {playerName}");
                SyncPlayerCards(playerName, seatIndex, cards);
            }
        }
    }
    
    private void HandleGameStartedEvent(EventData photonEvent)
    {
        if (!isOnlineGame || gameManager == null) return;
        
        Debug.Log("Network: Game started event received");
        // Синхронизируем начало игры
    }
    
    private void HandleGameEndedEvent(EventData photonEvent)
    {
        if (!isOnlineGame || gameManager == null) return;
        
        Debug.Log("Network: Game ended event received");
        // Синхронизируем окончание игры
    }
    
    private void HandleRoundStartedEvent(EventData photonEvent)
    {
        if (!isOnlineGame || gameManager == null) return;
        
        Hashtable data = (Hashtable)photonEvent.CustomData;
        int round = (int)data["round"];
        Debug.Log($"Network: Round {round} started");
        // Синхронизируем начало раунда
    }
    
    private void HandlePlayerTurnEvent(EventData photonEvent)
    {
        if (!isOnlineGame || gameManager == null) return;
        
        Hashtable data = (Hashtable)photonEvent.CustomData;
        int seatIndex = (int)data["seatIndex"];
        Debug.Log($"Network: Player turn - seat {seatIndex}");
        // Синхронизируем ход игрока
    }
    
    private void SyncCommunityCards(Card[] cards)
    {
        // Синхронизация общих карт обрабатывается через GameManager
        Debug.Log($"Syncing {cards.Length} community cards");
    }
    
    private void SyncPlayerCards(string playerName, int seatIndex, Card[] cards)
    {
        // Синхронизация карт игрока
        Debug.Log($"Syncing cards for {playerName} at seat {seatIndex}");
    }
    
    private void HandlePlayerActionEvent(EventData photonEvent)
    {
        if (!isOnlineGame || gameManager == null) return;
        
        Hashtable data = (Hashtable)photonEvent.CustomData;
        string playerName = (string)data["playerName"];
        string actionType = (string)data["actionType"];
        int amount = (int)data["amount"];
        int seatIndex = (int)data["seatIndex"];
        int actorNumber = (int)data["actorNumber"];
        
        Debug.Log($"Network: Player {playerName} (Actor {actorNumber}) performed action {actionType} with amount {amount}");
        
        // Игнорируем действия от локального игрока (они уже обработаны локально)
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            return;
        
        // Синхронизируем действие с GameManager
        SyncPlayerAction(playerName, actionType, amount, seatIndex);
    }
    
    private void SyncPlayerAction(string playerName, string actionType, int amount, int seatIndex)
    {
        if (gameManager == null) return;
        
        var players = gameManager.Players;
        if (players == null || seatIndex < 0 || seatIndex >= players.Count)
            return;
        
        var player = players[seatIndex];
        if (player == null || player.Name != playerName)
            return;
        
        // Обновляем UI с действием игрока
        // Реальная логика игры обрабатывается мастер-клиентом
        Debug.Log($"Syncing action: {playerName} - {actionType} ({amount})");
    }
    
    private void HandleGameStateEvent(EventData photonEvent)
    {
        if (!isOnlineGame || gameManager == null) return;
        
        Hashtable data = (Hashtable)photonEvent.CustomData;
        int phase = (int)data["phase"];
        int currentPlayerIndex = (int)data["currentPlayerIndex"];
        int currentBet = (int)data["currentBet"];
        int pot = (int)data["pot"];
        
        Debug.Log($"Network: Game state updated - Phase: {phase}, Current Player: {currentPlayerIndex}, Bet: {currentBet}, Pot: {pot}");
        
        // Синхронизируем состояние игры (только если мы не мастер-клиент)
        if (!PhotonNetwork.IsMasterClient)
        {
            SyncGameState((GamePhase)phase, currentPlayerIndex, currentBet, pot);
        }
    }
    
    private void SyncGameState(GamePhase phase, int currentPlayerIndex, int currentBet, int pot)
    {
        // Обновляем состояние игры для синхронизации UI
        // Основная логика обрабатывается мастер-клиентом
        Debug.Log($"Syncing game state: Phase={phase}, CurrentPlayer={currentPlayerIndex}, Bet={currentBet}, Pot={pot}");
    }
    
    private void HandlePlayerJoinedEvent(EventData photonEvent)
    {
        if (!isOnlineGame || gameManager == null) return;
        
        Hashtable data = (Hashtable)photonEvent.CustomData;
        
        // Проверяем, это список всех игроков или один игрок
        if (data.ContainsKey("players"))
        {
            // Это список всех игроков от мастер-клиента
            HandleAllPlayersList(data);
            return;
        }
        
        string playerName = (string)data["playerName"];
        int actorNumber = (int)data["actorNumber"];
        
        Debug.Log($"Network: Player {playerName} (Actor {actorNumber}) joined the game");
        
        // Сохраняем связь actorNumber -> playerName
        actorToPlayerName[actorNumber] = playerName;
        
        // Добавляем игрока в игру, если это мастер-клиент
        if (PhotonNetwork.IsMasterClient)
        {
            int chips = data.ContainsKey("chips") ? (int)data["chips"] : 1000;
            AddNetworkPlayer(playerName, chips, actorNumber);
        }
    }
    
    private void HandleAllPlayersList(Hashtable data)
    {
        // Обрабатываем список всех игроков от мастер-клиента
        object[] playersArray = (object[])data["players"];
        if (playersArray == null) return;
        
        Debug.Log($"Network: Received list of {playersArray.Length} players from master client");
        
        // Синхронизируем список игроков
        foreach (Hashtable playerInfo in playersArray)
        {
            string name = (string)playerInfo["name"];
            int stack = (int)playerInfo["stack"];
            int seatIndex = (int)playerInfo["seatIndex"];
            
            // Добавляем игрока в игру
            if (gameManager != null)
            {
                gameManager.AddPlayer(name, stack, seatIndex);
            }
        }
    }
    
    private void AddNetworkPlayer(string playerName, int chips, int actorNumber)
    {
        FindGameManager();
        if (gameManager == null)
        {
            Debug.LogWarning("NetworkGameManager: GameManager not found, cannot add player");
            return;
        }
        
        // Добавляем сетевого игрока в игру
        bool added = gameManager.AddPlayer(playerName, chips);
        if (added)
        {
            actorToPlayerName[actorNumber] = playerName;
            Debug.Log($"Network: Added player {playerName} with {chips} chips (Actor {actorNumber})");
            
            // Если игра еще не началась и есть минимум 2 игрока, можно начать игру
            if (PhotonNetwork.IsMasterClient && gameManager.Players != null && gameManager.Players.Count >= 2)
            {
                // Игра начнется автоматически при следующей раздаче
            }
        }
        else
        {
            Debug.LogWarning($"NetworkGameManager: Failed to add player {playerName}");
        }
    }
    
    private void HandlePlayerLeftEvent(EventData photonEvent)
    {
        Hashtable data = (Hashtable)photonEvent.CustomData;
        string playerName = (string)data["playerName"];
        int actorNumber = (int)data["actorNumber"];
        
        Debug.Log($"Network: Player {playerName} (Actor {actorNumber}) left the game");
        
        // Удаляем игрока из игры
        if (gameManager != null)
        {
            // gameManager.RemovePlayer(playerName);
        }
    }
    
    // Реализация методов интерфейса IInRoomCallbacks
    // Эти методы вызываются автоматически через систему callbacks Photon
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"Player {newPlayer.NickName} entered the room");
        
        if (!isOnlineGame) return;
        
        // Сохраняем связь actorNumber -> playerName
        actorToPlayerName[newPlayer.ActorNumber] = newPlayer.NickName;
        
        // Читаем профиль игрока из Custom Properties
        int chips = 1000; // Значение по умолчанию
        int xp = 0;
        int level = 1;
        
        if (newPlayer.CustomProperties.ContainsKey("chips"))
            chips = (int)newPlayer.CustomProperties["chips"];
        if (newPlayer.CustomProperties.ContainsKey("xp"))
            xp = (int)newPlayer.CustomProperties["xp"];
        if (newPlayer.CustomProperties.ContainsKey("level"))
            level = (int)newPlayer.CustomProperties["level"];
        
        Debug.Log($"Player {newPlayer.NickName} profile: Chips={chips}, XP={xp}, Level={level}");
        
        // Если мы мастер-клиент, добавляем игрока в игру
        if (PhotonNetwork.IsMasterClient && gameManager != null)
        {
            AddNetworkPlayer(newPlayer.NickName, chips, newPlayer.ActorNumber);
            
            // Отправляем новому игроку полное состояние игры
            SendFullGameStateToPlayer(newPlayer);
        }
        
        // Обновляем таблицу лидеров при присоединении нового игрока
        UpdateLeaderboard();
    }
    
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!isOnlineGame) return;
        
        Debug.Log($"Player {targetPlayer.NickName} properties updated");
        
        // Если обновились статистические данные, обновляем таблицу лидеров
        if (changedProps.ContainsKey("chips") || changedProps.ContainsKey("xp") || 
            changedProps.ContainsKey("level") || changedProps.ContainsKey("gamesWon"))
        {
            UpdateLeaderboard();
        }
    }
    
    /// <summary>
    /// Обновляет таблицу лидеров на основе данных из Photon Custom Properties
    /// </summary>
    private void UpdateLeaderboard()
    {
        if (!PhotonNetwork.InRoom) return;
        
        // Собираем данные всех игроков из комнаты
        List<LeaderboardEntry> onlinePlayers = new List<LeaderboardEntry>();
        
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties == null) continue;
            
            var props = player.CustomProperties;
            string username = player.NickName;
            int chips = props.ContainsKey("chips") ? (int)props["chips"] : 1000;
            int xp = props.ContainsKey("xp") ? (int)props["xp"] : 0;
            int level = props.ContainsKey("level") ? (int)props["level"] : 1;
            
            onlinePlayers.Add(new LeaderboardEntry
            {
                username = username,
                chips = chips,
                xp = xp,
                level = level,
                isCurrentUser = player == PhotonNetwork.LocalPlayer
            });
        }
        
        Debug.Log($"Updated leaderboard with {onlinePlayers.Count} online players");
        
        // Здесь можно обновить UI таблицы лидеров, если нужно
        // Например, через событие или прямой вызов MainMenuUIController
    }
    
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.NickName} left the room");
        
        if (!isOnlineGame) return;
        
        // Удаляем связь
        if (actorToPlayerName.ContainsKey(otherPlayer.ActorNumber))
        {
            string playerName = actorToPlayerName[otherPlayer.ActorNumber];
            actorToPlayerName.Remove(otherPlayer.ActorNumber);
            
            // Удаляем игрока из игры
            if (gameManager != null)
            {
                gameManager.RemovePlayer(playerName);
            }
        }
    }
    
    private void SendFullGameStateToPlayer(Photon.Realtime.Player targetPlayer)
    {
        if (gameManager == null) return;
        
        int phase = (int)gameManager.CurrentPhase;
        int currentPlayerIndex = gameManager.CurrentPlayerIndex;
        int currentBet = gameManager.CurrentBet;
        int pot = gameManager.Pots != null && gameManager.Pots.Count > 0 ? gameManager.Pots[0] : 0;
        
        // Используем RPC для отправки состояния конкретному игроку
        if (photonView != null)
        {
            photonView.RPC("RPC_GameState", targetPlayer, phase, currentPlayerIndex, currentBet, pot);
        }
        else
        {
            // Fallback на Events
            Hashtable stateData = new Hashtable();
            stateData["phase"] = phase;
            stateData["currentPlayerIndex"] = currentPlayerIndex;
            stateData["currentBet"] = currentBet;
            stateData["pot"] = pot;
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                TargetActors = new int[] { targetPlayer.ActorNumber },
                CachingOption = EventCaching.DoNotCache
            };
            
            PhotonNetwork.RaiseEvent(EVENT_GAME_STATE, stateData, raiseEventOptions, SendOptions.SendReliable);
        }
    }
    
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log($"Master client switched to: {newMasterClient.NickName}");
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Используется для синхронизации через PhotonView компонент
        // Если нужно синхронизировать позиции или другие данные
    }
    
    /// <summary>
    /// Проверяет, является ли текущий игрок мастер-клиентом
    /// </summary>
    public bool IsMasterClient()
    {
        return PhotonNetwork.IsMasterClient;
    }
    
    /// <summary>
    /// Получает количество игроков в комнате
    /// </summary>
    public int GetPlayerCount()
    {
        if (PhotonNetwork.InRoom)
            return PhotonNetwork.CurrentRoom.PlayerCount;
        return 0;
    }
    
    /// <summary>
    /// Получает список всех игроков в комнате
    /// </summary>
    public List<Photon.Realtime.Player> GetPlayersInRoom()
    {
        List<Photon.Realtime.Player> players = new List<Photon.Realtime.Player>();
        if (PhotonNetwork.InRoom)
        {
            foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            {
                players.Add(player);
            }
        }
        return players;
    }
}

