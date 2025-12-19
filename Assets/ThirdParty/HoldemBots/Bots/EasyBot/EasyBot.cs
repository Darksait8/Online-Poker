using System;
using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemBots.EasyBot
{
    /// <summary>
    /// Легкий бот - тупой и боязливый, часто фолдит, редко рейзит
    /// </summary>
    public class EasyBot : BaseBot
    {
        private HoldemPlayerContract.Card _hole1;
        private HoldemPlayerContract.Card _hole2;
        private Random _rnd;

        public override string Name => "EasyBot";

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            int seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
            seed += playerNum;
            _rnd = new Random(seed);
        }

        public override void ReceiveHoleCards(HoldemPlayerContract.Card hole1, HoldemPlayerContract.Card hole2)
        {
            _hole1 = hole1;
            _hole2 = hole2;
        }

        public override void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            amount = 0;
            yourAction = ActionType.Fold;

            if (stage == Stage.StageShowdown)
            {
                yourAction = ActionType.Show;
                amount = 0;
                return;
            }

            // Легкий бот очень боязливый - фолдит в 70% случаев
            int randomValue = _rnd.Next(100);

            // Если нужно коллировать больше 10% от стека, фолдим почти всегда
            // (это упрощенная проверка, в реальности нужно знать размер стека)
            if (callAmount > 0 && randomValue < 70)
            {
                yourAction = ActionType.Fold;
                amount = 0;
                return;
            }

            // Очень редко рейзим (только с очень сильными картами)
            bool isPair = _hole1.Rank == _hole2.Rank;
            bool isHighPair = isPair && (_hole1.Rank >= ERankType.RankKing);
            bool isHighCards = !isPair && ((_hole1.Rank >= ERankType.RankAce) || (_hole2.Rank >= ERankType.RankAce));

            if (isHighPair && randomValue > 90)
            {
                yourAction = ActionType.Raise;
                amount = minRaise;
            }
            else if (callAmount == 0 || (randomValue > 50 && randomValue < 70))
            {
                // Иногда коллируем, если бесплатно или случайно
                yourAction = ActionType.Call;
                amount = callAmount;
            }
            else
            {
                // В остальных случаях фолдим
                yourAction = ActionType.Fold;
                amount = 0;
            }
        }
    }
}

