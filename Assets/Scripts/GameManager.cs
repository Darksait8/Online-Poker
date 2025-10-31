using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Основной контроллер покерной игры
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int smallBlind = 10;
    [SerializeField] private int bigBlind = 20;
    [SerializeField] private int maxPlayers = 6;
    
    [Header("Game State")]
    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private int dealerIndex = 0;
    [SerializeField] private int currentPlayerIndex = -1;
    [SerializeField] private List<int> pots = new List<int>();
    [SerializeField] private int currentBet = 0;
    [SerializeField] private GamePhase currentPhase = GamePhase.WaitingToStart;
    
    [Header("Community Cards")]
    [SerializeField] private List<Card> communityCards = new List<Card>();
    
    // Events
    public event Action<GamePhase> OnPhaseChanged;
    public event Action<Player> OnPlayerTurn;
    public event Action<Player, string, int> OnPlayerAction;
    public event Action<List<Player>> OnShowdown;
    
    // Properties
    public List<Player> Players => players;
    public int DealerIndex => dealerIndex;
    public int CurrentPlayerIndex => currentPlayerIndex;
    public List<int> Pots => pots;
    public int CurrentBet => currentBet;
    public GamePhase CurrentPhase => currentPhase;
    public List<Card> CommunityCards => communityCards;
    public int SmallBlind => smallBlind;
    public int BigBlind => bigBlind;
    
    // Методы для поэтапной раздачи карт
    private void DealPlayerCards()
    {
        foreach (var player in players.Where(p => p != null && p.IsActive))
        {
            var cards = new List<Card> { deck.DrawCard(), deck.DrawCard() };
            player.SetHoleCards(cards);
        }
        currentPhase = GamePhase.PreFlopBetting;
    }

    private void DealFlop()
    {
        if (currentPhase != GamePhase.PreFlopBetting) return;
        
        for (int i = 0; i < 3; i++)
        {
            communityCards.Add(deck.DrawCard());
        }
        currentPhase = GamePhase.FlopBetting;
    }

    private void DealTurn()
    {
        if (currentPhase != GamePhase.FlopBetting) return;
        
        communityCards.Add(deck.DrawCard());
        currentPhase = GamePhase.TurnBetting;
    }

    private void DealRiver()
    {
        if (currentPhase != GamePhase.TurnBetting) return;
        
        communityCards.Add(deck.DrawCard());
        currentPhase = GamePhase.RiverBetting;
    }
    
    // Private fields
    private Deck deck;
    private int bettingRoundStartPlayer;
    private bool isHandInProgress = false;
    private SeatsLayoutRadial seatsLayout;

    private void Awake()
    {
        // Инициализация основных компонентов
        if (pots.Count == 0)
            pots.Add(0);
            
        // Находим SeatsLayoutRadial
        seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        deck = new Deck(); // Инициализируем колоду
    }
    
    private bool AllPlayersActed()
    {
        return players.Where(p => p != null && p.IsActive)
                     .All(p => p.HasActed || p.IsFolded);
    }

    /// <summary>
    /// Добавляет игрока за стол
    /// </summary>
    public bool AddPlayer(string playerName, int initialStack, int seatIndex = -1)
    {
        if (players.Count >= maxPlayers)
        {
            Debug.LogWarning("Table is full");
            return false;
        }

        // Автоматически выбираем место, если не указано
        if (seatIndex == -1)
        {
            var occupiedSeats = players.Select(p => p.SeatIndex).ToHashSet();
            for (int i = 0; i < maxPlayers; i++)
            {
                if (!occupiedSeats.Contains(i))
                {
                    seatIndex = i;
                    break;
                }
            }
        }

        // Проверяем, что место свободно
        if (players.Any(p => p.SeatIndex == seatIndex))
        {
            Debug.LogWarning($"Seat {seatIndex} is already occupied");
            return false;
        }

        var player = new Player(players.Count, playerName, initialStack, seatIndex);
        players.Add(player);
        
        // Обновляем UI
        if (seatsLayout != null)
        {
            seatsLayout.UpdatePlayerUI(player);
        }
        
        Debug.Log($"Player {playerName} joined at seat {seatIndex} with stack {initialStack}");
        return true;
    }

    /// <summary>
    /// Удаляет игрока со стола
    /// </summary>
    public bool RemovePlayer(string playerName)
    {
        var player = players.FirstOrDefault(p => p.Name == playerName);
        if (player == null) return false;

        players.Remove(player);
        
        // Обновляем UI
        if (seatsLayout != null)
        {
            seatsLayout.UpdateAllPlayersUI();
        }
        
        Debug.Log($"Player {playerName} left the table");
        return true;
    }

    /// <summary>
    /// Устанавливает размеры блайндов
    /// </summary>
    public void SetBlindLevels(int newSmallBlind, int newBigBlind)
    {
        if (newSmallBlind <= 0 || newBigBlind <= newSmallBlind)
        {
            Debug.LogWarning("Invalid blind levels");
            return;
        }

        smallBlind = newSmallBlind;
        bigBlind = newBigBlind;
        
        Debug.Log($"Blind levels updated: SB={smallBlind}, BB={bigBlind}");
    }

    /// <summary>
    /// Получает текущие размеры блайндов
    /// </summary>
    public (int smallBlind, int bigBlind) GetBlindLevels()
    {
        return (smallBlind, bigBlind);
    }

    /// <summary>
    /// Начинает новую раздачу
    /// </summary>
    public void StartNewHand()
    {
        if (isHandInProgress)
        {
            Debug.LogWarning("Hand already in progress");
            return;
        }

        var activePlayers = GetActivePlayers();
        if (activePlayers.Count < 2)
        {
            Debug.LogWarning("Need at least 2 players to start hand");
            return;
        }

        // Проверяем, что блайнды можно поставить
        if (!ValidateBlindRequirements())
        {
            Debug.LogError("Cannot start hand: blind requirements not met");
            return;
        }

        isHandInProgress = true;
        currentPhase = GamePhase.PreFlop;
        
        // Очищаем карты на столе
        communityCards.Clear();
        
        // Создаем новую колоду и тасуем
        deck = new Deck();
        deck.Shuffle();
        
        // Раздаем только карты игрокам
        DealPlayerCards();
        
        Debug.Log($"=== STARTING NEW HAND ===");
        Debug.Log($"Dealer at seat {dealerIndex}");
        Debug.Log($"Active players: {activePlayers.Count}");
        
        // Подготовка к новой раздаче
        PrepareNewHand();
        
        // Создаем и тасуем колоду
        deck = new Deck();
        deck.Shuffle();
        
        // Раздаем карты
        DealHoleCards();
        
        // Ставим блайнды
        PostBlinds();
        
        // Переходим к префлопу
        SetPhase(GamePhase.PreFlop);
        
        // Начинаем торговлю
        StartBettingRound();
        
        Debug.Log($"=== HAND STARTED ===");
    }

    /// <summary>
    /// Обрабатывает действие игрока
    /// </summary>
    public bool ProcessPlayerAction(string action, int amount = 0)
    {
        if (currentPlayerIndex == -1 || currentPlayerIndex >= players.Count)
        {
            Debug.LogWarning("No active player");
            return false;
        }

        var player = players[currentPlayerIndex];
        if (!player.CanAct)
        {
            Debug.LogWarning($"Player {player.Name} cannot act");
            return false;
        }

        bool actionProcessed = false;
        
        switch (action.ToLower())
        {
            case "fold":
                player.Fold();
                actionProcessed = true;
                break;
                
            case "check":
                if (player.CurrentBet == currentBet)
                {
                    actionProcessed = true;
                }
                break;
                
            case "call":
                var callAmount = currentBet - player.CurrentBet;
                if (callAmount > 0)
                {
                    var actualBet = player.MakeBet(callAmount);
                    pots[0] += actualBet;
                    actionProcessed = true;
                }
                break;
                
            case "raise":
            case "bet":
                if (amount > currentBet)
                {
                    var totalBet = amount - player.CurrentBet;
                    var actualBet = player.MakeBet(totalBet);
                    pots[0] += actualBet;
                    currentBet = player.CurrentBet;
                    actionProcessed = true;
                }
                break;
                
            case "allin":
                var allInAmount = player.MakeBet(player.Stack);
                pots[0] += allInAmount;
                if (player.CurrentBet > currentBet)
                {
                    currentBet = player.CurrentBet;
                }
                actionProcessed = true;
                break;
        }

        if (actionProcessed)
        {
            OnPlayerAction?.Invoke(player, action, amount);
            Debug.Log($"{player.Name} {action} {amount}");
            
            // Обновляем UI
            if (seatsLayout != null)
            {
                seatsLayout.UpdatePlayerUI(player);
            }
            
            // Переходим к следующему игроку или завершаем раунд
            if (IsBettingRoundComplete())
            {
                EndBettingRound();
            }
            else
            {
                NextPlayer();
            }
        }

        return actionProcessed;
    }

    /// <summary>
    /// Подготавливает игроков к новой раздаче
    /// </summary>
    private void PrepareNewHand()
    {
        communityCards.Clear();
        pots.Clear();
        pots.Add(0);
        currentBet = 0;
        currentPlayerIndex = -1;
        
        foreach (var player in players)
        {
            player.PrepareForNewHand();
        }
    }

    /// <summary>
    /// Раздает карманные карты игрокам
    /// </summary>
    private void DealHoleCards()
    {
        var activePlayers = GetActivePlayers();
        
        // Раздаем по 2 карты каждому игроку
        for (int cardIndex = 0; cardIndex < 2; cardIndex++)
        {
            foreach (var player in activePlayers)
            {
                player.HoleCards[cardIndex] = deck.Draw();
            }
        }
        
        Debug.Log($"Dealt hole cards to {activePlayers.Count} players");
    }

    /// <summary>
    /// Ставит блайнды
    /// </summary>
    private void PostBlinds()
    {
        var activePlayers = GetActivePlayers();
        if (activePlayers.Count < 2) 
        {
            Debug.LogWarning("Cannot post blinds: need at least 2 active players");
            return;
        }

        // Определяем позиции для блайндов
        int smallBlindSeat, bigBlindSeat;
        
        if (activePlayers.Count == 2)
        {
            // Хедз-ап: дилер ставит малый блайнд
            smallBlindSeat = dealerIndex;
            bigBlindSeat = GetNextActivePlayerIndex(dealerIndex);
        }
        else
        {
            // Обычная игра: малый блайнд слева от дилера
            smallBlindSeat = GetNextActivePlayerIndex(dealerIndex);
            bigBlindSeat = GetNextActivePlayerIndex(smallBlindSeat);
        }

        var smallBlindPlayer = GetPlayerBySeatIndex(smallBlindSeat);
        var bigBlindPlayer = GetPlayerBySeatIndex(bigBlindSeat);

        if (smallBlindPlayer == null || bigBlindPlayer == null)
        {
            Debug.LogError("Cannot find blind players");
            return;
        }

        // Ставим малый блайнд
        var sbAmount = PostBlind(smallBlindPlayer, smallBlind, "Small Blind");
        
        // Ставим большой блайнд
        var bbAmount = PostBlind(bigBlindPlayer, bigBlind, "Big Blind");
        
        // Обновляем банк и текущую ставку
        pots[0] += sbAmount + bbAmount;
        currentBet = Math.Max(sbAmount, bbAmount); // На случай если кто-то пошел олл-ин
        
        // Определяем, кто начинает торговлю
        if (activePlayers.Count == 2)
        {
            // Хедз-ап: большой блайнд начинает
            bettingRoundStartPlayer = bigBlindSeat;
        }
        else
        {
            // Обычная игра: игрок после большого блайнда
            bettingRoundStartPlayer = GetNextActivePlayerIndex(bigBlindSeat);
        }
        
        Debug.Log($"Blinds posted - SB: {smallBlindPlayer.Name} ({sbAmount}), BB: {bigBlindPlayer.Name} ({bbAmount})");
        Debug.Log($"Current bet: {currentBet}, Pot: {pots[0]}");
        
        // Обновляем UI
        if (seatsLayout != null)
        {
            seatsLayout.UpdatePlayerUI(smallBlindPlayer);
            seatsLayout.UpdatePlayerUI(bigBlindPlayer);
        }
    }

    /// <summary>
    /// Ставит блайнд для конкретного игрока
    /// </summary>
    private int PostBlind(Player player, int blindAmount, string blindType)
    {
        if (player.Stack <= blindAmount)
        {
            // Игрок идет олл-ин на блайнде
            var allInAmount = player.MakeBet(player.Stack);
            Debug.Log($"{player.Name} goes all-in for {blindType}: {allInAmount} (short of {blindAmount - allInAmount})");
            OnPlayerAction?.Invoke(player, $"{blindType} (All-In)", allInAmount);
            return allInAmount;
        }
        else
        {
            // Обычная постановка блайнда
            var actualAmount = player.MakeBet(blindAmount);
            Debug.Log($"{player.Name} posts {blindType}: {actualAmount}");
            OnPlayerAction?.Invoke(player, blindType, actualAmount);
            return actualAmount;
        }
    }

    /// <summary>
    /// Начинает раунд торговли
    /// </summary>
    private void StartBettingRound()
    {
        currentPlayerIndex = bettingRoundStartPlayer;
        OnPlayerTurn?.Invoke(players[currentPlayerIndex]);

        // Явно включаем ActionPanelController, чтобы панель всегда появлялась
        var actionPanel = FindObjectOfType<ActionPanelController>();
        if (actionPanel != null)
            actionPanel.SetPanelEnabled(true);
    }

    /// <summary>
    /// Переходит к следующему игроку
    /// </summary>
    private void NextPlayer()
    {
        var activePlayers = GetActivePlayers();
        if (activePlayers.Count == 0) return;
        // currentPlayerIndex теперь индекс в списке activePlayers
        int idx = activePlayers.FindIndex(p => p == players[currentPlayerIndex]);
        idx = (idx + 1) % activePlayers.Count;
        currentPlayerIndex = players.IndexOf(activePlayers[idx]);
        OnPlayerTurn?.Invoke(players[currentPlayerIndex]);
    }

    /// <summary>
    /// Проверяет, завершен ли раунд торговли
    /// </summary>
    private bool IsBettingRoundComplete()
    {
        var activePlayers = GetActivePlayers().Where(p => p.Status == PlayerStatus.Active).ToList();
        
        if (activePlayers.Count <= 1)
            return true;

        // Все активные игроки должны иметь одинаковые ставки
        return activePlayers.All(p => p.CurrentBet == currentBet);
    }

    /// <summary>
    /// Завершает раунд торговли
    /// </summary>
    private void EndBettingRound()
    {
        // Сбрасываем ставки игроков для следующего раунда
        foreach (var player in players)
        {
            player.ResetBet();
        }
        
        currentBet = 0;
        currentPlayerIndex = -1;

        // Переходим к следующей фазе
        switch (currentPhase)
        {
            case GamePhase.PreFlop:
                DealCommunityCards(3); // Флоп
                SetPhase(GamePhase.Flop);
                break;
            case GamePhase.Flop:
                DealCommunityCards(1); // Терн
                SetPhase(GamePhase.Turn);
                break;
            case GamePhase.Turn:
                DealCommunityCards(1); // Ривер
                SetPhase(GamePhase.River);
                break;
            case GamePhase.River:
                SetPhase(GamePhase.Showdown);
                StartShowdown();
                return;
        }

        // Начинаем новый раунд торговли (кроме шоудауна)
        bettingRoundStartPlayer = GetNextActivePlayerIndex(dealerIndex);
        StartBettingRound();
    }

    /// <summary>
    /// Раздает общие карты
    /// </summary>
    private void DealCommunityCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Сжигаем карту (как в реальном покере)
            deck.Draw();
            
            // Добавляем карту на борд
            var card = deck.Draw();
            communityCards.Add(card);
        }
        
        Debug.Log($"Dealt {count} community cards. Total: {communityCards.Count}");
    }

    /// <summary>
    /// Начинает шоудаун
    /// </summary>
    private void StartShowdown()
    {
        var contenders = GetActivePlayers().Where(p => p.IsInHand).ToList();
        OnShowdown?.Invoke(contenders);
        
        // TODO: Реализовать определение победителя
        Debug.Log($"Showdown with {contenders.Count} players");
        
        // Пока что просто завершаем раздачу
        EndHand();
    }

    /// <summary>
    /// Завершает текущую раздачу
    /// </summary>
    private void EndHand()
    {
        SetPhase(GamePhase.HandComplete);
        isHandInProgress = false;
        
        // Сдвигаем дилера
        dealerIndex = GetNextActivePlayerIndex(dealerIndex);
        
        Debug.Log("Hand completed");
    }

    /// <summary>
    /// Устанавливает текущую фазу игры
    /// </summary>
    private void SetPhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);
        Debug.Log($"Phase changed to: {newPhase}");
    }

    /// <summary>
    /// Получает список активных игроков
    /// </summary>
    private List<Player> GetActivePlayers()
    {
        return players.Where(p => p.Status != PlayerStatus.SittingOut && p.Stack > 0).ToList();
    }

    /// <summary>
    /// Получает индекс следующего активного игрока
    /// </summary>
    private int GetNextActivePlayerIndex(int currentIndex)
    {
        var activePlayers = GetActivePlayers();
        if (activePlayers.Count == 0) return -1;
        // currentIndex — индекс в players
        var currentPlayer = players[currentIndex];
        int idx = activePlayers.FindIndex(p => p == currentPlayer);
        idx = (idx + 1) % activePlayers.Count;
        return players.IndexOf(activePlayers[idx]);
    }

    /// <summary>
    /// Получает игрока по индексу места
    /// </summary>
    private Player GetPlayerBySeatIndex(int seatIndex)
    {
        return players.FirstOrDefault(p => p.SeatIndex == seatIndex);
    }

    /// <summary>
    /// Получает позицию малого блайнда
    /// </summary>
    public int GetSmallBlindPosition()
    {
        var activePlayers = GetActivePlayers();
        if (activePlayers.Count < 2) return -1;

        if (activePlayers.Count == 2)
        {
            // Хедз-ап: дилер = малый блайнд
            return dealerIndex;
        }
        else
        {
            // Обычная игра: малый блайнд слева от дилера
            return GetNextActivePlayerIndex(dealerIndex);
        }
    }

    /// <summary>
    /// Получает позицию большого блайнда
    /// </summary>
    public int GetBigBlindPosition()
    {
        var activePlayers = GetActivePlayers();
        if (activePlayers.Count < 2) return -1;

        int smallBlindPos = GetSmallBlindPosition();
        return GetNextActivePlayerIndex(smallBlindPos);
    }

    /// <summary>
    /// Проверяет, может ли игрок поставить блайнд
    /// </summary>
    public bool CanPostBlind(Player player, int blindAmount)
    {
        return player != null && player.CanAct && player.Stack > 0;
    }

    /// <summary>
    /// Получает информацию о текущих блайндах
    /// </summary>
    public (Player smallBlind, Player bigBlind, int sbAmount, int bbAmount) GetBlindInfo()
    {
        var activePlayers = GetActivePlayers();
        if (activePlayers.Count < 2) 
            return (null, null, 0, 0);

        int sbPos = GetSmallBlindPosition();
        int bbPos = GetBigBlindPosition();
        
        var sbPlayer = GetPlayerBySeatIndex(sbPos);
        var bbPlayer = GetPlayerBySeatIndex(bbPos);
        
        int sbAmount = sbPlayer != null ? Math.Min(smallBlind, sbPlayer.Stack) : 0;
        int bbAmount = bbPlayer != null ? Math.Min(bigBlind, bbPlayer.Stack) : 0;
        
        return (sbPlayer, bbPlayer, sbAmount, bbAmount);
    }

    /// <summary>
    /// Проверяет, достаточно ли у игроков фишек для блайндов
    /// </summary>
    public bool ValidateBlindRequirements()
    {
        var (sbPlayer, bbPlayer, sbAmount, bbAmount) = GetBlindInfo();
        
        if (sbPlayer == null || bbPlayer == null)
        {
            Debug.LogWarning("Cannot find players for blind positions");
            return false;
        }

        if (sbAmount == 0 && bbAmount == 0)
        {
            Debug.LogWarning("Both blind players have no chips");
            return false;
        }

        return true;
    }
}