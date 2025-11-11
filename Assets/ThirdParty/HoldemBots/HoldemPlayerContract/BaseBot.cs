using System;
using System.Collections.Generic;

namespace HoldemPlayerContract
{
    public abstract class BaseBot : MarshalByRefObject, IHoldemPlayer
    {
        public virtual void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
        }

        public abstract string Name { get; }

        public virtual bool IsObserver => false;

        public virtual void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int smallBlindSize, int bigBlindSize)
        {
        }

        public virtual void ReceiveHoleCards(Card hole1, Card hole2)
        {
        }

        public virtual void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
        {
        }

        public virtual void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            yourAction = ActionType.Fold;
            amount = 0;
        }

        public virtual void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
        }

        public virtual void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
        }

        public virtual void EndOfGame(int numPlayers, List<PlayerInfo> players)
        {
        }
    }
}

