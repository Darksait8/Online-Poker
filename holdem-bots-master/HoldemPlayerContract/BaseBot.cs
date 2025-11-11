
using System;
using System.IO;
using System.Collections.Generic;

namespace HoldemPlayerContract
{
    public abstract class BaseBot : MarshalByRefObject, IHoldemPlayer
    {
        public virtual void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            // This is called once at the start of the game. playerNum is your unique identifer for the game
        }

        public abstract string Name
        {
            // return the name of your player
            get;
        }

        public virtual bool IsObserver
        {
            get
            {
                return false;
            }
        }

        public virtual void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int smallBlindSize, int bigBlindSize)
        {
            // this is called at the start of every hand and tells you the current status of all players (e.g. if is alive and stack size and who is dealer)
        }

        public virtual void ReceiveHoleCards(Card hole1, Card hole2)
        {
            // receive your hole cards for this hand
        }

        public virtual void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
        {
            // this is called to inform you when any player (including yourself) makes an action (eg puts in blinds, checks, folds, calls, raises, or wins hand)
        }

        // This is the bit where you need to put the AI (mostly likely based on info you receive in other methods)
        public virtual void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            yourAction = ActionType.Fold;
            amount = 0;
        }

        public virtual void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            // this is called to inform you of the board cards (3 flop cards, turn and river)
        }

        public virtual void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
            // this is called to inform you of another players hand during the show down. 
            // bestHand is the best hand that they can form with their hole cards and the five board cards
        }

        public virtual void EndOfGame(int numPlayers, List<PlayerInfo> players)
        {
        }

    }
}
