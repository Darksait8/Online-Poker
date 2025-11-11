using System;
using System.Collections.Generic;
using System.Linq;

namespace HoldemPlayerContract.Player
{
    public class HandState : IHand
    {
        private readonly List<HandAction> _actions = new List<HandAction>();
        private readonly Dictionary<int, PlayerHandState> _players;
        private readonly List<Card> _communityCards = new List<Card>();

        public HandState(IEnumerable<PlayerInfo> players, int smallBlind, int bigBlind)
        {
            _players = players.ToDictionary(p => p.PlayerNum, p=> new PlayerHandState(p));
            SmallBlind = smallBlind;
            BigBlind = bigBlind;
        }

        public int SmallBlind { get; }
        public int BigBlind { get; }
        public Stage Stage { get; private set; }

        public IReadOnlyCollection<Card> CommunityCards => _communityCards.ToList();

        public void SetCommunityCard(Card card, EBoardCardType cardType)
        {
            _communityCards.Add(card);
        }

        public void SetAction(Stage stage, int playerId, ActionType action, int amount)
        {
            CheckUpdateStage(stage);

            var player = _players[playerId];

            var playerAction = new PokerActionHistory(stage, action, amount);
            
            player.SetAction(playerAction);

            switch (action)
            {
                case ActionType.Blind:
                case ActionType.Fold:
                case ActionType.Check:
                case ActionType.Call:
                case ActionType.Raise:
                    _actions.Add(new HandAction(player.Player, playerAction));
                    //player.SetAction(pAction);
                    break;
                case ActionType.Show:
                    break;
                case ActionType.Win:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private void CheckUpdateStage(Stage stage)
        {
            if (stage != Stage)
            {
                if (stage != (Stage + 1) && stage != Stage.StageShowdown)
                {
                    throw new Exception($"Can not move from stage {Stage} to stage {stage}");
                }
                Stage = stage;
                foreach (var player in _players.Values)
                {
                    player.UpdateStage(stage);
                }
            }
        }

        public PlayerHandState GetPlayer(int playerId)
        {
            return _players[playerId];
        }
    }
}