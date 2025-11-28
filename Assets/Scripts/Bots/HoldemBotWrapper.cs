using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using HoldemPlayerContract;
using WonderPokerCore;
using CorePlayer = WonderPokerCore.Player;
using HoldemCard = HoldemPlayerContract.Card;
using HoldemPlayerInfo = HoldemPlayerContract.PlayerInfo;
using HoldemHand = HoldemPlayerContract.Hand;

public sealed class HoldemBotWrapper : IPlayerDecisionProvider
{
    private readonly int _playerIndex;
    private readonly CorePlayer _corePlayer;
    private readonly IHoldemPlayer _bot;
    private readonly Dictionary<CorePlayer, int> _contributions;
    private readonly Dictionary<CorePlayer, int> _stackAtHandStart;
    private readonly Func<int> _lastFullRaiseGetter;
    private readonly Func<int> _raisesPerRoundGetter;
    private readonly Func<GameTable> _tableGetter;
    private readonly System.Random _random = new();
    private readonly int _minThinkMilliseconds = 400;
    private readonly int _maxThinkMilliseconds = 1200;
    private readonly float _mistakeProbability = 0.08f;

    public HoldemBotWrapper(int playerIndex,
                            CorePlayer corePlayer,
                            IHoldemPlayer botInstance,
                            Dictionary<CorePlayer, int> contributions,
                            Dictionary<CorePlayer, int> stackAtHandStart,
                            Func<int> lastFullRaiseGetter,
                            Func<int> raisesPerRoundGetter,
                            Func<GameTable> tableGetter)
    {
        _playerIndex = playerIndex;
        _corePlayer = corePlayer ?? throw new ArgumentNullException(nameof(corePlayer));
        _bot = botInstance ?? throw new ArgumentNullException(nameof(botInstance));
        _contributions = contributions ?? throw new ArgumentNullException(nameof(contributions));
        _stackAtHandStart = stackAtHandStart ?? throw new ArgumentNullException(nameof(stackAtHandStart));
        _lastFullRaiseGetter = lastFullRaiseGetter ?? (() => 0);
        _raisesPerRoundGetter = raisesPerRoundGetter ?? (() => int.MaxValue);
        _tableGetter = tableGetter ?? throw new ArgumentNullException(nameof(tableGetter));
    }

    public void InitPlayer(GameConfig config, Dictionary<string, string> settings)
    {
        _bot.InitPlayer(_playerIndex, config, settings ?? new Dictionary<string, string>());
    }

    public void InitHand(int handNum, int numPlayers, List<HoldemPlayerInfo> players, int dealerId, int smallBlind, int bigBlind)
    {
        _bot.InitHand(handNum, numPlayers, players, dealerId, smallBlind, bigBlind);
    }

    public void ReceiveHoleCards(HoldemCard card1, HoldemCard card2)
    {
        _bot.ReceiveHoleCards(card1, card2);
    }

    public void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
    {
        _bot.SeeAction(stage, playerNum, action, amount);
    }

    public void SeeBoardCard(EBoardCardType type, HoldemCard card)
    {
        _bot.SeeBoardCard(type, card);
    }

    public void SeePlayerHand(int playerNum, HoldemCard hole1, HoldemCard hole2, HoldemHand bestHand)
    {
        _bot.SeePlayerHand(playerNum, hole1, hole2, bestHand);
    }

    public void EndOfGame(int numPlayers, List<HoldemPlayerInfo> players)
    {
        _bot.EndOfGame(numPlayers, players);
    }

