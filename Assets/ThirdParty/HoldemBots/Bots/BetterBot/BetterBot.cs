using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemBots.BetterBot
{
    public class BetterBot : BaseBot
    {
        private HoldemPlayerContract.Card _hole1;
        private HoldemPlayerContract.Card _hole2;

        public override string Name => "BetterBot";

        public override void ReceiveHoleCards(HoldemPlayerContract.Card hole1, HoldemPlayerContract.Card hole2)
        {
            _hole1 = hole1;
            _hole2 = hole2;
        }

        public override void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            amount = 0;
            yourAction = ActionType.Fold;

            if (stage == Stage.StagePreflop)
            {
                GetPreFlopAction(callAmount, minRaise, out yourAction, out amount);
            }
            else if (stage == Stage.StageShowdown)
            {
                yourAction = ActionType.Show;
                amount = 0;
            }
            else
            {
                yourAction = ActionType.Call;
                amount = callAmount;
            }
        }

        private void GetPreFlopAction(int callAmount, int minRaise, out ActionType yourAction, out int amount)
        {
            bool isPair = _hole1.Rank == _hole2.Rank;
            bool isSuited = _hole1.Suit == _hole2.Suit;
            ERankType highRank = _hole1.Rank > _hole2.Rank ? _hole1.Rank : _hole2.Rank;
            ERankType lowRank = _hole1.Rank > _hole2.Rank ? _hole2.Rank : _hole1.Rank;

            yourAction = ActionType.Fold;
            amount = 0;

            if (isPair)
            {
                if (highRank >= ERankType.RankEight)
                {
                    yourAction = ActionType.Raise;
                    amount = minRaise;
                }
                else if (highRank >= ERankType.RankFive)
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
            }
            else
            {
                int gap = highRank - lowRank;

                if (highRank >= ERankType.RankKing && lowRank >= ERankType.RankEight)
                {
                    yourAction = ActionType.Raise;
                    amount = minRaise;
                }
                else if (highRank >= ERankType.RankJack)
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
                else if (isSuited && gap == 1)
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
            }
        }
    }
}

