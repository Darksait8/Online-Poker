using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Менеджер для синхронизации онлайн-игры между клиентами
/// Интегрирует PokerClient с GameManager
/// </summary>
public class NetworkGameManager : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PokerClient pokerClient;
    [SerializeField] private GameManager gameManager;
    
    [Header("Настройки")]
    [SerializeField] private bool enableOnlineMode = false;
    
    private bool isOnlineGame = false;
    private Dictionary<string, Player> networkPlayers = new Dictionary<string, Player>();
    
    private void Awake()
    {
        if (pokerClient == null)
            pokerClient = FindObjectOfType<PokerClient>();
        
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }
    
    private void OnEnable()
    {
        if (pokerClient != null)
        {
            pokerClient.OnConnectionStatusChanged += HandleConnectionStatusChanged;
            pokerClient.OnPlayersListUpdated += HandlePlayersListUpdated;
            pokerClient.OnHandStateChanged += HandleHandStateChanged;
            pokerClient.OnPlayerAction += HandlePlayerAction;
        }
    }
    
    private void OnDisable()
    {
        if (pokerClient != null)
        {
            pokerClient.OnConnectionStatusChanged -= HandleConnectionStatusChanged;
            pokerClient.OnPlayersListUpdated -= HandlePlayersListUpdated;
            pokerClient.OnHandStateChanged -= HandleHandStateChanged;
            pokerClient.OnPlayerAction -= HandlePlayerAction;
        }
    }
    
    /// <summary>
    /// Включает онлайн-режим и подключается к серверу
    /// </summary>
    public void EnableOnlineMode(string serverHost = "localhost", int serverPort = 8888)
    {
        if (pokerClient == null)
        {
            Debug.LogError("❌ NetworkGameManager: PokerClient не найден!");
            return;
        }
        
        enableOnlineMode = true;
        
        // Обновляем настройки клиента через рефлексию
        var clientType = typeof(PokerClient);
        var hostField = clientType.GetField("serverHost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var portField = clientType.GetField("serverPort", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        hostField?.SetValue(pokerClient, serverHost);
        portField?.SetValue(pokerClient, serverPort);
        
        // Подключаемся к серверу
        pokerClient.ConnectToServer();
    }
    
    /// <summary>
    /// Отключает онлайн-режим
    /// </summary>
    public void DisableOnlineMode()
    {
        enableOnlineMode = false;
        isOnlineGame = false;
        
        if (pokerClient != null && pokerClient.IsConnected())
        {
            pokerClient.DisconnectFromServer();
        }
    }
    
    /// <summary>
    /// Отправляет действие игрока на сервер
    /// </summary>
    public void SendPlayerAction(string action, int amount = 0)
    {
        if (!enableOnlineMode || !isOnlineGame || pokerClient == null || !pokerClient.IsConnected())
        {
            Debug.LogWarning("⚠️ Нельзя отправить действие: онлайн-режим не активен");
            return;
        }
        
        pokerClient.SendPlayerAction(action, amount);
    }
    
    private void HandleConnectionStatusChanged(string status)
    {
        Debug.Log($"📡 Статус подключения: {status}");
        
        if (status.Contains("Подключен") || status.Contains("Connected"))
        {
            isOnlineGame = true;
        }
        else if (status.Contains("Отключен") || status.Contains("Disconnected"))
        {
            isOnlineGame = false;
        }
    }
    
    private void HandlePlayersListUpdated(string[] playerNames)
    {
        Debug.Log($"👥 Обновлен список игроков: {string.Join(", ", playerNames)}");
        
        // Синхронизируем список игроков с GameManager
        if (gameManager != null && isOnlineGame)
        {
            // Удаляем игроков, которых больше нет в списке
            var currentPlayers = gameManager.Players.ToList();
            foreach (var player in currentPlayers)
            {
                if (!playerNames.Contains(player.Name))
                {
                    gameManager.RemovePlayer(player.Name);
                }
            }
            
            // Добавляем новых игроков
            foreach (var playerName in playerNames)
            {
                if (!currentPlayers.Any(p => p.Name == playerName))
                {
                    gameManager.AddPlayer(playerName, 1000);
                }
            }
        }
    }
    
    private void HandleHandStateChanged(bool handActive)
    {
        Debug.Log($"🃏 Состояние раздачи изменилось: {(handActive ? "Активна" : "Неактивна")}");
        
        if (handActive && gameManager != null)
        {
            // Начинаем новую раздачу
            gameManager.StartNewHand();
        }
    }
    
    private void HandlePlayerAction(string playerName, string action, int amount)
    {
        Debug.Log($"🎯 Действие игрока: {playerName} - {action} (сумма: {amount})");
        
        // Синхронизируем действие с GameManager
        if (gameManager != null && isOnlineGame)
        {
            // Находим игрока и обрабатываем его действие
            var player = gameManager.Players.FirstOrDefault(p => p.Name == playerName);
            if (player != null)
            {
                // Обновляем состояние игрока в зависимости от действия
                switch (action.ToLower())
                {
                    case "fold":
                        player.Status = PlayerStatus.Folded;
                        break;
                    case "call":
                    case "check":
                    case "raise":
                    case "bet":
                        // Обновляем ставку игрока
                        player.CurrentBet = amount;
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// Вызывается из ActionPanelController для отправки действий игрока
    /// </summary>
    public void OnPlayerFold()
    {
        SendPlayerAction("fold");
    }
    
    public void OnPlayerCall()
    {
        SendPlayerAction("call");
    }
    
    public void OnPlayerCheck()
    {
        SendPlayerAction("check");
    }
    
    public void OnPlayerRaise(int amount)
    {
        SendPlayerAction("raise", amount);
    }
    
    public bool IsOnlineModeActive()
    {
        return enableOnlineMode && isOnlineGame && pokerClient != null && pokerClient.IsConnected();
    }
}

