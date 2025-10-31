using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Полноценный UI контроллер для покерной игры
/// </summary>
public class PokerUIControllerFull : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI playersListText;
    [SerializeField] private TextMeshProUGUI gamePhaseText;
    [SerializeField] private TextMeshProUGUI potText;
    [SerializeField] private TextMeshProUGUI currentBetText;
    [SerializeField] private TextMeshProUGUI actionLogText;
    
    [Header("Кнопки действий")]
    [SerializeField] private Button foldButton;
    [SerializeField] private Button callButton;
    [SerializeField] private Button raiseButton;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button allInButton;
    
    [Header("Поля ввода")]
    [SerializeField] private TMP_InputField raiseAmountInput;
    [SerializeField] private Slider raiseSlider;
    
    [Header("Информация о блайндах")]
    [SerializeField] private TextMeshProUGUI smallBlindText;
    [SerializeField] private TextMeshProUGUI bigBlindText;
    
    [Header("Ссылки")]
    [SerializeField] private PokerClientFull pokerClient;
    
    private GameState currentGameState;
    private List<PlayerInfo> currentPlayers = new List<PlayerInfo>();
    private int minRaise = 20;
    private int maxRaise = 1000;
    
    private void Start()
    {
        // Подписываемся на события клиента
        if (pokerClient != null)
        {
            pokerClient.OnConnectionStatusChanged += UpdateConnectionStatus;
            pokerClient.OnPlayersListUpdated += UpdatePlayersList;
            pokerClient.OnGameStateChanged += UpdateGameState;
            pokerClient.OnPhaseChanged += UpdatePhase;
            pokerClient.OnHandStart += OnHandStart;
            pokerClient.OnHandEnd += OnHandEnd;
        }
        
        // Настраиваем кнопки
        SetupButtons();
        
        // Настраиваем слайдер
        SetupSlider();
        
        // Инициализируем UI
        InitializeUI();
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
            
        if (allInButton != null)
            allInButton.onClick.AddListener(AllInAction);
    }
    
    private void SetupSlider()
    {
        if (raiseSlider != null)
        {
            raiseSlider.minValue = minRaise;
            raiseSlider.maxValue = maxRaise;
            raiseSlider.value = minRaise;
            raiseSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }
    
    private void InitializeUI()
    {
        UpdateConnectionStatus("Инициализация...");
        UpdatePlayersList(new List<PlayerInfo>());
        UpdateGameState(new GameState());
        UpdatePhase("waiting");
    }
    
    private void RaiseAction()
    {
        int amount = minRaise;
        
        if (raiseAmountInput != null && int.TryParse(raiseAmountInput.text, out int inputAmount))
        {
            amount = Mathf.Clamp(inputAmount, minRaise, maxRaise);
        }
        else if (raiseSlider != null)
        {
            amount = Mathf.RoundToInt(raiseSlider.value);
        }
        
        pokerClient?.Raise(amount);
    }
    
    private void AllInAction()
    {
        // Находим текущего игрока и его стек
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null)
        {
            pokerClient?.Raise(currentPlayer.stack);
        }
    }
    
    private void OnSliderValueChanged(float value)
    {
        if (raiseAmountInput != null)
        {
            raiseAmountInput.text = Mathf.RoundToInt(value).ToString();
        }
    }
    
    private PlayerInfo GetCurrentPlayer()
    {
        // Простая логика - возвращаем первого игрока
        // В реальной игре здесь должна быть логика определения текущего игрока
        return currentPlayers.Count > 0 ? currentPlayers[0] : null;
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
    
    private void UpdatePlayersList(List<PlayerInfo> players)
    {
        currentPlayers = players;
        
        if (playersListText != null)
        {
            if (players.Count == 0)
            {
                playersListText.text = "Игроки: Нет подключенных игроков";
            }
            else
            {
                var playerTexts = new List<string>();
                foreach (var player in players)
                {
                    string status = player.folded ? " (сбросил)" : "";
                    playerTexts.Add($"{player.name}: {player.stack} фишек{status}");
                }
                playersListText.text = $"Игроки ({players.Count}):\n{string.Join("\n", playerTexts)}";
            }
        }
    }
    
    private void UpdateGameState(GameState gameState)
    {
        currentGameState = gameState;
        
        if (potText != null)
            potText.text = $"Банк: {gameState.pot}";
            
        if (currentBetText != null)
            currentBetText.text = $"Текущая ставка: {gameState.currentBet}";
            
        if (smallBlindText != null)
            smallBlindText.text = $"Small Blind: {gameState.smallBlind}";
            
        if (bigBlindText != null)
            bigBlindText.text = $"Big Blind: {gameState.bigBlind}";
        
        // Обновляем доступность кнопок
        UpdateButtonStates();
    }
    
    private void UpdatePhase(string phase)
    {
        if (gamePhaseText != null)
        {
            string phaseText = GetPhaseText(phase);
            gamePhaseText.text = $"Фаза: {phaseText}";
            gamePhaseText.color = GetPhaseColor(phase);
        }
    }
    
    private string GetPhaseText(string phase)
    {
        switch (phase)
        {
            case "waiting": return "Ожидание игроков";
            case "preflop": return "Префлоп";
            case "flop": return "Флоп";
            case "turn": return "Тёрн";
            case "river": return "Ривер";
            case "showdown": return "Шоудаун";
            default: return phase;
        }
    }
    
    private Color GetPhaseColor(string phase)
    {
        switch (phase)
        {
            case "waiting": return Color.yellow;
            case "preflop": return Color.blue;
            case "flop": return Color.green;
            case "turn": return Color.cyan;
            case "river": return Color.magenta;
            case "showdown": return Color.red;
            default: return Color.white;
        }
    }
    
    private void UpdateButtonStates()
    {
        bool canAct = currentGameState != null && 
                     currentGameState.phase != "waiting" && 
                     currentGameState.phase != "showdown" &&
                     pokerClient != null && 
                     pokerClient.IsConnected();
        
        if (foldButton != null) foldButton.interactable = canAct;
        if (callButton != null) callButton.interactable = canAct;
        if (raiseButton != null) raiseButton.interactable = canAct;
        if (checkButton != null) checkButton.interactable = canAct;
        if (allInButton != null) allInButton.interactable = canAct;
        if (raiseAmountInput != null) raiseAmountInput.interactable = canAct;
        if (raiseSlider != null) raiseSlider.interactable = canAct;
        
        // Обновляем текст кнопки Call/Check
        if (callButton != null && currentGameState != null)
        {
            var callText = callButton.GetComponentInChildren<TextMeshProUGUI>();
            if (callText != null)
            {
                if (currentGameState.currentBet > 0)
                {
                    callText.text = $"Call ({currentGameState.currentBet})";
                }
                else
                {
                    callText.text = "Check";
                }
            }
        }
    }
    
    private void OnHandStart()
    {
        LogAction("🃏", "Началась новая раздача");
        
        // Обновляем минимальный рейз
        if (currentGameState != null)
        {
            minRaise = currentGameState.bigBlind;
            maxRaise = GetCurrentPlayer()?.stack ?? 1000;
            
            if (raiseSlider != null)
            {
                raiseSlider.minValue = minRaise;
                raiseSlider.maxValue = maxRaise;
                raiseSlider.value = minRaise;
            }
        }
    }
    
    private void OnHandEnd()
    {
        LogAction("🔄", "Раздача завершена");
    }
    
    private void LogAction(string icon, string message)
    {
        if (actionLogText != null)
        {
            string logEntry = $"{icon} {message}\n";
            
            // Добавляем новую запись в начало
            actionLogText.text = logEntry + actionLogText.text;
            
            // Ограничиваем количество записей
            string[] lines = actionLogText.text.Split('\n');
            if (lines.Length > 15)
            {
                string[] newLines = new string[15];
                System.Array.Copy(lines, 0, newLines, 0, 15);
                actionLogText.text = string.Join("\n", newLines);
            }
        }
        
        Debug.Log($"{icon} {message}");
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
            pokerClient.OnGameStateChanged -= UpdateGameState;
            pokerClient.OnPhaseChanged -= UpdatePhase;
            pokerClient.OnHandStart -= OnHandStart;
            pokerClient.OnHandEnd -= OnHandEnd;
        }
    }
}
