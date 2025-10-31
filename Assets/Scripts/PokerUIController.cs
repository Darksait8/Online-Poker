using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Простой UI контроллер для покерного клиента
/// </summary>
public class PokerUIController : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI playersListText;
    [SerializeField] private TextMeshProUGUI gameStateText;
    [SerializeField] private TextMeshProUGUI actionLogText;
    
    [Header("Кнопки действий")]
    [SerializeField] private Button foldButton;
    [SerializeField] private Button callButton;
    [SerializeField] private Button raiseButton;
    [SerializeField] private Button checkButton;
    
    [Header("Поле ввода")]
    [SerializeField] private TMP_InputField raiseAmountInput;
    
    [Header("Ссылки")]
    [SerializeField] private PokerClient pokerClient;
    
    private void Start()
    {
        // Подписываемся на события клиента
        if (pokerClient != null)
        {
            pokerClient.OnConnectionStatusChanged += UpdateConnectionStatus;
            pokerClient.OnPlayersListUpdated += UpdatePlayersList;
            pokerClient.OnHandStateChanged += UpdateGameState;
            pokerClient.OnPlayerAction += LogPlayerAction;
        }
        
        // Настраиваем кнопки
        SetupButtons();
        
        // Инициализируем UI
        UpdateConnectionStatus("Инициализация...");
        UpdatePlayersList(new string[0]);
        UpdateGameState(false);
    }
    
    private void SetupButtons()
    {
        if (foldButton != null)
            foldButton.onClick.AddListener(() => pokerClient?.Fold());
            
        if (callButton != null)
            callButton.onClick.AddListener(() => pokerClient?.Call());
            
        if (raiseButton != null)
            raiseButton.onClick.AddListener(RaiseAction);
            
        if (checkButton != null)
            checkButton.onClick.AddListener(() => pokerClient?.Check());
    }
    
    private void RaiseAction()
    {
        if (raiseAmountInput != null && int.TryParse(raiseAmountInput.text, out int amount))
        {
            pokerClient?.Raise(amount);
        }
        else
        {
            Debug.LogWarning("⚠️ Неверная сумма для рейза");
        }
    }
    
    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = $"Статус: {status}";
            
            // Меняем цвет в зависимости от статуса
            if (status.Contains("Подключен"))
                connectionStatusText.color = Color.green;
            else if (status.Contains("Ошибка"))
                connectionStatusText.color = Color.red;
            else
                connectionStatusText.color = Color.yellow;
        }
    }
    
    private void UpdatePlayersList(string[] players)
    {
        if (playersListText != null)
        {
            if (players.Length == 0)
            {
                playersListText.text = "Игроки: Нет подключенных игроков";
            }
            else
            {
                playersListText.text = $"Игроки ({players.Length}): {string.Join(", ", players)}";
            }
        }
    }
    
    private void UpdateGameState(bool handActive)
    {
        if (gameStateText != null)
        {
            gameStateText.text = handActive ? "Состояние: Игра идет" : "Состояние: Ожидание игроков";
            gameStateText.color = handActive ? Color.green : Color.yellow;
        }
        
        // Включаем/выключаем кнопки действий
        bool canAct = handActive && pokerClient != null && pokerClient.IsConnected();
        
        if (foldButton != null) foldButton.interactable = canAct;
        if (callButton != null) callButton.interactable = canAct;
        if (raiseButton != null) raiseButton.interactable = canAct;
        if (checkButton != null) checkButton.interactable = canAct;
        if (raiseAmountInput != null) raiseAmountInput.interactable = canAct;
    }
    
    private void LogPlayerAction(string playerName, string action, int amount)
    {
        if (actionLogText != null)
        {
            string logEntry = $"{playerName}: {action}";
            if (amount > 0)
                logEntry += $" ({amount})";
            
            logEntry += "\n";
            
            // Добавляем новую запись в начало
            actionLogText.text = logEntry + actionLogText.text;
            
            // Ограничиваем количество записей
            string[] lines = actionLogText.text.Split('\n');
            if (lines.Length > 10)
            {
                string[] newLines = new string[10];
                System.Array.Copy(lines, 0, newLines, 0, 10);
                actionLogText.text = string.Join("\n", newLines);
            }
        }
        
        Debug.Log($"🎯 {playerName}: {action} (сумма: {amount})");
    }
    
    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        if (pokerClient != null)
        {
            pokerClient.RequestGameState();
        }
    }
    
    [ContextMenu("Reconnect")]
    public void Reconnect()
    {
        if (pokerClient != null)
        {
            pokerClient.DisconnectFromServer();
            pokerClient.ConnectToServer();
        }
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (pokerClient != null)
        {
            pokerClient.OnConnectionStatusChanged -= UpdateConnectionStatus;
            pokerClient.OnPlayersListUpdated -= UpdatePlayersList;
            pokerClient.OnHandStateChanged -= UpdateGameState;
            pokerClient.OnPlayerAction -= LogPlayerAction;
        }
    }
}