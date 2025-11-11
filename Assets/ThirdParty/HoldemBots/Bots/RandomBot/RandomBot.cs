using System;
using HoldemPlayerContract;

namespace HoldemBots.RandomBot
{
    public class RandomBot : BaseBot
    {
        private int _playerNum;
        private Random _rnd;

        public override void InitPlayer(int playerNum, GameConfig gameConfig, System.Collections.Generic.Dictionary<string, string> playerConfigSettings)
        {
            _playerNum = playerNum;
            int seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
            seed += _playerNum;
            _rnd = new Random(seed);
        }

        public override string Name => "RandomBot";

        public override void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            yourAction = ActionType.Fold;
            amount = 0;

            if (stage == Stage.StageShowdown)
            {
                yourAction = ActionType.Show;
                amount = 0;
                return;
            }

            int actionNum = _rnd.Next(100);

            if (actionNum < 20)
            {
                yourAction = ActionType.Fold;
                amount = 0;
            }
            else if (actionNum < 60)
            {
                yourAction = ActionType.Call;
                amount = callAmount;
            }
            else
            {
                yourAction = ActionType.Raise;
                amount = minRaise;
            }
        }
    }
}