    public async Task<PlayerDecision> RequestDecisionAsync(DecisionRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            var table = _tableGetter();
            if (table == null)
            {
                Debug.LogError($"[Bot {_corePlayer?.Nick ?? "Unknown"}] Table is null, defaulting to Fold");
                return new PlayerDecision(PlayerDecisionType.Fold);
            }

            var stage = ToStage(request.Round);

            var contributionsSnapshot = GetContributionsSnapshot();
            int callLevel = contributionsSnapshot.Values.Count > 0 ? contributionsSnapshot.Values.Max() : table.CurrentBid;
            contributionsSnapshot.TryGetValue(_corePlayer, out var myContribution);
            
            int availableTokens = _corePlayer?.TokensCount ?? 0;
            int currentBet = _corePlayer?.PlayersCurrentBet ?? 0;
            int maxAffordableCall = availableTokens + currentBet;
            
            // Ограничиваем callAmount доступными фишками бота
            int callAmount = Math.Max(0, Math.Min(callLevel - myContribution, maxAffordableCall));
            
            // Если callAmount больше доступных фишек, бот должен сделать олл-ин или фолд
            bool mustAllInOrFold = callAmount >= availableTokens && callAmount > 0;

            int lastFullRaise = Math.Max(_lastFullRaiseGetter(), table.Settings?.BigBlind ?? 20);
            
            // Ограничиваем minRaise доступными фишками
            int minRaise = Math.Min(callAmount + lastFullRaise, callAmount + availableTokens);
            
            // Ограничиваем maxRaise доступными фишками бота
            int maxOpponentContribution = GetMaxOpponentContribution(contributionsSnapshot);
            int maxRaise = Math.Min(callAmount + availableTokens, callAmount + Math.Min(availableTokens, maxOpponentContribution));
            if (maxRaise < callAmount)
                maxRaise = callAmount;
            
            // Если бот не может сделать минимальный рейз, ограничиваем minRaise
            if (minRaise > callAmount + availableTokens)
                minRaise = callAmount + availableTokens;

            int raisesRemaining = _raisesPerRoundGetter();
            if (raisesRemaining <= 0)
                raisesRemaining = 0;

            int potSize = Math.Max(table.TokensInGame, contributionsSnapshot.Values.Sum());

            // Защита от зависания бота - ограничиваем параметры разумными значениями
            callAmount = Math.Max(0, Math.Min(callAmount, availableTokens + currentBet));
            minRaise = Math.Max(0, Math.Min(minRaise, availableTokens + currentBet));
            maxRaise = Math.Max(0, Math.Min(maxRaise, availableTokens + currentBet));
            potSize = Math.Max(0, Math.Min(potSize, int.MaxValue / 2)); // Защита от переполнения

            // Дополнительная проверка для предотвращения некорректных значений
            if (callAmount < 0 || minRaise < 0 || maxRaise < 0 || potSize < 0)
            {
                Debug.LogError($"[Bot {_corePlayer?.Nick ?? "Unknown"}] Invalid parameters: callAmount={callAmount}, minRaise={minRaise}, maxRaise={maxRaise}, potSize={potSize}. Defaulting to Fold.");
                return new PlayerDecision(PlayerDecisionType.Fold);
            }

            ActionType action;
            int amount;
            
            try
            {
                _bot.GetAction(stage,
                               currentBet,
                               callAmount,
                               minRaise,
                               maxRaise,
                               raisesRemaining,
                               potSize,
                               out action,
                               out amount);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bot {_corePlayer?.Nick ?? "Unknown"}] GetAction threw exception: {ex.Message}. Stack trace: {ex.StackTrace}. Defaulting to Fold.");
                action = ActionType.Fold;
                amount = 0;
            }

            // Проверяем корректность ответа бота
            if (amount < 0)
                amount = 0;
            if (amount > availableTokens)
                amount = availableTokens;

            var decision = ConvertAction(action, amount, callAmount, mustAllInOrFold);
            
            // Если бот должен сделать олл-ин или фолд, но выбрал другое действие, конвертируем в олл-ин
            if (mustAllInOrFold && decision.Type != PlayerDecisionType.Fold && decision.Type != PlayerDecisionType.AllIn)
            {
                // Если есть фишки, делаем олл-ин, иначе фолд
                if (availableTokens > 0)
                    decision = new PlayerDecision(PlayerDecisionType.AllIn, availableTokens);
                else
                    decision = new PlayerDecision(PlayerDecisionType.Fold);
            }
            
            decision = MaybeInjectMistake(decision, callAmount);

