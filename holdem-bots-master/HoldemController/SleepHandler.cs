using System.Collections.Generic;
using System.Threading;
using HoldemPlayerContract;

namespace HoldemController
{
    internal class SleepHandler : IEventHandler
    {
        private int _delayMilliseconds;

        public SleepHandler(int delayMilliseconds)
        {
            UpdateDelay(delayMilliseconds);
        }

        public void UpdateDelay(int delayMilliseconds)
        {
            _delayMilliseconds = delayMilliseconds;
        }

        public void Initialise(List<PlayerInfo> players)
        {
            
        }

        public void BeginHand(int dealerPlayerId)
        {
            
        }

        public void TakeBlinds(int playerId, int amount)
        {
            
        }

        public void DealHand(int playerId, Card card1, Card card2)
        {
            
        }

        public void BeginStage(Stage stage, Card[] cards)
        {
            
        }

        public void AwaitingPlayerAction(int playerId)
        {
            Thread.Sleep(_delayMilliseconds);
        }

        public void PlayerActionPerformed(int playerId, int stackSize, ActionType action, int betAmount)
        {
            
        }

        public void EndStage(List<Pot> pots)
        {
            Thread.Sleep(_delayMilliseconds);
        }

        public void DistributeWinnings(IDictionary<int, int> playerWinnings)
        {
            Thread.Sleep(_delayMilliseconds);
        }

        public void EndHand()
        {
            Thread.Sleep(_delayMilliseconds);
        }

        public void EndGame()
        {
            Thread.Sleep(_delayMilliseconds);
        }
    }
}