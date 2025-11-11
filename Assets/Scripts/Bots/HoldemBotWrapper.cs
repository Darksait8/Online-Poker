using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        var table = _tableGetter();
        var stage = ToStage(request.Round);

        var contributionsSnapshot = GetContributionsSnapshot();
        int callLevel = contributionsSnapshot.Values.Count > 0 ? contributionsSnapshot.Values.Max() : table.CurrentBid;
        contributionsSnapshot.TryGetValue(_corePlayer, out var myContribution);
        int callAmount = Math.Max(0, callLevel - myContribution);

        int lastFullRaise = Math.Max(_lastFullRaiseGetter(), table.Settings.BigBlind);
        int minRaise = callAmount + lastFullRaise;

        int maxRaise = callAmount + Math.Min(_corePlayer.TokensCount, GetMaxOpponentContribution(contributionsSnapshot));
        if (maxRaise < callAmount)
            maxRaise = callAmount;

        int raisesRemaining = _raisesPerRoundGetter();
        if (raisesRemaining <= 0)
            raisesRemaining = 0;

        int potSize = Math.Max(table.TokensInGame, contributionsSnapshot.Values.Sum());

        _bot.GetAction(stage,
                       _corePlayer.PlayersCurrentBet,
                       callAmount,
                       minRaise,
                       maxRaise,
                       raisesRemaining,
                       potSize,
                       out var action,
                       out var amount);

        var decision = ConvertAction(action, amount, callAmount);
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

    private Dictionary<CorePlayer, int> GetContributionsSnapshot()
    {
        return _contributions.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    private int GetMaxOpponentContribution(Dictionary<CorePlayer, int> contributions)
    {
        int maxOpponentCall = 0;

        foreach (var pair in contributions)
        {
            if (pair.Key == _corePlayer)
                continue;

            _stackAtHandStart.TryGetValue(pair.Key, out var stackAtStart);
            int maxContribution = Math.Min(stackAtStart, pair.Value + pair.Key.TokensCount + pair.Key.PlayersCurrentBet);
            if (maxContribution > maxOpponentCall)
                maxOpponentCall = maxContribution;
        }

        return maxOpponentCall;
    }

    private PlayerDecision ConvertAction(ActionType action, int amount, int callAmount)
    {
        var availableTokens = _corePlayer.TokensCount;

        switch (action)
        {
            case ActionType.Fold:
                if (callAmount == 0)
                    return new PlayerDecision(PlayerDecisionType.Check);
                return new PlayerDecision(PlayerDecisionType.Fold);

            case ActionType.Check:
                return new PlayerDecision(PlayerDecisionType.Check);

            case ActionType.Call:
                int callValue = Math.Min(callAmount, availableTokens + _corePlayer.PlayersCurrentBet);
                if (callValue <= 0)
                    return new PlayerDecision(PlayerDecisionType.Check);
                return new PlayerDecision(PlayerDecisionType.Call);

            case ActionType.Raise:
                int totalBet = Math.Max(amount, callAmount);
                totalBet = Math.Min(totalBet, callAmount + availableTokens);
                int raiseAmount = Math.Max(0, totalBet - callAmount);
                if (raiseAmount <= 0)
                {
                    if (callAmount <= 0)
                        return new PlayerDecision(PlayerDecisionType.Check);
                    return new PlayerDecision(PlayerDecisionType.Call);
                }
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

        switch (originalDecision.Type)
        {
            case PlayerDecisionType.Fold:
            case PlayerDecisionType.Check:
                return new PlayerDecision(PlayerDecisionType.Call);
            case PlayerDecisionType.Call:
                return new PlayerDecision(PlayerDecisionType.Raise, Math.Max(1, callAmount / 2));
            case PlayerDecisionType.Raise:
                return new PlayerDecision(PlayerDecisionType.Call);
            default:
                return originalDecision;
        }
    }
}

