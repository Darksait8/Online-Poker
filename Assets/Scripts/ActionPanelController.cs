using UnityEngine;
using UnityEngine.UI;

public class ActionPanelController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button foldButton;
    [SerializeField] private Button checkCallButton;
    [SerializeField] private Button betRaiseButton;
    [SerializeField] private Slider betSlider;
    [SerializeField] private Text betValueText;

    [Header("Gameplay")]
    [SerializeField] private GameStateMachine game; // Старая система (для совместимости)
    [SerializeField] private GameManager gameManager; // Новая система
    [SerializeField] private int minBet = 100;
    [SerializeField] private int maxBet = 1000;
    [SerializeField] private int betStep = 50;
    
    // Состояние панели
    private bool isEnabled = false;
    private Player currentPlayer;

    private void Awake()
    {
        if (foldButton != null) foldButton.onClick.AddListener(OnFold);
        if (checkCallButton != null) checkCallButton.onClick.AddListener(OnCheckCall);
        if (betRaiseButton != null) betRaiseButton.onClick.AddListener(OnBetRaise);

        SetupSlider(minBet, maxBet, betStep);
        
        // Находим GameManager если не назначен
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
            
        // Подписываемся на события GameManager
        if (gameManager != null)
        {
            gameManager.OnPlayerTurn += OnPlayerTurn;
            gameManager.OnPhaseChanged += OnPhaseChanged;
        }
        
    // Не скрываем панель автоматически, чтобы она была видна при старте
    }

    private void OnDestroy()
    {
        if (foldButton != null) foldButton.onClick.RemoveListener(OnFold);
        if (checkCallButton != null) checkCallButton.onClick.RemoveListener(OnCheckCall);
        if (betRaiseButton != null) betRaiseButton.onClick.RemoveListener(OnBetRaise);
        if (betSlider != null) betSlider.onValueChanged.RemoveAllListeners();
    }

    public void SetupSlider(int min, int max, int step)
    {
        minBet = Mathf.Max(1, min);
        maxBet = Mathf.Max(minBet, max);
        betStep = Mathf.Max(1, step);

        if (betSlider == null) return;
        betSlider.minValue = minBet;
        betSlider.maxValue = maxBet;
        betSlider.wholeNumbers = true;
        betSlider.value = minBet;
        betSlider.onValueChanged.AddListener(UpdateBetLabel);
        UpdateBetLabel(betSlider.value);
    }

    private int GetRoundedBet()
    {
        if (betSlider == null) return minBet;
        int raw = Mathf.RoundToInt(betSlider.value);
        int stepped = ((raw - minBet + betStep - 1) / betStep) * betStep + minBet;
        return Mathf.Clamp(stepped, minBet, maxBet);
    }

    private void UpdateBetLabel(float _)
    {
        if (betValueText != null)
            betValueText.text = GetRoundedBet().ToString();
    }

    /// <summary>
    /// Включает/выключает панель действий
    /// </summary>
    public void SetPanelEnabled(bool enabled)
    {
        isEnabled = enabled;
        gameObject.SetActive(enabled);
        
        if (enabled)
        {
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// Обновляет состояние кнопок в зависимости от игровой ситуации
    /// </summary>
    private void UpdateButtonStates()
    {
        if (gameManager == null || currentPlayer == null)
        {
            SetAllButtonsEnabled(false);
            return;
        }

        // Fold всегда доступен
        if (foldButton != null)
            foldButton.interactable = true;

        // Check/Call логика
        if (checkCallButton != null)
        {
            bool canCheck = gameManager.CurrentBet == 0 || currentPlayer.CurrentBet == gameManager.CurrentBet;
            bool canCall = gameManager.CurrentBet > currentPlayer.CurrentBet && currentPlayer.Stack > 0;
            
            checkCallButton.interactable = canCheck || canCall;
            
            // Обновляем текст кнопки
            var buttonText = checkCallButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                if (canCheck && gameManager.CurrentBet == currentPlayer.CurrentBet)
                {
                    buttonText.text = "Check";
                }
                else if (canCall)
                {
                    int callAmount = gameManager.CurrentBet - currentPlayer.CurrentBet;
                    buttonText.text = $"Call {callAmount}";
                }
                else
                {
                    buttonText.text = "Check";
                }
            }
        }

        // Bet/Raise логика
        if (betRaiseButton != null)
        {
            bool canBetRaise = currentPlayer.Stack > 0;
            betRaiseButton.interactable = canBetRaise;
            
            // Обновляем текст кнопки
            var buttonText = betRaiseButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                if (gameManager.CurrentBet == 0)
                {
                    buttonText.text = "Bet";
                }
                else
                {
                    buttonText.text = "Raise";
                }
            }
        }

        // Обновляем слайдер
        UpdateSliderRange();
    }

    /// <summary>
    /// Обновляет диапазон слайдера в зависимости от игровой ситуации
    /// </summary>
    private void UpdateSliderRange()
    {
        if (betSlider == null || currentPlayer == null || gameManager == null) return;

        int minRaise = gameManager.CurrentBet == 0 ? gameManager.BigBlind : gameManager.CurrentBet * 2;
        int maxRaise = currentPlayer.Stack + currentPlayer.CurrentBet;
        
        // Минимальная ставка не может быть больше стека игрока
        minRaise = Mathf.Min(minRaise, maxRaise);
        
        betSlider.minValue = minRaise;
        betSlider.maxValue = maxRaise;
        betSlider.value = minRaise;
        
        UpdateBetLabel(betSlider.value);
    }

    /// <summary>
    /// Устанавливает доступность всех кнопок
    /// </summary>
    private void SetAllButtonsEnabled(bool enabled)
    {
        if (foldButton != null) foldButton.interactable = enabled;
        if (checkCallButton != null) checkCallButton.interactable = enabled;
        if (betRaiseButton != null) betRaiseButton.interactable = enabled;
        if (betSlider != null) betSlider.interactable = enabled;
    }

    // === ОБРАБОТЧИКИ СОБЫТИЙ GAMEMANAGER ===
    
    private void OnPlayerTurn(Player player)
    {
        currentPlayer = player;
        
        // Панель активна только для текущего игрока
        // TODO: Здесь нужно проверить, что это локальный игрок
        bool isLocalPlayer = true; // Пока что считаем всех игроков локальными
        
        SetPanelEnabled(isLocalPlayer);
        
        Debug.Log($"ActionPanel: {player.Name}'s turn");
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        Debug.Log($"ActionPanel: Phase changed to {phase}");
        
        // Отключаем панель во время шоудауна и завершения раздачи
        if (phase == GamePhase.Showdown || phase == GamePhase.HandComplete)
        {
            SetPanelEnabled(false);
        }
    }

    // === ОБРАБОТЧИКИ КНОПОК ===
    
    private void OnFold()
    {
        if (!isEnabled || currentPlayer == null) return;
        
        // Используем новый GameManager
        if (gameManager != null)
        {
            gameManager.ProcessPlayerAction("fold");
        }
        // Совместимость со старой системой
        else if (game != null)
        {
            game.PlayerAction(PlayerActionType.Fold, 0);
        }
        
        SetPanelEnabled(false);
    }

    private void OnCheckCall()
    {
        if (!isEnabled || currentPlayer == null) return;
        
        if (gameManager != null)
        {
            if (gameManager.CurrentBet == 0 || currentPlayer.CurrentBet == gameManager.CurrentBet)
            {
                gameManager.ProcessPlayerAction("check");
            }
            else
            {
                gameManager.ProcessPlayerAction("call");
            }
        }
        else if (game != null)
        {
            game.PlayerAction(PlayerActionType.CheckOrCall, 0);
        }
        
        SetPanelEnabled(false);
    }

    private void OnBetRaise()
    {
        if (!isEnabled || currentPlayer == null) return;
        
        int betAmount = GetRoundedBet();
        
        if (gameManager != null)
        {
            if (gameManager.CurrentBet == 0)
            {
                gameManager.ProcessPlayerAction("bet", betAmount);
            }
            else
            {
                gameManager.ProcessPlayerAction("raise", betAmount);
            }
        }
        else if (game != null)
        {
            game.PlayerAction(PlayerActionType.BetOrRaise, betAmount);
        }
        
        SetPanelEnabled(false);
    }
}


