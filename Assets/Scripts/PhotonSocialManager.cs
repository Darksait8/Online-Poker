using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ExitGames.Client.Photon;

/// <summary>
/// Менеджер для синхронизации социальных функций через Photon:
/// - Друзья и заявки в друзья
/// - Инвайты к столу
/// - Поиск друзей онлайн
/// - Синхронизация профилей пользователей
/// </summary>
public class PhotonSocialManager : MonoBehaviourPunCallbacks
{
    public static PhotonSocialManager Instance { get; private set; }
    
    // Коды событий Photon для социальных функций
    public const byte EVENT_FRIEND_REQUEST = 100;
    public const byte EVENT_FRIEND_REQUEST_ACCEPTED = 101;
    public const byte EVENT_FRIEND_REQUEST_DECLINED = 102;
    public const byte EVENT_FRIEND_REMOVED = 103;
    public const byte EVENT_TABLE_INVITE = 104;
    public const byte EVENT_TABLE_INVITE_ACCEPTED = 105;
    public const byte EVENT_TABLE_INVITE_DECLINED = 106;
    public const byte EVENT_PROFILE_UPDATE = 107;
    
    // События для UI
    public static event Action<string> OnFriendRequestReceived; // username
    public static event Action<string> OnFriendRequestAccepted; // username
    public static event Action<string> OnFriendRemoved; // username
    public static event Action<TableInvite> OnTableInviteReceived;
    public static event Action<List<OnlineFriendInfo>> OnFriendsOnlineStatusUpdated;
    
    private Dictionary<string, OnlineFriendInfo> onlineFriends = new Dictionary<string, OnlineFriendInfo>();
    private bool isInitialized = false;
    
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
    
