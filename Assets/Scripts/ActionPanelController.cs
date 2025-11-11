using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WonderPokerCore;

public class ActionPanelController : MonoBehaviour, IPlayerDecisionProvider
{
    [Header("UI")]
    [SerializeField] private Button foldButton;
    [SerializeField] private Button checkCallButton;
    [SerializeField] private Button betRaiseButton;
    [SerializeField] private Slider betSlider;
    [SerializeField] private Text betValueText;

    [Header("Настройки по умолчанию")]
    [SerializeField] private int betStep = 50;

    [Header("Совместимость")]
    [SerializeField] private GameStateMachine legacyStateMachine;
    [SerializeField] private GameManager gameManager;

    private bool panelEnabled = false;
    private DecisionRequest activeRequest;
    private TaskCompletionSource<PlayerDecision> pendingDecision;
    private CancellationTokenRegistration cancellationRegistration;
    private int callAmount;
    private int minTotalBet;
    private int maxTotalBet;
    private int defaultSliderMin = 0;
    private int defaultSliderMax = 0;

    private void Awake()
    {
        if (foldButton != null) foldButton.onClick.AddListener(OnFoldClicked);
        if (checkCallButton != null) checkCallButton.onClick.AddListener(OnCheckCallClicked);
        if (betRaiseButton != null) betRaiseButton.onClick.AddListener(OnBetRaiseClicked);

        if (betSlider != null)
        {
            betSlider.wholeNumbers = true;
            betSlider.onValueChanged.AddListener(OnSliderChanged);
            defaultSliderMin = Mathf.RoundToInt(betSlider.minValue);
            defaultSliderMax = Mathf.RoundToInt(betSlider.maxValue);
        }

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (gameManager != null)
            gameManager.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        if (foldButton != null) foldButton.onClick.RemoveListener(OnFoldClicked);
        if (checkCallButton != null) checkCallButton.onClick.RemoveListener(OnCheckCallClicked);
        if (betRaiseButton != null) betRaiseButton.onClick.RemoveListener(OnBetRaiseClicked);
        if (betSlider != null) betSlider.onValueChanged.RemoveListener(OnSliderChanged);

        if (gameManager != null)
            gameManager.OnPhaseChanged -= HandlePhaseChanged;

        CancelPendingDecision();
    }

    public void SetupSlider(int min, int max, int step)
    {
        if (step > 0) betStep = step;
        if (betSlider == null) return;

        defaultSliderMin = min;
        defaultSliderMax = Mathf.Max(min, max);
        betSlider.minValue = defaultSliderMin;
        betSlider.maxValue = defaultSliderMax;
        betSlider.value = defaultSliderMin;
        OnSliderChanged(betSlider.value);
    }

    public Task<PlayerDecision> RequestDecisionAsync(DecisionRequest request, CancellationToken cancellationToken)
    {
        CancelPendingDecision();

        activeRequest = request ?? throw new ArgumentNullException(nameof(request));
        ConfigureForCurrentRequest();
        SetPanelEnabled(true);

        pendingDecision = new TaskCompletionSource<PlayerDecision>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(CancelPendingDecision);
        }

