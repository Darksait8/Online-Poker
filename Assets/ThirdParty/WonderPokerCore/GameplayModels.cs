using System;
using System.Threading;
using System.Threading.Tasks;

namespace WonderPokerCore
{
    public enum GameRound
    {
        PreFlop = 0,
        Flop = 1,
        Turn = 2,
        River = 3,
        Showdown = 4
    }

    public enum PlayerDecisionType
    {
        Fold,
        Check,
        Call,
        Raise,
        AllIn
    }

    public readonly struct PlayerDecision
    {
        public PlayerDecision(PlayerDecisionType type, int amount = 0)
        {
            Type = type;
            Amount = amount;
        }

        public PlayerDecisionType Type { get; }
        /// <summary>
        /// For raises – amount over call that the player wants to invest.
        /// </summary>
        public int Amount { get; }

        public override string ToString() =>
            Type switch
            {
                PlayerDecisionType.Raise => $"Raise({Amount})",
                _ => Type.ToString()
            };
    }

    public sealed class DecisionRequest
    {
        public DecisionRequest(GameTable table, Player player, GameRound round)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Round = round;
        }

        public GameTable Table { get; }
        public Player Player { get; }
        public GameRound Round { get; }
    }

    public interface IPlayerDecisionProvider
    {
        Task<PlayerDecision> RequestDecisionAsync(DecisionRequest request, CancellationToken cancellationToken);
    }
}

