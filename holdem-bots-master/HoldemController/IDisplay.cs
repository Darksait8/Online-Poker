using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemController
{
    internal interface IDisplay
    {
        void Initialise(GameConfig gameConfig, int numPlayers, int sleepAfterActionMilliSeconds); 
        void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int smallBlindSize, int bigBlindSize);
        void DisplayHoleCards(int playerId, Card hole1, Card hole2);
        void DisplayAction(Stage stage, int playerId, ActionType action, int totalAmount, int betSize, int callAmount, int raiseAmount, bool isAllIn, PotManager potMan);
        void DisplayBoardCard(EBoardCardType cardType, Card boardCard);
        void DisplayPlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand);
        void DisplayShowdown(HandRanker handRanker, PotManager potMan);
        void DisplayEndOfGame(int numPlayers, List<PlayerInfo> players);
    }
}