    /// <summary>
    /// Создает экземпляр PhotonSocialManager если его еще нет
    /// </summary>
    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("PhotonSocialManager");
            go.AddComponent<PhotonSocialManager>();
        }
    }
    
    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            InitializeSocialFeatures();
        }
    }
    
    public override void OnEnable()
    {
        base.OnEnable();
        if (PhotonNetwork.NetworkingClient != null)
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
        }
    }
    
    public override void OnDisable()
    {
        if (PhotonNetwork.NetworkingClient != null)
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
        }
        base.OnDisable();
    }
    
    public override void OnConnectedToMaster()
    {
        InitializeSocialFeatures();
    }
    
    private void InitializeSocialFeatures()
    {
        if (isInitialized || !PhotonNetwork.IsConnected || !AuthManager.IsLoggedIn)
            return;
        
        isInitialized = true;
        
        // Устанавливаем Custom Properties игрока с информацией о друзьях
        UpdatePlayerCustomProperties();
        
        // Подключаемся к лобби для поиска друзей
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        
        Debug.Log("PhotonSocialManager: Социальные функции инициализированы");
    }
    
    /// <summary>
    /// Обновляет Custom Properties игрока с информацией о друзьях и профиле
    /// </summary>
    public void UpdatePlayerCustomProperties()
    {
        if (!PhotonNetwork.IsConnected || !AuthManager.IsLoggedIn)
            return;
        
        var user = AuthManager.CurrentUser;
        var props = new ExitGames.Client.Photon.Hashtable();
        
        // Информация о профиле
        props["username"] = user.username;
        props["chips"] = user.chips;
        props["xp"] = user.XP;
        props["level"] = user.Level;
        props["isOnline"] = true;
        
        // Список друзей (для поиска)
        props["friends"] = user.friends != null ? string.Join(",", user.friends) : "";
        
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    
    /// <summary>
    /// Отправляет заявку в друзья через Photon Event
    /// </summary>
    public bool SendFriendRequestViaPhoton(string targetUsername)
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("PhotonSocialManager: Не подключен к Photon");
            return false;
        }
        
        if (!AuthManager.IsLoggedIn)
        {
            Debug.LogWarning("PhotonSocialManager: Пользователь не авторизован");
            return false;
        }
        
        // Сначала проверяем локально
        if (!AuthManager.TrySendFriendRequest(targetUsername, out string error))
        {
            Debug.LogWarning($"PhotonSocialManager: Ошибка локальной проверки: {error}");
            return false;
        }
        
        // Ищем игрока в лобби или комнатах
        var targetPlayer = FindPlayerByUsername(targetUsername);
        
        if (targetPlayer != null)
        {
            // Отправляем событие напрямую игроку
            var eventData = new ExitGames.Client.Photon.Hashtable();
            eventData["from"] = AuthManager.CurrentUser.username;
            eventData["to"] = targetUsername;
            eventData["timestamp"] = DateTime.Now.Ticks;
            
            RaiseEventOptions options = new RaiseEventOptions();
            options.TargetActors = new int[] { targetPlayer.ActorNumber };
            options.Receivers = ReceiverGroup.Others;
            
            PhotonNetwork.RaiseEvent(EVENT_FRIEND_REQUEST, eventData, options, SendOptions.SendReliable);
            
            Debug.Log($"PhotonSocialManager: Заявка в друзья отправлена {targetUsername} через Photon");
            return true;
        }
        else
        {
            // Игрок не онлайн - сохраняем локально, он получит при следующем подключении
            Debug.Log($"PhotonSocialManager: Игрок {targetUsername} не онлайн, заявка сохранена локально");
            return true;
        }
    }
    
    /// <summary>
    /// Принимает заявку в друзья и уведомляет отправителя через Photon
    /// </summary>
    public void AcceptFriendRequestViaPhoton(string requesterUsername)
    {
        if (!PhotonNetwork.IsConnected || !AuthManager.IsLoggedIn)
            return;
        
        if (!AuthManager.TryAcceptFriendRequest(requesterUsername, out string error))
        {
            Debug.LogWarning($"PhotonSocialManager: Ошибка принятия заявки: {error}");
            return;
        }
        
        // Обновляем Custom Properties
        UpdatePlayerCustomProperties();
        
        // Уведомляем отправителя заявки
        var requesterPlayer = FindPlayerByUsername(requesterUsername);
        if (requesterPlayer != null)
        {
            var eventData = new ExitGames.Client.Photon.Hashtable();
            eventData["from"] = AuthManager.CurrentUser.username;
            eventData["to"] = requesterUsername;
            
            RaiseEventOptions options = new RaiseEventOptions();
            options.TargetActors = new int[] { requesterPlayer.ActorNumber };
            options.Receivers = ReceiverGroup.Others;
            
            PhotonNetwork.RaiseEvent(EVENT_FRIEND_REQUEST_ACCEPTED, eventData, options, SendOptions.SendReliable);
        }
    }
    
    /// <summary>
    /// Отправляет инвайт к столу другу через Photon
    /// </summary>
    public bool SendTableInviteViaPhoton(TableInfo table, string friendUsername)
    {
        if (!PhotonNetwork.IsConnected || !AuthManager.IsLoggedIn)
            return false;
        
        if (table == null || string.IsNullOrEmpty(friendUsername))
            return false;
        
        var friendPlayer = FindPlayerByUsername(friendUsername);
        if (friendPlayer == null)
        {
            Debug.LogWarning($"PhotonSocialManager: Друг {friendUsername} не онлайн");
            return false;
        }
        
        var eventData = new ExitGames.Client.Photon.Hashtable();
        eventData["tableId"] = table.tableId;
        eventData["tableName"] = table.tableName;
        eventData["creatorId"] = AuthManager.CurrentUser.username;
        eventData["creatorUsername"] = AuthManager.CurrentUser.username;
        eventData["smallBlind"] = table.smallBlind;
        eventData["bigBlind"] = table.bigBlind;
        eventData["maxSeats"] = table.maxSeats;
        eventData["invitedUsername"] = friendUsername;
        
        RaiseEventOptions options = new RaiseEventOptions();
        options.TargetActors = new int[] { friendPlayer.ActorNumber };
        options.Receivers = ReceiverGroup.Others;
        
        PhotonNetwork.RaiseEvent(EVENT_TABLE_INVITE, eventData, options, SendOptions.SendReliable);
        
        Debug.Log($"PhotonSocialManager: Инвайт к столу '{table.tableName}' отправлен {friendUsername}");
        return true;
    }
    
    /// <summary>
    /// Получает список друзей онлайн
    /// </summary>
    public List<OnlineFriendInfo> GetOnlineFriends()
    {
        return onlineFriends.Values.ToList();
    }
    
    /// <summary>
    /// Проверяет, онлайн ли друг
    /// </summary>
    public bool IsFriendOnline(string username)
    {
        return onlineFriends.ContainsKey(username) && onlineFriends[username].isOnline;
    }
    
    /// <summary>
    /// Находит игрока по username в лобби или комнатах
    /// </summary>
    private Photon.Realtime.Player FindPlayerByUsername(string username)
    {
        // Проверяем игроков в текущей комнате
        if (PhotonNetwork.InRoom)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (string.Equals(player.NickName, username, StringComparison.OrdinalIgnoreCase))
                {
                    return player;
                }
            }
        }
        
        // Проверяем игроков в лобби (через Custom Properties)
        // Примечание: Photon не предоставляет прямой доступ к списку всех игроков в лобби
        // Нужно использовать FindFriends или другой механизм
        
        return null;
    }
    
    /// <summary>
    /// Обработка Photon Events
    /// </summary>
    private void OnPhotonEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        var eventData = photonEvent.CustomData as ExitGames.Client.Photon.Hashtable;
        
        if (eventData == null)
            return;
        
        switch (eventCode)
        {
            case EVENT_FRIEND_REQUEST:
                HandleFriendRequestEvent(eventData);
                break;
            case EVENT_FRIEND_REQUEST_ACCEPTED:
                HandleFriendRequestAcceptedEvent(eventData);
                break;
            case EVENT_FRIEND_REQUEST_DECLINED:
                HandleFriendRequestDeclinedEvent(eventData);
                break;
            case EVENT_FRIEND_REMOVED:
                HandleFriendRemovedEvent(eventData);
                break;
            case EVENT_TABLE_INVITE:
                HandleTableInviteEvent(eventData);
                break;
            case EVENT_TABLE_INVITE_ACCEPTED:
                HandleTableInviteAcceptedEvent(eventData);
                break;
            case EVENT_TABLE_INVITE_DECLINED:
                HandleTableInviteDeclinedEvent(eventData);
                break;
            case EVENT_PROFILE_UPDATE:
                HandleProfileUpdateEvent(eventData);
                break;
        }
    }
    
    private void HandleFriendRequestEvent(ExitGames.Client.Photon.Hashtable eventData)
    {
        string from = eventData["from"] as string;
        if (string.IsNullOrEmpty(from))
            return;
        
        // Сохраняем заявку локально
        if (AuthManager.TrySendFriendRequest(from, out string error))
        {
            OnFriendRequestReceived?.Invoke(from);
            Debug.Log($"PhotonSocialManager: Получена заявка в друзья от {from}");
        }
    }
    
    private void HandleFriendRequestAcceptedEvent(ExitGames.Client.Photon.Hashtable eventData)
    {
        string from = eventData["from"] as string;
        if (string.IsNullOrEmpty(from))
            return;
        
        // Принимаем заявку локально (если она есть)
        if (AuthManager.TryAcceptFriendRequest(from, out string error))
        {
            UpdatePlayerCustomProperties();
            OnFriendRequestAccepted?.Invoke(from);
            Debug.Log($"PhotonSocialManager: Заявка в друзья принята {from}");
        }
    }
    
    private void HandleFriendRequestDeclinedEvent(ExitGames.Client.Photon.Hashtable eventData)
    {
        string from = eventData["from"] as string;
        if (string.IsNullOrEmpty(from))
            return;
        
        // Отклоняем заявку локально
        AuthManager.TryDeclineFriendRequest(from, out string error);
        Debug.Log($"PhotonSocialManager: Заявка в друзья отклонена {from}");
    }
    
    private void HandleFriendRemovedEvent(ExitGames.Client.Photon.Hashtable eventData)
    {
        string username = eventData["username"] as string;
        if (string.IsNullOrEmpty(username))
            return;
        
        // Удаляем друга локально
        AuthManager.TryRemoveFriend(username, out string error);
        UpdatePlayerCustomProperties();
        OnFriendRemoved?.Invoke(username);
        Debug.Log($"PhotonSocialManager: Друг удален {username}");
    }
    
    private void HandleTableInviteEvent(ExitGames.Client.Photon.Hashtable eventData)
    {
        try
        {
            string tableId = eventData["tableId"] as string;
            string tableName = eventData["tableName"] as string;
            string creatorId = eventData["creatorId"] as string;
            string creatorUsername = eventData["creatorUsername"] as string;
            string invitedUsername = eventData["invitedUsername"] as string;
            
            if (string.IsNullOrEmpty(tableId) || string.IsNullOrEmpty(creatorUsername))
                return;
            
            TableInvite invite = new TableInvite(
                tableId,
                tableName ?? "Стол",
                creatorId,
                creatorUsername,
                invitedUsername ?? AuthManager.CurrentUser?.username ?? "",
                AuthManager.CurrentUser?.username ?? ""
            );
            
            // Добавляем инвайт локально
            TableListController.AddInvite(invite);
            
            OnTableInviteReceived?.Invoke(invite);
            Debug.Log($"PhotonSocialManager: Получен инвайт к столу '{tableName}' от {creatorUsername}");
        }
        catch (Exception e)
        {
            Debug.LogError($"PhotonSocialManager: Ошибка обработки инвайта к столу: {e.Message}");
        }
    }
    
    private void HandleTableInviteAcceptedEvent(ExitGames.Client.Photon.Hashtable eventData)
    {
        // Обработка принятия инвайта (если нужно уведомить создателя стола)
        Debug.Log("PhotonSocialManager: Инвайт к столу принят");
    }
    
    private void HandleTableInviteDeclinedEvent(ExitGames.Client.Photon.Hashtable eventData)
    {
        // Обработка отклонения инвайта
        Debug.Log("PhotonSocialManager: Инвайт к столу отклонен");
    }
    
    private void HandleProfileUpdateEvent(ExitGames.Client.Photon.Hashtable eventData)
    {
        // Обновление профиля друга (если нужно)
        Debug.Log("PhotonSocialManager: Профиль друга обновлен");
    }
    
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        UpdateOnlineFriendsList();
    }
    
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        UpdateOnlineFriendsList();
    }
    
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Обновляем информацию о друзьях при изменении их свойств
        if (changedProps.ContainsKey("username"))
        {
            UpdateOnlineFriendsList();
        }
    }
    
    /// <summary>
    /// Обновляет список друзей онлайн на основе игроков в комнате и лобби
    /// </summary>
    private void UpdateOnlineFriendsList()
    {
        if (!AuthManager.IsLoggedIn)
            return;
        
        var friends = AuthManager.GetFriends();
        onlineFriends.Clear();
        
        // Проверяем игроков в текущей комнате
        if (PhotonNetwork.InRoom)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                string playerUsername = player.NickName;
                if (friends.Contains(playerUsername, StringComparer.OrdinalIgnoreCase))
                {
                    onlineFriends[playerUsername] = new OnlineFriendInfo
                    {
                        username = playerUsername,
                        isOnline = true,
                        isInRoom = true,
                        roomName = PhotonNetwork.CurrentRoom?.Name,
                        chips = GetPlayerProperty<int>(player, "chips", 0),
                        level = GetPlayerProperty<int>(player, "level", 1)
                    };
                }
            }
        }
        
        // Уведомляем UI об обновлении
        OnFriendsOnlineStatusUpdated?.Invoke(onlineFriends.Values.ToList());
    }
    
    private T GetPlayerProperty<T>(Photon.Realtime.Player player, string key, T defaultValue)
    {
        if (player.CustomProperties != null && player.CustomProperties.ContainsKey(key))
        {
            return (T)player.CustomProperties[key];
        }
        return defaultValue;
    }
    
    public override void OnJoinedLobby()
    {
        Debug.Log("PhotonSocialManager: Присоединился к лобби");
        UpdateOnlineFriendsList();
    }
    
    public override void OnLeftLobby()
    {
        Debug.Log("PhotonSocialManager: Покинул лобби");
    }
}

/// <summary>
/// Информация о друге онлайн
/// </summary>
[Serializable]
public class OnlineFriendInfo
{
    public string username;
    public bool isOnline;
    public bool isInRoom;
    public string roomName;
    public int chips;
    public int level;
}

