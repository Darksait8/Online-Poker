using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemController
{
    internal interface IEventHandler
    {
        void Initialise(List<PlayerInfo> players);
        void BeginHand(int dealerPlayerId);
        void TakeBlinds(int playerId, int amount);
        void DealHand(int playerId, Card card1, Card card2);
        void BeginStage(Stage stage, Card[] cards);
        void AwaitingPlayerAction(int playerId);
        void PlayerActionPerformed(int playerId, int stackSize, ActionType action, int betAmount);
        void EndStage(List<Pot> pots); // might not be needed
        void DistributeWinnings(IDictionary<int, int> playerWinnings);
        void EndHand(); // clear some junk?
        void EndGame();
    }
}