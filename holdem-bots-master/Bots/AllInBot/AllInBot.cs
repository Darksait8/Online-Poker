using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HoldemPlayerContract;
using HoldemPlayerContract.Player;

namespace AllInBot
{
    // this bot always goes allin at every opportunity.
    // This is especially useful when testing side pots are working correctly. i.e. use multiple allin bots with different starting stack sizes
    public class AllInBot : BaseBot
    {
        public override string Name
        {
            get
            {
                return "AllInBot";
            }
        }

        public override void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            yourAction = ActionType.Raise;
            amount = maxRaise;
        }
    }

    public class AwesomePlayer : PokerPlayer
    {
        private readonly Random _rnd;

        public AwesomePlayer() : base("Awesome Player")
        {
            _rnd = new Random();
        }

        protected override PokerAction PerformAction(int betSize, int callAmount, int minRaise, int potSize)
        {
            switch (_rnd.Next(0, 5))
            {
                case 0:
                    return PokerAction.Call();
                case 1:
                    return PokerAction.Check();
                case 2:
                    return PokerAction.BetOrRaise(minRaise);
                case 3:
                    return PokerAction.Fold();
                case 4:
                    return PokerAction.Show();
                default:
                    throw new NotImplementedException();
            }
        }

        public override void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
            
        }
    }
}
