using System;

namespace HoldemPlayerContract.Player
{
    public class PokerAction
    {
        /// <summary>
        /// Use the static methods instead
        /// </summary>
        [Obsolete("Please use the static methods")]
        public PokerAction(ActionType action, int amount)
        {
            Action = action;
            Amount = amount;
        }

        public ActionType Action { get; }
        public int Amount { get; }

        public static PokerAction Check()
        {
            return new PokerAction(ActionType.Check, 0);
        }

        public static PokerAction Call()
        {
            return new PokerAction(ActionType.Call, 0);
        }

        public static PokerAction BetOrRaise(int amount)
        {
            return new PokerAction(ActionType.Raise, amount);
        }

        public static PokerAction Fold()
        {
            return new PokerAction(ActionType.Fold, 0);
        }
        
        public static PokerAction Show()
        {
            return new PokerAction(ActionType.Show, 0);
        }
    }
}