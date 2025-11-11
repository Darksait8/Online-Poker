using System.Collections.Generic;
using System.Linq;
using HoldemPlayerContract;

namespace HoldemController.ConsoleDisplay
{
    internal class ConsoleDisplayHandler : IEventHandler
    {
        private Dictionary<int, int> _playerStackSize;
        private ConsoleRenderer _renderer;

        public void Initialise(List<PlayerInfo> players)
        {
            _renderer = new ConsoleRenderer(125, 34, players.Count); // todo: shouldn't have to set the width + height here. this is a UI element

            _playerStackSize = new Dictionary<int, int>(players.Count);
            foreach (var player in players)
            {
                var playerId = player.PlayerNum;
                _playerStackSize.Add(playerId, player.StackSize);

                _renderer.UpdatePlayerName(playerId, player.Name);
                _renderer.UpdatePlayerStackSize(playerId, player.StackSize);
            }
        }

        public void BeginHand(int dealerPlayerId)
        {
            // todo: do we update which players are no longer in the hand?
            _renderer.UpdateDealer(dealerPlayerId);
        }

        public void TakeBlinds(int playerId, int amount)
        {
            var stack = (_playerStackSize[playerId] -= amount);
            _playerStackSize[playerId] = stack;
            _renderer.UpdatePlayerAction(playerId, ActionType.Blind, amount);
            _renderer.UpdatePlayerStackSize(playerId, stack);
        }

        public void BeginStage(Stage stage, Card[] cards)
        {
            _renderer.UpdateCommunityCards(cards); // todo: show stage in UI?
        }

        public void AwaitingPlayerAction(int playerId)
        {
            _renderer.UpdatePlayerTurn(playerId);
        }

        public void DealHand(int playerId, Card card1, Card card2)
        {
            _renderer.UpdatePlayerCards(playerId, card1, card2);
        }

        public void PlayerActionPerformed(int playerId, int stackSize, ActionType action, int betAmount)
        {
            _playerStackSize[playerId] = stackSize;
            
            _renderer.UpdatePlayerStackSize(playerId, stackSize);
            _renderer.UpdatePlayerAction(playerId, action, betAmount);

            if (action == ActionType.Fold)
            {
                _renderer.UpdatePlayerCards(playerId, null, null);
            }
        }

        public void EndStage(List<Pot> pots)
        {
            _renderer.UpdatePots(pots.Select(p=>p.Size()));
            foreach (var player in _playerStackSize)
            {
                _renderer.UpdatePlayerAction(player.Key, null, 0);
            }
            _renderer.UpdatePlayerTurn(null);
        }

        public void DistributeWinnings(IDictionary<int, int> playerWinnings)
        {
            foreach (var winnings in playerWinnings)
            {
                var playerId = winnings.Key;
                var amount = winnings.Value;
                var stack = (_playerStackSize[playerId] += amount);
                
                _renderer.UpdatePlayerStackSize(playerId, stack);
                _renderer.UpdatePlayerAction(playerId, ActionType.Win, amount);
            }
            _renderer.UpdatePots(null);
        }

        public void EndHand()
        {
            foreach (var playerId in _playerStackSize.Keys)
            {
                _renderer.UpdatePlayerCards(playerId, null, null);
                _renderer.UpdatePlayerAction(playerId, null, 0);
            }
        }

        public void EndGame()
        {
            
        }
    }
}