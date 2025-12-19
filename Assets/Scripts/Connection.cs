using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

public class Connection : MonoBehaviourPunCallbacks
{
    [SerializeField] private string sceneName = "Main"; // Сцена игры по умолчанию 
    [SerializeField] private float connectionTimeout = 10f;
    
    private bool isConnecting = false;
    private float connectionStartTime;
    
    void Start()
    {
        // Создаем PhotonSocialManager если его еще нет
        PhotonSocialManager.EnsureInstance();
        
        // Устанавливаем никнейм игрока из AuthManager
        if (AuthManager.IsLoggedIn && AuthManager.CurrentUser != null)
        {
            PhotonNetwork.NickName = AuthManager.CurrentUser.username;
        }
        else
        {
            PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);
        }
        
        if (!PhotonNetwork.IsConnected)
        {
            isConnecting = true;
            connectionStartTime = Time.time;
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            // Уже подключены - переходим к следующему шагу
            HandleConnectionReady();
        }
    }
    
    void Update()
    {
        // Проверка таймаута подключения
        if (isConnecting && Time.time - connectionStartTime > connectionTimeout)
        {
            Debug.LogError("Connection timeout!");
            isConnecting = false;
            // Можно показать сообщение об ошибке пользователю
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server!");
        isConnecting = false;
        
        // Инициализируем социальные функции через Photon (неблокирующе, в фоне)
        if (PhotonSocialManager.Instance != null)
        {
            // Запускаем в фоне, не ждем завершения
            System.Threading.Tasks.Task.Run(() =>
            {
                UnityEngine.Debug.Log("Обновление custom properties в фоне...");
                PhotonSocialManager.Instance.UpdatePlayerCustomProperties();
            });
        }
        
        // Если это онлайн стол, создаем или присоединяемся к комнате
        if (TableRuntimeConfig.HasConfig && TableRuntimeConfig.IsOnlineTable)
        {
            JoinOrCreateRoom();
        }
        else
        {
            // Локальная игра - просто переходим на сцену
            HandleConnectionReady();
        }
    }
    
    private void JoinOrCreateRoom()
    {
        string roomName = TableRuntimeConfig.TableId;
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "Table_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        }
        
        // Настройки комнаты
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)TableRuntimeConfig.MaxSeats;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        
        // Кастомные свойства комнаты
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["smallBlind"] = TableRuntimeConfig.SmallBlind;
        roomProps["bigBlind"] = TableRuntimeConfig.BigBlind;
        roomProps["tableName"] = TableRuntimeConfig.TableName;
        roomOptions.CustomRoomProperties = roomProps;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "smallBlind", "bigBlind", "tableName" };
        
        // Пытаемся присоединиться к существующей комнате или создать новую
        Debug.Log($"Attempting to join or create room: {roomName}");
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log($"Successfully joined room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        
        // Переходим на игровую сцену
        HandleConnectionReady();
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to join room: {message} (Code: {returnCode})");
        // Пытаемся создать новую комнату
        CreateNewRoom();
    }
    
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to create room: {message} (Code: {returnCode})");
        // Fallback: переходим на сцену без онлайн подключения
        HandleConnectionReady();
    }
    
    private void CreateNewRoom()
    {
        string roomName = "Table_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)TableRuntimeConfig.MaxSeats;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["smallBlind"] = TableRuntimeConfig.SmallBlind;
        roomProps["bigBlind"] = TableRuntimeConfig.BigBlind;
        roomProps["tableName"] = TableRuntimeConfig.TableName;
        roomOptions.CustomRoomProperties = roomProps;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "smallBlind", "bigBlind", "tableName" };
        
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    private void HandleConnectionReady()
    {
        // Если это онлайн стол, загружаем игровую сцену
        if (TableRuntimeConfig.HasConfig && TableRuntimeConfig.IsOnlineTable)
        {
            SceneManager.LoadScene("Main");
        }
        else if (!string.IsNullOrEmpty(sceneName))
        {
            // Иначе загружаем указанную сцену
            SceneManager.LoadScene(sceneName);
        }
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected from Photon: {cause}");
        isConnecting = false;
    }
}