        return pendingDecision.Task;
    }

    private void ConfigureForCurrentRequest()
    {
        if (activeRequest == null || activeRequest.Player == null || activeRequest.Table == null)
            return;

        var player = activeRequest.Player;
        var table = activeRequest.Table;

        callAmount = Math.Max(0, table.CurrentBid - player.PlayersCurrentBet);
        int availableTokens = player.TokensCount;
        bool canCheck = callAmount == 0;
        bool canCall = availableTokens > 0 || canCheck;

        int minimumRaiseUnit = table.Settings.BigBlind > 0 ? table.Settings.BigBlind : 1;
        if (table.CurrentBid > 0)
            minimumRaiseUnit = Math.Max(minimumRaiseUnit, table.CurrentBid);

        int maxTotal = callAmount + availableTokens;
        int minRaiseTotal = callAmount + minimumRaiseUnit;

        bool canRaise = availableTokens > Math.Max(0, minRaiseTotal - callAmount);
        if (!canRaise && availableTokens > 0 && table.CurrentBid == 0)
        {
            minRaiseTotal = callAmount + availableTokens;
            canRaise = availableTokens > 0;
        }

        minTotalBet = Mathf.Clamp(minRaiseTotal, callAmount, maxTotal);
        maxTotalBet = maxTotal;

        if (betSlider != null)
        {
            betSlider.gameObject.SetActive(canRaise);
            if (canRaise)
            {
                betSlider.minValue = minTotalBet;
                betSlider.maxValue = Math.Max(minTotalBet, maxTotalBet);
                betSlider.value = betSlider.minValue;
            }
        }

        if (betValueText != null)
        {
            betValueText.text = canRaise ? Mathf.RoundToInt(betSlider.value).ToString() : "-";
        }

        if (foldButton != null)
        {
            foldButton.gameObject.SetActive(true);
            foldButton.interactable = !IsLastStanding(player);
        }

        if (checkCallButton != null)
        {
            checkCallButton.gameObject.SetActive(true);
            checkCallButton.interactable = canCall || canCheck;
            var label = checkCallButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                if (canCheck && callAmount == 0)
                    label.text = "Check";
                else
                {
                    int callDisplay = Math.Min(callAmount, availableTokens + player.PlayersCurrentBet);
                    label.text = callDisplay <= 0 ? "Check" : $"Call {callDisplay}";
                }
            }
        }

        if (betRaiseButton != null)
        {
            betRaiseButton.gameObject.SetActive(canRaise);
            betRaiseButton.interactable = canRaise;
            var label = betRaiseButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = table.CurrentBid == 0 ? "Bet" : "Raise";
        }
    }

    private void SetPanelEnabled(bool enabled)
    {
        panelEnabled = enabled;
        gameObject.SetActive(enabled);
    }

    private void OnFoldClicked()
    {
        if (!panelEnabled || pendingDecision == null || activeRequest == null)
            return;

        if (IsLastStanding(activeRequest.Player))
        {
            CompleteDecision(new PlayerDecision(PlayerDecisionType.Check));
            return;
        }

        CompleteDecision(new PlayerDecision(PlayerDecisionType.Fold));
    }

    private void OnCheckCallClicked()
    {
        if (!panelEnabled || pendingDecision == null || activeRequest == null)
            return;

        if (callAmount <= 0)
        {
            CompleteDecision(new PlayerDecision(PlayerDecisionType.Check));
        }
        else
        {
            CompleteDecision(new PlayerDecision(PlayerDecisionType.Call));
        }
    }

    private void OnBetRaiseClicked()
    {
        if (!panelEnabled || pendingDecision == null || activeRequest == null || betSlider == null)
            return;

        int totalBet = Mathf.RoundToInt(betSlider.value);
        int raiseAmount = Math.Max(0, totalBet - callAmount);
        CompleteDecision(new PlayerDecision(PlayerDecisionType.Raise, raiseAmount));
    }

    private void OnSliderChanged(float value)
    {
        if (betValueText != null)
            betValueText.text = Mathf.RoundToInt(value).ToString();
    }

    private void CompleteDecision(PlayerDecision decision)
    {
        if (pendingDecision == null)
            return;

        cancellationRegistration.Dispose();
        pendingDecision.TrySetResult(decision);
        pendingDecision = null;
        activeRequest = null;
        SetPanelEnabled(false);
    }

    private void CancelPendingDecision()
    {
        cancellationRegistration.Dispose();
        if (pendingDecision != null)
        {
            pendingDecision.TrySetCanceled();
            pendingDecision = null;
        }
        activeRequest = null;
        SetPanelEnabled(false);
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Showdown || phase == GamePhase.HandComplete)
        {
            CancelPendingDecision();
        }
    }

    private bool IsLastStanding(WonderPokerCore.Player player)
    {
        if (player == null || activeRequest == null || activeRequest.Table == null)
            return false;

        int activeCount = activeRequest.Table.Players.Count(p => !p.Folded);
        return activeCount <= 1;
    }
}
