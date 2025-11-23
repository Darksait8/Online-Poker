using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WonderPokerCore;
using HoldemPlayerContract;
using HoldemBots.BetterBot;
using HoldemBots.RandomBot;
using CorePlayer = WonderPokerCore.Player;
using HoldemCard = HoldemPlayerContract.Card;
using HoldemPlayerInfo = HoldemPlayerContract.PlayerInfo;
using HoldemStage = HoldemPlayerContract.Stage;
using HoldemAction = HoldemPlayerContract.ActionType;
using HoldemBoardCardType = HoldemPlayerContract.EBoardCardType;
using HoldemSuitType = HoldemPlayerContract.ESuitType;
using HoldemRankType = HoldemPlayerContract.ERankType;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int smallBlind = 10;
    [SerializeField] private int bigBlind = 20;
    [Header("Bots")]
    [SerializeField] private bool enableBots = true;
    [SerializeField] private int humanSeatIndex = 0;
    [SerializeField] private int maxRaisesPerRound = 4;

    [Header("References")]
    [SerializeField] private BoardController boardController;
    [SerializeField] private ActionPanelController actionPanel;
    [SerializeField] private SeatsLayoutRadial seatsLayout;
    [SerializeField] private UnifiedPlayerManager unifiedPlayerManager;
    [SerializeField] private HandResultPopup handResultPopup;
    [SerializeField] private GameOverPanel gameOverPanel;

    [Header("Debug State (read-only)")]
    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private List<int> pots = new List<int>();
    [SerializeField] private List<Card> communityCards = new List<Card>();
    [SerializeField] private GamePhase currentPhase = GamePhase.WaitingToStart;
    [SerializeField] private int dealerIndex;
    [SerializeField] private int currentPlayerIndex = -1;
    [SerializeField] private int currentBet;
    
    public event Action<GamePhase> OnPhaseChanged;
    public event Action<Player> OnPlayerTurn;
    public event Action<Player, string, int> OnPlayerAction;
    public event Action<List<Player>> OnShowdown;
    
    public List<Player> Players => players;
    public int DealerIndex => dealerIndex;
    public int CurrentPlayerIndex => currentPlayerIndex;
    public List<int> Pots => pots;
    public int CurrentBet => currentBet;
    public GamePhase CurrentPhase => currentPhase;
    public List<Card> CommunityCards => communityCards;
    public int SmallBlind => smallBlind;
    public int BigBlind => bigBlind;
    
    [Header("Flow Settings")]
    [SerializeField] private float delayBeforeNextHand = 2.5f;

    private CancellationTokenSource handCancellation;
    private Coroutine nextHandCoroutine;
    private GameTable gameTable;
    private TexasHoldemDealer dealer;
    private GameplayController controller;
    private DecisionProviderRouter decisionRouter;
    private readonly Dictionary<CorePlayer, HoldemBotWrapper> botWrappers = new();
    private readonly Dictionary<CorePlayer, int> playerContributions = new();
    private readonly Dictionary<CorePlayer, int> stackAtHandStart = new();
    private readonly Dictionary<CorePlayer, int> seatIndexByPlayer = new();
    private GameConfig botGameConfig;
    private int handCounter;
    private int lastFullRaiseAmount;
    private bool matchFinished;
    private readonly List<PlayerSeatView> lastHandWinners = new();

    private readonly List<PlayerSeatView> runtimePlayers = new();
    private readonly Dictionary<WonderPokerCore.Player, PlayerSeatView> coreToView = new();
    private static Sprite cachedIndicatorSprite;

    private class PlayerSeatView
    {
        public Player legacy;
        public WonderPokerCore.Player core;
        public NewBehaviourScript ui;
        public GameObject turnIndicator;
        public Sprite avatarSprite;
    }

    private void Awake()
    {
        boardController ??= FindObjectOfType<BoardController>();
        actionPanel ??= FindObjectOfType<ActionPanelController>();
        seatsLayout ??= FindObjectOfType<SeatsLayoutRadial>();
        unifiedPlayerManager ??= FindObjectOfType<UnifiedPlayerManager>();
        matchFinished = false;

        var legacyStateMachine = FindObjectOfType<GameStateMachine>();
        if (legacyStateMachine != null)
        {
            legacyStateMachine.enabled = false;
        }

        if (unifiedPlayerManager == null)
        {
            StartCoroutine(InitializeAndStartRoutine());
        }

        EnsureHandResultPopup();
        EnsureGameOverPanel();
    }

    private void OnEnable()
    {
        AuthManager.OnUserProfileChanged += HandleUserProfileChanged;
        EnsureGameOverPanel();
    }

    private void OnDisable()
    {
        AuthManager.OnUserProfileChanged -= HandleUserProfileChanged;
        handCancellation?.Cancel();
        handCancellation = null;
        if (gameOverPanel != null)
        {
            gameOverPanel.OnRestartRequested -= HandleGameRestartRequested;
            gameOverPanel.OnMainMenuRequested -= HandleGameMainMenuRequested;
        }
    }

    private IEnumerator InitializeAndStartRoutine()
    {
        EnsureReferences();
        yield return null;
        InitializeRuntimePlayers();
        SetupController();
        StartNewHand();
    }

    private void InitializeRuntimePlayers()
    {
        runtimePlayers.Clear();
        coreToView.Clear();
        players.Clear();
        communityCards.Clear();
        pots.Clear();
            pots.Add(0);
        currentBet = 0;
        currentPlayerIndex = -1;
        dealerIndex = 0;
        currentPhase = GamePhase.WaitingToStart;

        if (!EnsureReferences())
        {
            Debug.LogError("GameManager: required references are missing, aborting initialization");
            return;
        }

        if (seatsLayout == null)
        {
            Debug.LogError("GameManager: SeatsLayoutRadial not found");
            return;
        }

        var occupiedSeats = seatsLayout.GetOccupiedSeats();
        if (occupiedSeats == null || occupiedSeats.Count < 2)
        {
            Debug.LogWarning("GameManager: not enough players to start gameplay");
            return;
        }

        int defaultStack = unifiedPlayerManager != null ? unifiedPlayerManager.DefaultStack : 1000;

        botWrappers.Clear();
        playerContributions.Clear();
        stackAtHandStart.Clear();
        seatIndexByPlayer.Clear();
        lastFullRaiseAmount = bigBlind;

        UserProfile currentUser = AuthManager.CurrentUser;
        string humanNickname = currentUser != null && !string.IsNullOrWhiteSpace(currentUser.username)
            ? currentUser.username
            : "Гость";
        Sprite humanAvatar = CustomAvatarManager.GetAvatarSprite(currentUser);
        if (humanAvatar == null)
            humanAvatar = AuthManager.GetCurrentAvatarSprite();
        if (humanAvatar == null)
            humanAvatar = AvatarLibrary.GetAvatarSprite("default");
        Sprite botAvatar = AvatarLibrary.GetAvatarSprite("bot") ?? AvatarLibrary.GetAvatarSprite("default");

        // Загружаем баланс из профиля для реального игрока
        int humanStartingChips = defaultStack;
        int humanStartingXp = 0;
        if (currentUser != null && !enableBots)
        {
            humanStartingChips = currentUser.chips; // Используем точный баланс из профиля
            humanStartingXp = currentUser.XP;
        }
        else if (currentUser != null && enableBots)
        {
            // Если включены боты, используем баланс из профиля
            humanStartingChips = currentUser.chips; // Используем точный баланс из профиля
            humanStartingXp = currentUser.XP;
        }

        for (int i = 0; i < occupiedSeats.Count; i++)
        {
            var seatUI = occupiedSeats[i];
            if (seatUI == null) continue;

            bool isHuman = !enableBots || i == humanSeatIndex;
            string playerName = isHuman ? humanNickname : $"Бот {i + 1}";
            Sprite seatAvatar = isHuman ? humanAvatar : botAvatar;
            int startingChips = isHuman ? humanStartingChips : defaultStack;
            int startingXp = isHuman ? humanStartingXp : 0;
            
            var existingIndicator = seatUI.transform.Find("TurnIndicator");
            if (existingIndicator != null)
            {
                Destroy(existingIndicator.gameObject);
            }
            seatUI.SetPlayer(playerName, startingChips, seatAvatar);
            seatUI.ShowBet(0);
            seatUI.HideHoles();
            seatUI.SetDealer(false);

            WonderPokerCore.Player corePlayer;
            if (isHuman)
            {
                corePlayer = new HumanPlayer(playerName, PlayerType.Human)
                {
                    TokensCount = startingChips,
                    SeatNr = i,
                    XP = startingXp
                };
            }
            else
            {
                corePlayer = new WonderPokerCore.Player(playerName, PlayerType.Bot)
                {
                    TokensCount = startingChips,
                    SeatNr = i,
                    XP = 0 // Боты не получают XP
                };
            }

            var legacyPlayer = new Player(i, playerName, startingChips, i)
            {
                Status = PlayerStatus.Active
            };

            runtimePlayers.Add(new PlayerSeatView
            {
                core = corePlayer,
                legacy = legacyPlayer,
                ui = seatUI,
                avatarSprite = seatAvatar
            });

            players.Add(legacyPlayer);
            playerContributions[corePlayer] = 0;
            stackAtHandStart[corePlayer] = defaultStack;
            seatIndexByPlayer[corePlayer] = i;
        }

        if (runtimePlayers.Count < 2)
        {
            Debug.LogWarning("GameManager: less than two players after initialization");
            return;
        }

        var owner = runtimePlayers[0].core as HumanPlayer;
        if (owner == null)
        {
            owner = new HumanPlayer(runtimePlayers[0].core.Nick, PlayerType.Human)
            {
                TokensCount = runtimePlayers[0].core.TokensCount,
                SeatNr = runtimePlayers[0].core.SeatNr,
                XP = runtimePlayers[0].core.XP
            };
            runtimePlayers[0].core = owner;
        }

        gameTable = new GameTable("Main Table", owner);
        gameTable.Settings.ChangeBigBlind(bigBlind);
        gameTable.Settings.ChangeMinTokens(0); // Разрешаем игрокам с любым количеством фишек
        gameTable.Settings.ChangeMinExperience(0); // Разрешаем игрокам с любым XP
        gameTable.Settings.ChangeMaxPlayers(runtimePlayers.Count);
        
        coreToView[owner] = runtimePlayers[0];

        for (int i = 1; i < runtimePlayers.Count; i++)
        {
            var view = runtimePlayers[i];
            bool added = gameTable.AddPlayer(view.core);
            if (!added)
            {
                Debug.LogError($"GameManager: Failed to add player {view.core.Nick} (seat {i}) to gameTable. XP: {view.core.XP}, Chips: {view.core.TokensCount}");
            }
            else
            {
                coreToView[view.core] = view;
            }
        }

        // Проверяем, что все игроки добавлены в gameTable
        if (gameTable.Players.Count != runtimePlayers.Count)
        {
            Debug.LogError($"GameManager: Mismatch between gameTable.Players.Count ({gameTable.Players.Count}) and runtimePlayers.Count ({runtimePlayers.Count}). Cannot start game.");
            Debug.LogError($"GameManager: gameTable.Players: {string.Join(", ", gameTable.Players.Select(p => $"{p.Nick} (XP: {p.XP}, Chips: {p.TokensCount})"))}");
            Debug.LogError($"GameManager: runtimePlayers: {string.Join(", ", runtimePlayers.Select(v => $"{v.core.Nick} (XP: {v.core.XP}, Chips: {v.core.TokensCount})"))}");
            return;
        }

        SetupDecisionRouter();
        InitializeBotPlayers(defaultStack);
        ApplyProfileToHumanSeat(currentUser);
    }

    private void SetupController()
    {
        DisposeController();

        if (!EnsureReferences() || gameTable == null || actionPanel == null)
        {
            Debug.LogError("GameManager: controller dependencies are missing");
            return;
        }

        dealer = new TexasHoldemDealer();
        SetInitialDealerPosition();
        var provider = (IPlayerDecisionProvider)actionPanel;
        if (decisionRouter != null)
            provider = decisionRouter;
        controller = new GameplayController(gameTable, dealer, provider);

        controller.GameStarted += HandleGameStarted;
        controller.GameEnded += HandleGameEnded;
        controller.RoundStarted += HandleRoundStarted;
        controller.RoundEnded += HandleRoundEnded;
        controller.PlayerTurnStarted += HandlePlayerTurnStarted;
        controller.PlayerActionCommitted += HandlePlayerActionCommitted;
        controller.PlayerFolded += HandlePlayerFolded;
        controller.BlindPaid += HandleBlindPaid;
        controller.DealerButtonChanged += HandleDealerChanged;
        controller.PlayerHoleCardsUpdated += HandlePlayerHoleCardsUpdated;
        controller.CommunityCardsUpdated += HandleCommunityCardsUpdated;
        controller.WinnersDetermined += HandleWinnersDetermined;

        actionPanel?.SetupSlider(0, 0, 50);
    }

    public void StartNewHand()
    {
        if (controller == null)
        {
            Debug.LogError("GameManager: controller not ready");
            return;
        }

        if (matchFinished)
        {
            Debug.Log("GameManager: match already finished, StartNewHand ignored.");
            return;
        }

        if (runtimePlayers.Count < 2)
        {
            Debug.LogWarning("GameManager: not enough players to start hand");
            return;
        }

        handCancellation?.Cancel();
        handCancellation = new CancellationTokenSource();
        RunHandAsync(handCancellation.Token);
    }

    private async void RunHandAsync(CancellationToken token)
    {
        if (matchFinished)
            return;

        if (gameTable == null || gameTable.Players == null)
        {
            Debug.LogError("GameManager: gameTable or Players is null");
            return;
        }

        int playersCount = gameTable.Players.Count;
        if (playersCount < 2)
        {
            Debug.LogError($"GameManager: not enough players in gameTable to start hand. Players count: {playersCount}, runtimePlayers: {runtimePlayers.Count}");
            return;
        }

        try
        {
            await controller.PlayHandAsync(token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void HandleGameStarted()
    {
        dealerIndex = dealer.Position;
        currentBet = 0;
        pots = new List<int> { 0 };
        communityCards.Clear();
        SetPhase(GamePhase.PreFlop);

        boardController?.Clear();
        ShowActionPanel();

        foreach (var view in runtimePlayers)
        {
            view.core.ResetPlayerGameState();
            view.legacy.PrepareForNewHand();
            view.legacy.Status = PlayerStatus.Active;
            view.legacy.Stack = view.core.TokensCount;
            view.legacy.CurrentBet = 0;
            view.ui.ShowBet(0);
            view.ui.UpdateStack(view.core.TokensCount);
            view.ui.HideHoles();
            view.ui.SetDealer(view == runtimePlayers[dealerIndex]);
            view.ui.ShowChips(false);
            SetTurnIndicatorActive(view, false);
        }

        handResultPopup?.HideImmediate();

        handCounter++;
        lastFullRaiseAmount = bigBlind;
        foreach (var view in runtimePlayers)
        {
            playerContributions[view.core] = 0;
            stackAtHandStart[view.core] = view.core.TokensCount;
        }

        if (botWrappers.Count > 0)
        {
            var playerInfos = BuildPlayerInfos();
            foreach (var wrapper in botWrappers.Values)
            {
                wrapper.InitHand(handCounter, playerInfos.Count, playerInfos, dealerIndex, smallBlind, bigBlind);
            }
        }
    }

    private void HandleGameEnded()
    {
        SetPhase(GamePhase.HandComplete);
        foreach (var view in runtimePlayers)
        {
            SetTurnIndicatorActive(view, false);
        }
        HideActionPanel();

        bool bustedPlayersRemoved = RemoveBustedPlayers();

        if (nextHandCoroutine != null)
            StopCoroutine(nextHandCoroutine);

        if (!gameObject.activeInHierarchy || runtimePlayers.Count < 2)
        {
            HideActionPanel();
            if (!matchFinished)
            {
                var winner = runtimePlayers.Count == 1
                    ? runtimePlayers[0]
                    : lastHandWinners.FirstOrDefault();
                ShowGameOverPanel(winner);
            }
            return;
        }

        if (matchFinished)
            return;

        nextHandCoroutine = StartCoroutine(StartNextHandAfterDelay());

        if (botWrappers.Count > 0)
        {
            var playerInfos = BuildPlayerInfos();
            foreach (var wrapper in botWrappers.Values)
            {
                wrapper.EndOfGame(playerInfos.Count, playerInfos);
            }
        }
    }

    private void HandleRoundStarted(GameRound round)
    {
        switch (round)
        {
            case GameRound.PreFlop:
                SetPhase(GamePhase.PreFlopBetting);
                break;
            case GameRound.Flop:
                SetPhase(GamePhase.FlopBetting);
                break;
            case GameRound.Turn:
                SetPhase(GamePhase.TurnBetting);
                break;
            case GameRound.River:
                SetPhase(GamePhase.RiverBetting);
                break;
        }
    }

    private void HandleRoundEnded(GameRound round)
    {
        currentBet = 0;
        foreach (var view in runtimePlayers)
        {
            view.core.PlayersCurrentBet = 0;
            view.legacy.ResetBet();
        }
        pots[0] = gameTable.TokensInGame;
    }

    private void HandlePlayerTurnStarted(WonderPokerCore.Player player)
    {
        if (!coreToView.TryGetValue(player, out var view))
            return;

        currentPlayerIndex = players.IndexOf(view.legacy);
        HighlightCurrentPlayer(view);
        OnPlayerTurn?.Invoke(view.legacy);
    }

    private void HandlePlayerActionCommitted(WonderPokerCore.Player player, PlayerDecision decision)
    {
        if (!coreToView.TryGetValue(player, out var view))
            return;

        string actionName = decision.Type switch
        {
            PlayerDecisionType.Fold => "fold",
            PlayerDecisionType.Check => "check",
            PlayerDecisionType.Call => "call",
            PlayerDecisionType.Raise => player.PlayersCurrentBet > currentBet ? "raise" : "bet",
            PlayerDecisionType.AllIn => "all-in",
            _ => decision.Type.ToString().ToLower()
        };

        view.legacy.CurrentBet = player.PlayersCurrentBet;
        view.legacy.Stack = player.TokensCount;
        if (decision.Type == PlayerDecisionType.Fold)
        {
            view.legacy.Status = PlayerStatus.Folded;
            view.ui.HideHoles();
            view.ui.ShowChips(false);
        }

        view.ui.UpdateStack(player.TokensCount);
        view.ui.ShowBet(player.PlayersCurrentBet);

        currentBet = Math.Max(currentBet, gameTable.CurrentBid);
        pots[0] = gameTable.TokensInGame;

        OnPlayerAction?.Invoke(view.legacy, actionName, decision.Amount);

        int previousContribution = playerContributions.TryGetValue(player, out var prevValue) ? prevValue : 0;
        int highestContribution = playerContributions.Count > 0 ? playerContributions.Values.Max() : previousContribution;
        int callAmountBefore = Math.Max(0, highestContribution - previousContribution);

        int totalInvested = stackAtHandStart.TryGetValue(player, out var startingStack)
            ? Math.Max(0, startingStack - player.TokensCount)
            : previousContribution;
        playerContributions[player] = totalInvested;
        int delta = Math.Max(0, totalInvested - previousContribution);

        if (decision.Type == PlayerDecisionType.Raise && decision.Amount > 0)
            lastFullRaiseAmount = Math.Max(decision.Amount, bigBlind);
        else if (decision.Type == PlayerDecisionType.AllIn && delta > 0)
            lastFullRaiseAmount = Math.Max(lastFullRaiseAmount, delta);

        bool playerAllIn = decision.Type == PlayerDecisionType.AllIn || view.core.TokensCount <= 0 || view.legacy.Stack <= 0;
        if (playerAllIn)
            HandlePlayerAllIn(view);

        NotifyBotAction(player,
                        ToStage(currentPhase),
                        ConvertDecisionToAction(decision, callAmountBefore),
                        delta);
    }

    private void HandlePlayerFolded(WonderPokerCore.Player player)
    {
        if (!coreToView.TryGetValue(player, out var view))
            return;

        view.legacy.Status = PlayerStatus.Folded;
        view.ui.HideHoles();
        view.ui.ShowChips(false);
    }

    private void HandlePlayerAllIn(PlayerSeatView view)
    {
        if (view == null)
            return;

        view.legacy.Status = PlayerStatus.AllIn;
        RevealPlayerCards(view);
    }

    private void RevealPlayerCards(PlayerSeatView view)
    {
        if (view == null || view.ui == null)
            return;

        var cards = view.legacy.HoleCards;
        if (cards == null || cards.Length < 2)
        {
            var coreCards = view.core?.PlayerHand?.Cards;
            if (coreCards != null && coreCards.Count >= 2)
            {
                cards = new[]
                {
                    WonderCardConverter.ToClientCard(coreCards[0]),
                    WonderCardConverter.ToClientCard(coreCards[1])
                };
                view.legacy.HoleCards = cards;
            }
        }

        if (cards != null && cards.Length >= 2)
            view.ui.ShowHole(cards[0], cards[1]);
    }

    private void HandleBlindPaid(WonderPokerCore.Player player, int amount)
    {
        if (!coreToView.TryGetValue(player, out var view))
            return;

        view.legacy.CurrentBet = player.PlayersCurrentBet;
        view.legacy.Stack = player.TokensCount;
        view.ui.UpdateStack(player.TokensCount);
        view.ui.ShowBet(player.PlayersCurrentBet);

        currentBet = Math.Max(currentBet, gameTable.CurrentBid);
        pots[0] = gameTable.TokensInGame;
        OnPlayerAction?.Invoke(view.legacy, "blind", amount);

        int previous = playerContributions.TryGetValue(player, out var prevContribution) ? prevContribution : 0;
        int total = stackAtHandStart.TryGetValue(player, out var startStack)
            ? Math.Max(0, startStack - player.TokensCount)
            : previous + amount;
        playerContributions[player] = total;
        int delta = Math.Max(0, total - previous);
        lastFullRaiseAmount = Math.Max(lastFullRaiseAmount, delta > 0 ? delta : amount);

        NotifyBotAction(player, HoldemStage.StagePreflop, HoldemAction.Blind, delta > 0 ? delta : amount);
    }

    private void HandleDealerChanged(WonderPokerCore.Player player)
    {
        dealerIndex = gameTable.Players.IndexOf(player);
        for (int i = 0; i < runtimePlayers.Count; i++)
        {
            runtimePlayers[i].ui.SetDealer(i == dealerIndex);
        }
    }

    private void HandlePlayerHoleCardsUpdated(WonderPokerCore.Player player)
    {
        if (!coreToView.TryGetValue(player, out var view))
            return;

        var hand = player.PlayerHand.Cards;
        if (hand == null || hand.Count < 2)
        {
            view.ui.HideHoles();
            return;
        }

        Card[] clientCards =
        {
            WonderCardConverter.ToClientCard(hand[0]),
            WonderCardConverter.ToClientCard(hand[1])
        };

        bool isHumanSeat = seatIndexByPlayer.TryGetValue(player, out int seatIndex) && seatIndex == humanSeatIndex;
        if (isHumanSeat)
        {
            view.ui.ShowHole(clientCards[0], clientCards[1]);
            }
            else
            {
            view.ui.ShowHoleBacks();
        }
        view.legacy.HoleCards[0] = clientCards[0];
        view.legacy.HoleCards[1] = clientCards[1];

        if (botWrappers.TryGetValue(player, out var wrapper))
        {
            if (hand != null && hand.Count >= 2)
            {
                wrapper.ReceiveHoleCards(ToHoldemCard(hand[0]), ToHoldemCard(hand[1]));
            }
        }
    }

    private void HandleCommunityCardsUpdated(CardsCollection collection)
    {
        int previousCount = communityCards?.Count ?? 0;
        var cards = WonderCardConverter.ToClientCards(collection);
        communityCards = cards.ToList();

        if (cards.Length >= 3)
            boardController?.SetFlopCards(cards.Take(3).ToArray());
        if (cards.Length >= 4)
            boardController?.SetTurnCard(cards[3]);
        if (cards.Length >= 5)
            boardController?.SetRiverCard(cards[4]);

        if (cards.Length == 3)
            SetPhase(GamePhase.Flop);
        else if (cards.Length == 4)
            SetPhase(GamePhase.Turn);
        else if (cards.Length == 5)
            SetPhase(GamePhase.River);

        if (botWrappers.Count > 0 && collection?.Cards != null)
        {
            int currentCount = collection.Cards.Count;
            for (int i = previousCount; i < currentCount && i < 5; i++)
            {
                var boardCard = ToHoldemCard(collection.Cards[i]);
                var cardType = ToBoardCardType(i);
                foreach (var wrapper in botWrappers.Values)
                {
                    wrapper.SeeBoardCard(cardType, boardCard);
                }
            }
        }
    }

    private void HandleWinnersDetermined(IReadOnlyList<WonderPokerCore.Player> winners)
    {
        var winnerViews = winners?
            .Select(w => coreToView.TryGetValue(w, out var view) ? view : null)
            .Where(v => v != null)
            .ToList() ?? new List<PlayerSeatView>();

        if (winnerViews.Count == 0)
            return;

        lastHandWinners.Clear();
        lastHandWinners.AddRange(winnerViews);

        SetPhase(GamePhase.Showdown);
        HighlightWinners(winnerViews);

        var legacyWinners = new List<Player>();
        var messageBuilder = new System.Text.StringBuilder();
        messageBuilder.AppendLine("Раздача завершена!");

        UserProfile currentUser = AuthManager.CurrentUser;
        
        foreach (var view in winnerViews)
        {
            int previousStack = view.legacy.Stack;
            int chipsGain = view.core.TokensCount - previousStack;
            int xpGain = 0;
            
            // Начисляем XP и обновляем баланс только реальному игроку (не ботам)
            bool isHuman = view.core is HumanPlayer || (!enableBots || (seatIndexByPlayer.TryGetValue(view.core, out int seatIdx) && seatIdx == humanSeatIndex));
            if (isHuman && currentUser != null)
            {
                xpGain = 100; // Начисляем XP только реальному игроку
                int newBalance = view.core.TokensCount;
                int newXp = view.core.XP + xpGain;
                
                // Обновляем баланс и XP в профиле
                AuthManager.UpdatePlayerBalance(newBalance);
                AuthManager.AddPlayerXp(xpGain);
                
                // Обновляем XP в core объекте для отображения
                view.core.XP = newXp;
                
                messageBuilder.AppendLine($"{view.legacy.Name}: +{chipsGain} фишек (+{xpGain} XP)");
            }
            else
            {
                // Для ботов просто показываем выигрыш фишек, без XP
                messageBuilder.AppendLine($"{view.legacy.Name}: +{chipsGain} фишек");
            }
            
            legacyWinners.Add(view.legacy);
        }

        OnShowdown?.Invoke(legacyWinners);
        UpdateAllPlayerStacksFromCore();
        ResetPotValue();
        handResultPopup?.Show(messageBuilder.ToString(), delayBeforeNextHand);
    }

    private void SetPhase(GamePhase phase)
    {
        if (currentPhase == phase)
            return;

        currentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    private void HighlightCurrentPlayer(PlayerSeatView current)
    {
        foreach (var view in runtimePlayers)
        {
            SetTurnIndicatorActive(view, view == current);
        }
    }

    private void SetTurnIndicatorActive(PlayerSeatView view, bool active)
    {
        EnsureTurnIndicator(view);
        if (view.turnIndicator != null)
            view.turnIndicator.SetActive(active);
    }

    private void EnsureTurnIndicator(PlayerSeatView view)
    {
        if (view.turnIndicator != null || view.ui == null)
            return;

        var seatRect = view.ui.GetComponent<RectTransform>();
        if (seatRect == null) return;

        var go = new GameObject("TurnIndicator", typeof(RectTransform), typeof(Image));
        var indicatorRect = go.GetComponent<RectTransform>();
        indicatorRect.SetParent(seatRect, false);
        indicatorRect.anchorMin = new Vector2(0.5f, 1f);
        indicatorRect.anchorMax = new Vector2(0.5f, 1f);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);
        indicatorRect.anchoredPosition = new Vector2(0f, 45f);
        indicatorRect.sizeDelta = new Vector2(40f, 40f);

        var image = go.GetComponent<Image>();
        image.sprite = GetTurnIndicatorSprite();
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = new Color(1f, 0f, 0f, 0.85f);

        view.turnIndicator = go;
        go.SetActive(false);
    }

    public void RebuildPlayersAndStart()
    {
        Debug.Log("GameManager: rebuilding seats and starting new hand");
        handCancellation?.Cancel();
        if (!EnsureReferences())
        {
            Debug.LogError("GameManager: failed to rebuild because references are missing");
            return;
        }
        InitializeRuntimePlayers();
        SetupController();
        StartNewHand();
    }

    private static Sprite GetTurnIndicatorSprite()
    {
        if (cachedIndicatorSprite != null)
            return cachedIndicatorSprite;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        var center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var pos = new Vector2(x + 0.5f, y + 0.5f);
                float dist = Vector2.Distance(pos, center);
                float alpha = dist <= radius ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        cachedIndicatorSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cachedIndicatorSprite.name = "TurnIndicatorSprite";
        return cachedIndicatorSprite;
    }

    private void DisposeController()
    {
        if (controller == null)
            return;

        controller.GameStarted -= HandleGameStarted;
        controller.GameEnded -= HandleGameEnded;
        controller.RoundStarted -= HandleRoundStarted;
        controller.RoundEnded -= HandleRoundEnded;
        controller.PlayerTurnStarted -= HandlePlayerTurnStarted;
        controller.PlayerActionCommitted -= HandlePlayerActionCommitted;
        controller.PlayerFolded -= HandlePlayerFolded;
        controller.BlindPaid -= HandleBlindPaid;
        controller.DealerButtonChanged -= HandleDealerChanged;
        controller.PlayerHoleCardsUpdated -= HandlePlayerHoleCardsUpdated;
        controller.CommunityCardsUpdated -= HandleCommunityCardsUpdated;
        controller.WinnersDetermined -= HandleWinnersDetermined;
        controller = null;
    }

    private void HighlightWinners(IEnumerable<PlayerSeatView> winnerViews)
    {
        var winnerSet = new HashSet<PlayerSeatView>(winnerViews);
        foreach (var view in runtimePlayers)
        {
            bool isWinner = winnerSet.Contains(view);
            SetTurnIndicatorActive(view, isWinner);
        }
    }

    private void UpdateAllPlayerStacksFromCore()
    {
        foreach (var view in runtimePlayers)
        {
            view.legacy.Stack = view.core.TokensCount;
            view.legacy.CurrentBet = view.core.PlayersCurrentBet;
            view.ui.SetPlayer(view.legacy.Name, view.legacy.Stack, view.avatarSprite);
            view.ui.UpdateStack(view.legacy.Stack);
            view.ui.ShowBet(view.legacy.CurrentBet);
        }
    }

    private void ResetPotValue()
    {
        if (pots != null && pots.Count > 0)
            pots[0] = 0;
        currentBet = 0;
    }

    private IEnumerator StartNextHandAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeNextHand);
        boardController?.ResetBoard();
        StartNewHand();
    }

    private void HideActionPanel()
    {
        if (actionPanel != null && actionPanel.gameObject.activeSelf)
        {
            actionPanel.gameObject.SetActive(false);
        }
    }

    private void ShowActionPanel()
    {
        if (actionPanel != null && !actionPanel.gameObject.activeSelf)
        {
            actionPanel.gameObject.SetActive(true);
        }
    }

    private void EnsureHandResultPopup()
    {
        if (handResultPopup != null)
            return;

        handResultPopup = FindObjectOfType<HandResultPopup>();
        if (handResultPopup != null)
            return;

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        handResultPopup = HandResultPopup.CreateDefault(canvas.transform);
    }

    private void EnsureGameOverPanel()
    {
        if (gameOverPanel == null)
            gameOverPanel = FindObjectOfType<GameOverPanel>(true);

        if (gameOverPanel == null)
        {
            Canvas targetCanvas = null;
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c == null) continue;
                if (targetCanvas == null)
                    targetCanvas = c;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    targetCanvas = c;
                    break;
                }
            }

            if (targetCanvas != null)
                gameOverPanel = GameOverPanel.CreateDefault(targetCanvas.transform);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.HideImmediate();
            gameOverPanel.OnRestartRequested -= HandleGameRestartRequested;
            gameOverPanel.OnRestartRequested += HandleGameRestartRequested;
            gameOverPanel.OnMainMenuRequested -= HandleGameMainMenuRequested;
            gameOverPanel.OnMainMenuRequested += HandleGameMainMenuRequested;
        }
    }

    private void SetupDecisionRouter()
    {
        decisionRouter = new DecisionProviderRouter(actionPanel);
    }

    private void InitializeBotPlayers(int startingStack)
    {
        botWrappers.Clear();
        if (!enableBots)
            return;

        botGameConfig = BuildBotConfig(startingStack);

        foreach (var view in runtimePlayers)
        {
            if (view.core.Type != PlayerType.Bot)
                continue;

            var botInstance = CreateBotInstance(view.core.SeatNr);
            var wrapper = new HoldemBotWrapper(
                view.core.SeatNr,
                view.core,
                botInstance,
                playerContributions,
                stackAtHandStart,
                () => lastFullRaiseAmount,
                () => Math.Max(0, maxRaisesPerRound),
                () => gameTable);

            wrapper.InitPlayer(botGameConfig, new Dictionary<string, string>());
            botWrappers[view.core] = wrapper;
            decisionRouter.Register(view.core, wrapper);
        }
    }

    private GameConfig BuildBotConfig(int startingStack)
    {
        return new GameConfig
        {
            SmallBlindSize = smallBlind,
            BigBlindSize = bigBlind,
            StartingStack = startingStack,
            MaxNumRaisesPerBettingRound = Math.Max(1, maxRaisesPerRound),
            MaxHands = 0,
            DoubleBlindFrequency = 0,
            BotTimeOutMilliSeconds = 0,
            RandomDealer = false,
            RandomSeating = false
        };
    }

    private IHoldemPlayer CreateBotInstance(int seatIndex)
    {
        // чередуем стили ботов для разнообразия
        return seatIndex % 2 == 0 ? new BetterBot() : new RandomBot();
    }

    private void SetInitialDealerPosition()
    {
        int count = runtimePlayers.Count;
        if (count <= 0)
        {
            dealer.Position = -1;
                return;
        }

        int desiredDealer = ((count - 3) % count + count) % count;
        int startingPosition = (desiredDealer - 1 + count) % count;
        dealer.Position = startingPosition;
    }

    private List<HoldemPlayerInfo> BuildPlayerInfos()
    {
        var list = new List<HoldemPlayerInfo>(runtimePlayers.Count);
        foreach (var view in runtimePlayers)
        {
            bool isAlive = view.core.TokensCount > 0;
            list.Add(new HoldemPlayerInfo(view.core.SeatNr, view.core.Nick, isAlive, view.core.TokensCount));
        }
        return list;
    }

    private void NotifyBotAction(CorePlayer actor, HoldemStage stage, HoldemAction action, int amount)
    {
        if (botWrappers.Count == 0)
            return;

        int playerSeat = seatIndexByPlayer.TryGetValue(actor, out var seatIndex) ? seatIndex : actor.SeatNr;
        foreach (var wrapper in botWrappers.Values)
        {
            wrapper.SeeAction(stage, playerSeat, action, amount);
        }
    }

    private static HoldemCard ToHoldemCard(WonderPokerCore.Card card)
    {
        if (card == null) return null;

        var suit = card.Sign switch
        {
            CardSign.Club => HoldemSuitType.SuitClubs,
            CardSign.Diamond => HoldemSuitType.SuitDiamonds,
            CardSign.Heart => HoldemSuitType.SuitHearts,
            CardSign.Spade => HoldemSuitType.SuitSpades,
            _ => HoldemSuitType.SuitUnknown
        };

        var rank = card.Value switch
        {
            CardValue.Two => HoldemRankType.RankTwo,
            CardValue.Three => HoldemRankType.RankThree,
            CardValue.Four => HoldemRankType.RankFour,
            CardValue.Five => HoldemRankType.RankFive,
            CardValue.Six => HoldemRankType.RankSix,
            CardValue.Seven => HoldemRankType.RankSeven,
            CardValue.Eight => HoldemRankType.RankEight,
            CardValue.Nine => HoldemRankType.RankNine,
            CardValue.Ten => HoldemRankType.RankTen,
            CardValue.Jack => HoldemRankType.RankJack,
            CardValue.Queen => HoldemRankType.RankQueen,
            CardValue.King => HoldemRankType.RankKing,
            CardValue.Ace => HoldemRankType.RankAce,
            _ => HoldemRankType.RankUnknown
        };

        return new HoldemCard(rank, suit);
    }

    private static HoldemBoardCardType ToBoardCardType(int index)
    {
        return index switch
        {
            0 => HoldemBoardCardType.BoardFlop1,
            1 => HoldemBoardCardType.BoardFlop2,
            2 => HoldemBoardCardType.BoardFlop3,
            3 => HoldemBoardCardType.BoardTurn,
            4 => HoldemBoardCardType.BoardRiver,
            _ => HoldemBoardCardType.BoardFlop1
        };
    }

    private static HoldemStage ToStage(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.PreFlop or GamePhase.PreFlopBetting => HoldemStage.StagePreflop,
            GamePhase.Flop or GamePhase.FlopBetting => HoldemStage.StageFlop,
            GamePhase.Turn or GamePhase.TurnBetting => HoldemStage.StageTurn,
            GamePhase.River or GamePhase.RiverBetting => HoldemStage.StageRiver,
            GamePhase.Showdown or GamePhase.HandComplete => HoldemStage.StageShowdown,
            _ => HoldemStage.StagePreflop
        };
    }

    private static HoldemAction ConvertDecisionToAction(PlayerDecision decision, int callAmountBefore)
    {
        return decision.Type switch
        {
            PlayerDecisionType.Fold => HoldemAction.Fold,
            PlayerDecisionType.Check => HoldemAction.Check,
            PlayerDecisionType.Call => HoldemAction.Call,
            PlayerDecisionType.Raise => HoldemAction.Raise,
            PlayerDecisionType.AllIn => HoldemAction.Raise,
            _ => HoldemAction.Check
        };
    }

    private bool RemoveBustedPlayers()
    {
        if (runtimePlayers.Count == 0)
            return false;

        var busted = new List<PlayerSeatView>();
        foreach (var view in runtimePlayers)
        {
            if (view?.core != null && view.core.TokensCount <= 0)
                busted.Add(view);
        }

        if (busted.Count == 0)
            return false;

        foreach (var view in busted)
        {
            seatsLayout?.Leave(view.legacy.Name);
            coreToView.Remove(view.core);
            botWrappers.Remove(view.core);
            playerContributions.Remove(view.core);
            stackAtHandStart.Remove(view.core);
            seatIndexByPlayer.Remove(view.core);
            if (gameTable != null)
                gameTable.Players.Remove(view.core);
            players.Remove(view.legacy);
            runtimePlayers.Remove(view);

            if (view.ui != null)
            {
                view.ui.SetPlayer("Свободно", 0, null);
                view.ui.ShowBet(0);
                view.ui.HideHoles();
                view.ui.ShowChips(false);
                view.ui.SetDealer(false);
            }
        }

        return true;
    }

    private void HandleGameRestartRequested()
    {
        Time.timeScale = 1f;
        matchFinished = false;
        gameOverPanel?.HideImmediate();
        handCancellation?.Cancel();
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadGameScene();
        }
        else
        {
            SceneManager.LoadScene("Main");
        }
    }

    private void HandleGameMainMenuRequested()
    {
        Time.timeScale = 1f;
        matchFinished = false;
        gameOverPanel?.HideImmediate();
        handCancellation?.Cancel();
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainMenu();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void ShowGameOverPanel(PlayerSeatView winner)
    {
        if (matchFinished)
            return;

        EnsureGameOverPanel();
        if (gameOverPanel == null)
        {
            Debug.LogWarning("GameManager: GameOverPanel not found, cannot show match result.");
            return;
        }

        matchFinished = true;
        Time.timeScale = 0f;

        // Сохраняем финальный баланс для реального игрока
        UserProfile currentUser = AuthManager.CurrentUser;
        if (currentUser != null && winner != null)
        {
            bool isHuman = winner.core is HumanPlayer || 
                          (!enableBots || (seatIndexByPlayer.TryGetValue(winner.core, out int seatIdx) && seatIdx == humanSeatIndex));
            
            if (isHuman)
            {
                int finalBalance = winner.core.TokensCount;
                AuthManager.UpdatePlayerBalance(finalBalance);
                Debug.Log($"GameManager: Сохранен финальный баланс {finalBalance} фишек для игрока {currentUser.username}");
            }
        }

        string winnerName = winner?.legacy?.Name ?? "Игрок";
        int winnerStack = winner?.legacy?.Stack ?? 0;

        gameOverPanel.Show(winnerName, winnerStack, handCounter);
    }

    private void HandleUserProfileChanged(UserProfile profile)
    {
        ApplyProfileToHumanSeat(profile);
    }

    private void ApplyProfileToHumanSeat(UserProfile profile)
    {
        if (runtimePlayers == null || runtimePlayers.Count == 0)
            return;

        int seat = Mathf.Clamp(humanSeatIndex, 0, runtimePlayers.Count - 1);
        if (seat < 0 || seat >= runtimePlayers.Count)
            return;

        var view = runtimePlayers[seat];
        if (view == null || view.ui == null)
            return;

        string nickname = profile != null && !string.IsNullOrWhiteSpace(profile?.username)
            ? profile.username
            : "Гость";

        view.legacy.Name = nickname;
        view.core.ChangeNick(nickname);

        Sprite avatar = CustomAvatarManager.GetAvatarSprite(profile);
        if (avatar == null)
            avatar = AuthManager.GetCurrentAvatarSprite();
        if (avatar == null)
            avatar = AvatarLibrary.GetAvatarSprite("default");

        view.avatarSprite = avatar;
        view.ui.SetPlayer(nickname, view.legacy.Stack, avatar);
    }

    public bool AddPlayer(string playerName, int initialStack, int seatIndex = -1)
    {
        if (seatsLayout == null)
            return false;

        if (string.IsNullOrWhiteSpace(playerName))
            playerName = $"Игрок {seatsLayout.OccupiedCount + 1}";

        bool success = seatsLayout.TryJoin(playerName, initialStack);
        if (success)
        {
            RebuildPlayersAndStart();
        }
        return success;
    }

    public bool RemovePlayer(string playerName)
    {
        if (seatsLayout == null)
            return false;

        bool success = seatsLayout.Leave(playerName);
        if (success)
        {
            RebuildPlayersAndStart();
        }
        return success;
    }

    public bool ResetPlayerStack(string playerName, int newStack)
    {
        var view = runtimePlayers.FirstOrDefault(v => v.legacy.Name == playerName);
        if (view == null) return false;

        int stack = Mathf.Max(0, newStack);
        view.core.TokensCount = stack;
        view.legacy.Stack = stack;
        view.ui.UpdateStack(stack);
        return true;
    }

    public void SetBlindLevels(int newSmallBlind, int newBigBlind)
    {
        if (newSmallBlind <= 0 || newBigBlind <= newSmallBlind)
        {
            Debug.LogWarning("GameManager: invalid blind levels");
            return;
        }

        smallBlind = newSmallBlind;
        bigBlind = newBigBlind;
        gameTable?.Settings.ChangeBigBlind(bigBlind);
    }

    public (int smallBlind, int bigBlind) GetBlindLevels() => (smallBlind, bigBlind);

    public bool ProcessPlayerAction(string action, int amount = 0)
    {
        Debug.LogWarning("GameManager.ProcessPlayerAction is deprecated in the new gameplay system");
            return false;
        }

    private bool EnsureReferences()
    {
        boardController ??= FindObjectOfType<BoardController>();
        actionPanel ??= FindObjectOfType<ActionPanelController>();
        seatsLayout ??= FindObjectOfType<SeatsLayoutRadial>();
        unifiedPlayerManager ??= FindObjectOfType<UnifiedPlayerManager>();
        if (handResultPopup == null)
            EnsureHandResultPopup();

        return boardController != null && actionPanel != null && seatsLayout != null;
    }
}