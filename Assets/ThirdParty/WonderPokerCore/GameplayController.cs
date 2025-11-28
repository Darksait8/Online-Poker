using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WonderPokerCore
{
    /// <summary>
    /// Orchestrates a single hand of Texas Hold'em. Originally this class operated together with TCP streams;
    /// here it is refactored to rely on an asynchronous decision provider and a set of events.
    /// </summary>
    public class GameplayController
    {
        private readonly GameTable gameTable;
        private readonly ICardsDealer dealer;
        private readonly IPlayerDecisionProvider decisionProvider;
        private readonly HandsComparer handsComparer = new();

        private int currentRound;
        private int positionOfPlayerWhoRaised = -1;
        private bool gameEndedEarly;

        public GameplayController(GameTable gameTable, ICardsDealer cardsDealer, IPlayerDecisionProvider decisionProvider)
        {
            this.gameTable = gameTable ?? throw new ArgumentNullException(nameof(gameTable));
            dealer = cardsDealer ?? throw new ArgumentNullException(nameof(cardsDealer));
            this.decisionProvider = decisionProvider ?? throw new ArgumentNullException(nameof(decisionProvider));
        }

        public event Action GameStarted;
        public event Action GameEnded;
        public event Action<GameRound> RoundStarted;
        public event Action<Player> PlayerTurnStarted;
        public event Action<Player, PlayerDecision> PlayerActionCommitted;
        public event Action<Player> PlayerFolded;
        public event Action<Player, int> BlindPaid;
        public event Action<Player> DealerButtonChanged;
        public event Action<IReadOnlyList<Player>> WinnersDetermined;
        public event Action<CardsCollection> CommunityCardsUpdated;
        public event Action<string> Log;
        public event Action<Player> PlayerHoleCardsUpdated;
        public event Action<GameRound> RoundEnded;

        public async Task PlayHandAsync(CancellationToken cancellationToken = default)
        {
            if (gameTable.Players.Count < GameTableSettings.MinPlayersCountByRules)
                throw new InvalidOperationException("Not enough players to start the game.");

            gameTable.ResetGameState();
            gameTable.IsGameActive = true;
            gameTable.SortPlayersBySeats();
            gameEndedEarly = false;

            dealer.ChangePosition(gameTable);
            DealerButtonChanged?.Invoke(gameTable.Players[dealer.Position]);
            EmitLog($"Dealer button moved to {gameTable.Players[dealer.Position].Nick}");

            GameStarted?.Invoke();
            EmitLog("Hand started");

            ApplyBlinds();

            currentRound = 0;
            positionOfPlayerWhoRaised = -1;

            while (currentRound < 4 && !cancellationToken.IsCancellationRequested)
            {
                await MakeNextRoundAsync(cancellationToken);
                if (currentRound < 4)
                    positionOfPlayerWhoRaised = -1;

                if (gameEndedEarly)
                    break;
            }

            if (!gameEndedEarly)
            {
                ConcludeGame();
                gameTable.IsGameActive = false;
                GameEnded?.Invoke();
                EmitLog("Hand ended");
            }
        }

        private void ApplyBlinds()
        {
            Player smallBlind = gameTable.Players[GetPositionOfPlayerOffBy(dealer.Position, 1)];
            Player bigBlind = gameTable.Players[GetPositionOfPlayerOffBy(dealer.Position, 2)];

            int smallBlindAmount = Math.Max(1, gameTable.Settings.BigBlind / 2);
            if (smallBlind.PayBlind(smallBlindAmount))
            {
                gameTable.CurrentBid = smallBlind.PlayersCurrentBet;
                positionOfPlayerWhoRaised = GetPositionOfPlayerOffBy(dealer.Position, 1);
                BlindPaid?.Invoke(smallBlind, smallBlindAmount);
                PlayerActionCommitted?.Invoke(smallBlind, new PlayerDecision(PlayerDecisionType.Raise, smallBlindAmount));
                EmitLog($"{smallBlind.Nick} pays small blind {smallBlindAmount}");
            }

            if (bigBlind.PayBlind(gameTable.Settings.BigBlind))
            {
                gameTable.CurrentBid = Math.Max(gameTable.CurrentBid, bigBlind.PlayersCurrentBet);
                positionOfPlayerWhoRaised = GetPositionOfPlayerOffBy(dealer.Position, 2);
                BlindPaid?.Invoke(bigBlind, gameTable.Settings.BigBlind);
                PlayerActionCommitted?.Invoke(bigBlind, new PlayerDecision(PlayerDecisionType.Raise, gameTable.Settings.BigBlind));
                EmitLog($"{bigBlind.Nick} pays big blind {gameTable.Settings.BigBlind}");
            }
        }

        private async Task MakeNextRoundAsync(CancellationToken cancellationToken)
        {
            GameRound round = (GameRound)currentRound;
            RoundStarted?.Invoke(round);
            EmitLog($"Round {round} started");

            switch (round)
            {
                case GameRound.PreFlop:
                    dealer.DealCards(gameTable, 0);
                    foreach (Player player in gameTable.Players)
                        PlayerHoleCardsUpdated?.Invoke(player);
                    break;
                case GameRound.Flop:
                    dealer.DealCards(gameTable, 1);
                    CommunityCardsUpdated?.Invoke(gameTable.ShownHelpingCards);
                    break;
                case GameRound.Turn:
                    dealer.DealCards(gameTable, 2);
                    CommunityCardsUpdated?.Invoke(gameTable.ShownHelpingCards);
                    break;
                case GameRound.River:
                    dealer.DealCards(gameTable, 3);
                    CommunityCardsUpdated?.Invoke(gameTable.ShownHelpingCards);
                    break;
            }

            int startingPlayer = round == GameRound.PreFlop
                ? GetPositionOfPlayerOffBy(GetBigBlindPosition(), 1)
                : GetSmallBlindPosition();

            await MakeTurnAsync(startingPlayer, gameTable.Players.Count, cancellationToken);

            if (gameEndedEarly)
                return;

            RoundEnded?.Invoke(round);
            EmitLog($"Round {round} ended");
            ResetBetsForNextRound();
            currentRound++;
        }

        private async Task MakeTurnAsync(int startingPlayerIndex, int participantsCount, CancellationToken cancellationToken)
        {
            bool equalBets = false;
            int maxRounds = 100; // Защита от бесконечного цикла - максимум 100 раундов торговли
            int roundCount = 0;
            
            while (!equalBets && !cancellationToken.IsCancellationRequested && roundCount < maxRounds)
            {
                roundCount++;
                bool anyActionThisRound = false;
                
                for (int i = 0; i < participantsCount; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
                    int currentIndex = (startingPlayerIndex + i) % gameTable.Players.Count;
                    
                    if (currentIndex < 0 || currentIndex >= gameTable.Players.Count)
                    {
                        EmitLog($"Error: Invalid player index {currentIndex}. Breaking turn loop.");
                        equalBets = true;
                        break;
                    }
                    
                    Player player = gameTable.Players[currentIndex];

                    if (player == null)
                    {
                        EmitLog($"Error: Player at index {currentIndex} is null. Breaking turn loop.");
                        equalBets = true;
                        break;
                    }

                    if (player.Folded || player.AllInMade)
                        continue;

                    if (positionOfPlayerWhoRaised == currentIndex && gameTable.CurrentBid == player.PlayersCurrentBet)
                    {
                        equalBets = true;
                        break;
                    }

                    PlayerTurnStarted?.Invoke(player);
                    EmitLog($"{player.Nick} turn started");

                    bool moveAccepted = false;
                    int maxAttempts = 10; // Защита от бесконечного цикла
                    int attempts = 0;
                    
                    while (!moveAccepted && !cancellationToken.IsCancellationRequested && attempts < maxAttempts)
                    {
                        attempts++;
                        
                        // Добавляем таймаут для запроса решения (максимум 30 секунд)
                        PlayerDecision decision;
                        try
                        {
                            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                            {
                                decision = await decisionProvider.RequestDecisionAsync(
                                    new DecisionRequest(gameTable, player, (GameRound)currentRound),
                                    linkedCts.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            EmitLog($"Error: {player.Nick} decision request timed out or was cancelled. Forcing Fold.");
                            decision = new PlayerDecision(PlayerDecisionType.Fold);
                        }
                        catch (Exception ex)
                        {
                            EmitLog($"Error: {player.Nick} decision request failed: {ex.Message}. Forcing Fold.");
                            decision = new PlayerDecision(PlayerDecisionType.Fold);
                        }

                        // Если бот должен сделать олл-ин (недостаточно фишек для колла), принудительно устанавливаем олл-ин
                        if (decision.Type != PlayerDecisionType.AllIn && 
                            decision.Type != PlayerDecisionType.Fold &&
                            player.TokensCount > 0)
                        {
                            int callAmount = gameTable.CurrentBid - player.PlayersCurrentBet;
                            if (callAmount >= player.TokensCount && callAmount > 0)
                            {
                                EmitLog($"{player.Nick} forced to All-In (insufficient chips for call: {callAmount} >= {player.TokensCount})");
                                decision = new PlayerDecision(PlayerDecisionType.AllIn, player.TokensCount);
                            }
                        }

                        moveAccepted = ApplyDecision(player, decision);
                        if (moveAccepted)
                        {
                            PlayerActionCommitted?.Invoke(player, decision);
                            EmitLog($"{player.Nick} -> {decision.Type} ({decision.Amount})");
                            if (decision.Type == PlayerDecisionType.Fold)
                            {
                                PlayerFolded?.Invoke(player);
                                EmitLog($"{player.Nick} folded");
                            }

                            if (gameEndedEarly)
                                return;
                        }
                        else
                        {
                            EmitLog($"Warning: {player.Nick} decision {decision.Type} ({decision.Amount}) was rejected. Attempt {attempts}/{maxAttempts}");
                            
                            // Если решение не принято несколько раз подряд, принудительно делаем фолд или олл-ин
                            if (attempts >= 3)
                            {
                                int callAmount = gameTable.CurrentBid - player.PlayersCurrentBet;
                                if (player.TokensCount > 0 && callAmount >= player.TokensCount)
                                {
                                    EmitLog($"{player.Nick} forced All-In after {attempts} failed attempts");
                                    decision = new PlayerDecision(PlayerDecisionType.AllIn, player.TokensCount);
                                    moveAccepted = ApplyDecision(player, decision);
                                    if (moveAccepted)
                                    {
                                        PlayerActionCommitted?.Invoke(player, decision);
                                        EmitLog($"{player.Nick} -> All-In (forced)");
                                    }
                                }
                                else if (callAmount > 0)
                                {
                                    EmitLog($"{player.Nick} forced Fold after {attempts} failed attempts");
                                    decision = new PlayerDecision(PlayerDecisionType.Fold);
                                    moveAccepted = ApplyDecision(player, decision);
                                    if (moveAccepted)
                                    {
                                        PlayerActionCommitted?.Invoke(player, decision);
                                        PlayerFolded?.Invoke(player);
                                        EmitLog($"{player.Nick} -> Fold (forced)");
                                    }
                                }
                            }
                        }
                    }
                    
                    if (!moveAccepted && attempts >= maxAttempts)
                    {
                        EmitLog($"Error: {player.Nick} failed to make a valid decision after {maxAttempts} attempts. Forcing fold.");
                        player.Fold();
                        PlayerFolded?.Invoke(player);
                    }

                    if (AllPlayersExceptOneFolded())
                    {
                        EndGameDueToFold();
                        return;
                    }
                    
                    anyActionThisRound = true;
                }
                
                // Если за весь раунд никто не сделал действия, принудительно завершаем
                if (!anyActionThisRound && !equalBets)
                {
                    EmitLog($"Warning: No actions in round {roundCount}, forcing end of betting round");
                    equalBets = true;
                }
                
                // Дополнительная проверка - если слишком много раундов, принудительно завершаем
                if (roundCount >= maxRounds)
                {
                    EmitLog($"Error: Maximum betting rounds ({maxRounds}) reached. Forcing end of betting round.");
                    equalBets = true;
                }
            }
            
            if (roundCount >= maxRounds && !equalBets)
            {
                EmitLog($"Critical: Betting round exceeded maximum rounds. Ending turn with potential game state issues.");
            }
        }

        private bool ApplyDecision(Player player, PlayerDecision decision)
        {
            switch (decision.Type)
            {
                case PlayerDecisionType.Fold:
                    player.Fold();
                    return true;
                case PlayerDecisionType.Check:
                    if (player.CheckOrCall(gameTable.CurrentBid))
                    {
                        UpdateRaiserIfNeeded(player);
                        return true;
                    }
                    return false;
                case PlayerDecisionType.Call:
                    if (player.CheckOrCall(gameTable.CurrentBid))
                    {
                        UpdateRaiserIfNeeded(player);
                        return true;
                    }
                    return false;
                case PlayerDecisionType.AllIn:
                    // Проверяем, что у игрока есть фишки для олл-ина
                    if (player.TokensCount <= 0)
                    {
                        EmitLog($"Warning: {player.Nick} attempted All-In with 0 tokens, forcing fold");
                        player.Fold();
                        return true;
                    }
                    
                    if (player.AllIn())
                    {
                        if (player.PlayersCurrentBet > gameTable.CurrentBid)
                        {
                            gameTable.CurrentBid = player.PlayersCurrentBet;
                            positionOfPlayerWhoRaised = gameTable.Players.IndexOf(player);
                        }
                        return true;
                    }
                    
                    // Если AllIn() вернул false, это ошибка - принудительно делаем фолд
                    EmitLog($"Error: {player.Nick} AllIn() returned false, forcing fold");
                    player.Fold();
                    return true;
                case PlayerDecisionType.Raise:
                    int raiseAmount = Math.Max(0, decision.Amount);
                    if (player.Raise(gameTable.CurrentBid, raiseAmount))
                    {
                        gameTable.CurrentBid = Math.Max(gameTable.CurrentBid, player.PlayersCurrentBet);
                        positionOfPlayerWhoRaised = gameTable.Players.IndexOf(player);
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        private void UpdateRaiserIfNeeded(Player player)
        {
            if (positionOfPlayerWhoRaised == -1)
            {
                positionOfPlayerWhoRaised = gameTable.Players.IndexOf(player);
            }
        }

        private bool AllPlayersExceptOneFolded()
        {
            return gameTable.Players.Count(p => !p.Folded) <= 1;
        }

        private void EndGameDueToFold()
        {
            if (gameEndedEarly)
                return;

            gameEndedEarly = true;
            EmitLog("Hand ended early due to folds");
            ConcludeGame();
            gameTable.IsGameActive = false;
            GameEnded?.Invoke();
            EmitLog("Hand ended");
        }

        private void ResetBetsForNextRound()
        {
            foreach (Player player in gameTable.Players)
            {
                player.PlayersCurrentBet = 0;
            }
            gameTable.CurrentBid = 0;
        }

        private int GetSmallBlindPosition() => GetPositionOfPlayerOffBy(dealer.Position, 1);

        private int GetBigBlindPosition() => GetPositionOfPlayerOffBy(dealer.Position, 2);

        private int GetPositionOfPlayerOffBy(int basePlayerPosition, int relativeOffset)
        {
            return (basePlayerPosition + relativeOffset) % gameTable.Players.Count;
        }

        private void ConcludeGame()
        {
            List<Player> winners = DetermineWinners();
            if (winners.Count == 0)
                return;

            // Simplified payout: winner takes all (same as reference implementation)
            Player winner = winners[0];
            winner.TokensCount += gameTable.TokensInGame;
            winner.XP += 100;

            WinnersDetermined?.Invoke(winners);
            EmitLog($"Winners determined: {string.Join(", ", winners.Select(w => w.Nick))} | Pot {gameTable.TokensInGame}");
        }

        private List<Player> DetermineWinners()
        {
            List<Player> winners = new();
            CardsCollection bestHand = null;

            foreach (Player player in gameTable.Players)
            {
                if (player.Folded)
                    continue;

                // Находим лучшую комбинацию из 7 карт (2 карманные + 5 общих)
                CardsCollection allCards = player.PlayerHand + gameTable.ShownHelpingCards;
                CardsCollection playerBestHand = handsComparer.FindBestHand(allCards);

                if (bestHand == null)
                {
                    // Первый игрок - автоматически лучший пока
                    bestHand = playerBestHand;
                    winners.Clear();
                    winners.Add(player);
                }
                else
                {
                    // Сравниваем с текущим лучшим
                    int comparison = handsComparer.CompareHandsDetailed(playerBestHand, bestHand);
                    
                    if (comparison < 0)
                    {
                        // Этот игрок лучше
                        bestHand = playerBestHand;
                        winners.Clear();
                        winners.Add(player);
                    }
                    else if (comparison == 0)
                    {
                        // Равные руки - добавляем в победители
                        winners.Add(player);
                    }
                    // Если comparison > 0, этот игрок хуже - пропускаем
                }
            }

            return winners;
        }

        private void EmitLog(string message)
        {
            Log?.Invoke(message);
        }
    }
}

