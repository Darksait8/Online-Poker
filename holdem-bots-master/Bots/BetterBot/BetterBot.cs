using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using HoldemPlayerContract;

namespace BetterBot
{
    public class BetterBot : BaseBot
    {
        private int _playerNum;
        private Card _hole1;
        private Card _hole2;
        private Card [] _board;

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            // This is called once at the start of the game. playerNum is your unique identifer for the game
            _playerNum = playerNum;
            _board = new Card[5];
        }

        public override string Name
        {
            // return the name of your player
            get
            {
                return "BetterBot";
            }
        }

        public override void ReceiveHoleCards(Card hole1, Card hole2)
        {
            // receive your hole cards for this hand
            _hole1 = hole1;
            _hole2 = hole2;
        }

        public override void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            amount = 0;
            yourAction = ActionType.Fold;

            // This is the bit where you need to put the AI (mostly likely based on info you receive in other methods)
            if (stage == Stage.StagePreflop)
            {
                GetPreFlopAction(callAmount, minRaise, maxRaise, raisesRemaining, potSize, out yourAction, out amount);
            }
            else if (stage == Stage.StageFlop)
            {
                GetFlopAction(callAmount, minRaise, maxRaise, raisesRemaining, potSize, out yourAction, out amount);
            }
            else if (stage == Stage.StageTurn)
            {
                GetTurnAction(callAmount, minRaise, maxRaise, raisesRemaining, potSize, out yourAction, out amount);
            }
            else if (stage == Stage.StageRiver)
            {
                GetRiverAction(callAmount, minRaise, maxRaise, raisesRemaining, potSize, out yourAction, out amount);
            }
            else if (stage == Stage.StageShowdown)
            {
                GetShowdownAction(callAmount, minRaise, maxRaise, raisesRemaining, potSize, out yourAction, out amount);
            }
        }

        private void GetPreFlopAction(int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            bool bIsPair = false;
            bool bIsSuited = false;
            ERankType highRank;
            ERankType lowRank;
            int gap;

            amount = 0;
            yourAction = ActionType.Fold;

            if (_hole1.Rank == _hole2.Rank) 
            {
                bIsPair = true;
                lowRank = highRank = _hole1.Rank;
            }
            else if (_hole1.Rank > _hole2.Rank)
            {
                highRank = _hole1.Rank;
                lowRank = _hole2.Rank;
            }
            else
            {
                highRank = _hole2.Rank;
                lowRank = _hole1.Rank;
            }

            gap = highRank - lowRank;
    
            if(_hole1.Suit == _hole2.Suit)
            {
                bIsSuited = true;
            }

            if (bIsPair)
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
                else if (bIsSuited && gap == 1)
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
            }
        }

        private void GetFlopAction(int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            yourAction = ActionType.Raise;
            amount = callAmount;
        }

        private void GetTurnAction(int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            yourAction = ActionType.Raise;
            amount = callAmount;
        }

        private void GetRiverAction(int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            yourAction = ActionType.Raise;
            amount = callAmount;
        }

        private void GetShowdownAction(int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            // if stage is the showdown then choose whether to show your hand or fold
            yourAction = ActionType.Show;
            amount = 0;
        }

        public override void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            // this is called to inform you of the board cards (3 flop cards, turn and river)
            _board[(int)cardType] = boardCard;
        }
    }
}
