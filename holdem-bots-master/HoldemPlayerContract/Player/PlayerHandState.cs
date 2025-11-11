using System;
using System.Collections.Generic;

namespace HoldemPlayerContract.Player
{
    public class PlayerHandState : IPlayerHandState
    {
        private Stage _currentStage;

        public PlayerHandState(PlayerInfo playerInfo)
        {
            Player = playerInfo;
            HandActions = new List<PokerActionHistory>();
            StageActions = new List<PokerActionHistory>();
        }

        public PlayerInfo Player { get; }
        public int PlayerId => Player.PlayerNum;
        public int TotalStageBet { get; private set; }
        public Card Card1 { get; private set; }
        public Card Card2 { get; private set; }
        public List<PokerActionHistory> HandActions { get; }
        public List<PokerActionHistory> StageActions { get; private set; }

        public void SetCards(Card card1, Card card2)
        {
            Card1 = card1;
            Card2 = card2;
        }
        public void SetAction(PokerActionHistory action)
        {
            if (action.Stage != _currentStage)
            {
                throw new ArgumentException($"{nameof(action)} does not match the current stage", nameof(action));
            }

            HandActions.Add(action);
            StageActions.Add(action);
            switch (action.Action)
            {
                case ActionType.Blind:
                case ActionType.Call:
                case ActionType.Raise:
                    TotalStageBet += action.Amount;
                    break;
            }
        }

        public void UpdateStage(Stage stage)
        {
            if (_currentStage != stage - 1 && stage != Stage.StageShowdown)
            {
                throw new Exception($"Can not move from stage {_currentStage} to stage {stage}");
            }

            _currentStage = stage;
            StageActions = new List<PokerActionHistory>();
            TotalStageBet = 0;
        }
    }

    public interface IPlayerHandState
    {
        int TotalStageBet { get; }
        Card Card1 { get; }
        Card Card2 { get; }
    }
}