            int delay = _random.Next(_minThinkMilliseconds, _maxThinkMilliseconds + 1);
            if (delay > 0)
            {
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // пропускаем задержку, если отменено
                }
            }

            return decision;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Bot {_corePlayer?.Nick ?? "Unknown"}] RequestDecisionAsync exception: {ex.Message}. Stack trace: {ex.StackTrace}. Defaulting to Fold.");
            return new PlayerDecision(PlayerDecisionType.Fold);
        }
    }

    private Dictionary<CorePlayer, int> GetContributionsSnapshot()
    {
        return _contributions.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    private int GetMaxOpponentContribution(Dictionary<CorePlayer, int> contributions)
    {
        int maxOpponentCall = 0;
        int botMaxAffordable = _corePlayer.TokensCount + _corePlayer.PlayersCurrentBet;

        foreach (var pair in contributions)
        {
            if (pair.Key == _corePlayer)
                continue;

            _stackAtHandStart.TryGetValue(pair.Key, out var stackAtStart);
            int maxContribution = Math.Min(stackAtStart, pair.Value + pair.Key.TokensCount + pair.Key.PlayersCurrentBet);
            
            // Ограничиваем максимальный вклад оппонента доступными фишками бота
            // чтобы избежать проблем с очень большими ставками
            maxContribution = Math.Min(maxContribution, botMaxAffordable);
            
            if (maxContribution > maxOpponentCall)
                maxOpponentCall = maxContribution;
        }

        return maxOpponentCall;
    }

    private PlayerDecision ConvertAction(ActionType action, int amount, int callAmount, bool mustAllInOrFold = false)
    {
        var availableTokens = _corePlayer.TokensCount;
        int maxAffordable = availableTokens + _corePlayer.PlayersCurrentBet;

        switch (action)
        {
            case ActionType.Fold:
                if (callAmount == 0)
                    return new PlayerDecision(PlayerDecisionType.Check);
                return new PlayerDecision(PlayerDecisionType.Fold);

            case ActionType.Check:
                // Если нужно сделать олл-ин или фолд, нельзя чекнуть
                if (mustAllInOrFold && callAmount > 0)
                    return new PlayerDecision(PlayerDecisionType.Fold);
                return new PlayerDecision(PlayerDecisionType.Check);

            case ActionType.Call:
                int callValue = Math.Min(callAmount, maxAffordable);
                if (callValue <= 0)
                    return new PlayerDecision(PlayerDecisionType.Check);
                
                // Если колл требует всех фишек, это олл-ин
                if (callValue >= availableTokens)
                    return new PlayerDecision(PlayerDecisionType.AllIn, availableTokens);
                
                return new PlayerDecision(PlayerDecisionType.Call);

            case ActionType.Raise:
                int totalBet = Math.Max(amount, callAmount);
                totalBet = Math.Min(totalBet, callAmount + availableTokens);
                int raiseAmount = Math.Max(0, totalBet - callAmount);
                
                if (raiseAmount <= 0)
                {
                    if (callAmount <= 0)
                        return new PlayerDecision(PlayerDecisionType.Check);
                    
                    // Если колл требует всех фишек, это олл-ин
                    if (callAmount >= availableTokens)
                        return new PlayerDecision(PlayerDecisionType.AllIn, availableTokens);
                    
                    return new PlayerDecision(PlayerDecisionType.Call);
                }
                
                // Если рейз требует всех фишек, это олл-ин
                if (raiseAmount >= availableTokens || totalBet >= callAmount + availableTokens)
                    return new PlayerDecision(PlayerDecisionType.AllIn, availableTokens);
                
                return new PlayerDecision(PlayerDecisionType.Raise, raiseAmount);

            case ActionType.Show:
                return new PlayerDecision(PlayerDecisionType.Check);

            default:
                return new PlayerDecision(PlayerDecisionType.Fold);
        }
    }

    private static Stage ToStage(GameRound round)
    {
        return round switch
        {
            GameRound.PreFlop => Stage.StagePreflop,
            GameRound.Flop => Stage.StageFlop,
            GameRound.Turn => Stage.StageTurn,
            GameRound.River => Stage.StageRiver,
            _ => Stage.StageShowdown
        };
    }

    private PlayerDecision MaybeInjectMistake(PlayerDecision originalDecision, int callAmount)
    {
        if (_random.NextDouble() > _mistakeProbability)
            return originalDecision;

        int availableTokens = _corePlayer.TokensCount;
        bool mustAllInOrFold = callAmount >= availableTokens && callAmount > 0;
        
        // Не меняем решение, если бот должен сделать олл-ин или фолд
        if (mustAllInOrFold && (originalDecision.Type == PlayerDecisionType.AllIn || originalDecision.Type == PlayerDecisionType.Fold))
            return originalDecision;

        switch (originalDecision.Type)
        {
            case PlayerDecisionType.Fold:
            case PlayerDecisionType.Check:
                // Если нужно сделать олл-ин, не меняем на колл
                if (mustAllInOrFold)
                    return originalDecision;
                return new PlayerDecision(PlayerDecisionType.Call);
            case PlayerDecisionType.Call:
                // Если колл требует всех фишек, не меняем на рейз
                if (callAmount >= availableTokens)
                    return originalDecision;
                int raiseAmount = Math.Max(1, Math.Min(callAmount / 2, availableTokens));
                return new PlayerDecision(PlayerDecisionType.Raise, raiseAmount);
            case PlayerDecisionType.Raise:
                return new PlayerDecision(PlayerDecisionType.Call);
            case PlayerDecisionType.AllIn:
                // Не меняем олл-ин на другое действие
                return originalDecision;
            default:
                return originalDecision;
        }
    }
}

