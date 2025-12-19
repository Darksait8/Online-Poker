using System;
using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemBots.HardBot
{
    /// <summary>
    /// Тяжелый бот - умный, просчитывает стол, агрессивный, хорошо играет
    /// </summary>
    public class HardBot : BaseBot
    {
        private HoldemPlayerContract.Card _hole1;
        private HoldemPlayerContract.Card _hole2;
        private List<HoldemPlayerContract.Card> _board;
        private int _playerNum;
        private Dictionary<int, PlayerActionHistory> _playerHistories;

        private class PlayerActionHistory
        {
            public int raisesCount;
            public int callsCount;
            public int foldsCount;
            public bool isAggressive;
        }

        public override string Name => "HardBot";

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            _playerNum = playerNum;
            _board = new List<HoldemPlayerContract.Card>();
            _playerHistories = new Dictionary<int, PlayerActionHistory>();
        }

        public override void ReceiveHoleCards(HoldemPlayerContract.Card hole1, HoldemPlayerContract.Card hole2)
        {
            _hole1 = hole1;
            _hole2 = hole2;
        }

        public override void SeeBoardCard(EBoardCardType cardType, HoldemPlayerContract.Card boardCard)
        {
            _board.Add(boardCard);
        }

        public override void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
        {
            if (!_playerHistories.ContainsKey(playerNum))
            {
                _playerHistories[playerNum] = new PlayerActionHistory();
            }

            var history = _playerHistories[playerNum];
            if (action == ActionType.Raise)
            {
                history.raisesCount++;
                history.isAggressive = history.raisesCount > history.callsCount;
            }
            else if (action == ActionType.Call)
            {
                history.callsCount++;
            }
            else if (action == ActionType.Fold)
            {
                history.foldsCount++;
            }
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

            // Оцениваем силу руки
            int handStrength = EvaluateHandStrength(stage);

            // Рассчитываем pot odds
            float potOdds = callAmount > 0 ? (float)callAmount / (potSize + callAmount) : 0f;

            // Агрессивная стратегия для сильных рук
            if (handStrength >= 8) // Очень сильная рука (пара тузов, королей, или лучше)
            {
                if (raisesRemaining > 0 && minRaise > 0)
                {
                    // Агрессивный рейз для извлечения максимальной ценности
                    yourAction = ActionType.Raise;
                    amount = minRaise;
                    // Иногда делаем больший рейз для сильных рук
                    if (handStrength >= 9 && maxRaise > minRaise)
                    {
                        amount = Math.Min(minRaise * 2, maxRaise);
                    }
                }
                else
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
            }
            else if (handStrength >= 6) // Хорошая рука
            {
                if (callAmount == 0)
                {
                    // Бесплатно - всегда коллируем или рейзим
                    if (raisesRemaining > 0 && minRaise > 0)
                    {
                        yourAction = ActionType.Raise;
                        amount = minRaise;
                    }
                    else
                    {
                        yourAction = ActionType.Call;
                        amount = callAmount;
                    }
                }
                else if (potOdds < 0.3f) // Хорошие pot odds
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
                else if (raisesRemaining > 0 && minRaise > 0)
                {
                    // Полу-блеф или value bet
                    yourAction = ActionType.Raise;
                    amount = minRaise;
                }
                else
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
            }
            else if (handStrength >= 4) // Средняя рука
            {
                if (callAmount == 0)
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
                else if (potOdds < 0.2f && callAmount < potSize / 10) // Очень хорошие pot odds и небольшая ставка
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
                else
                {
                    yourAction = ActionType.Fold;
                    amount = 0;
                }
            }
            else // Слабая рука
            {
                if (callAmount == 0)
                {
                    yourAction = ActionType.Call; // Бесплатно - коллируем
                    amount = callAmount;
                }
                else
                {
                    yourAction = ActionType.Fold;
                    amount = 0;
                }
            }
        }

        private int EvaluateHandStrength(Stage stage)
        {
            bool isPair = _hole1.Rank == _hole2.Rank;
            bool isSuited = _hole1.Suit == _hole2.Suit;
            ERankType highRank = _hole1.Rank > _hole2.Rank ? _hole1.Rank : _hole2.Rank;
            ERankType lowRank = _hole1.Rank > _hole2.Rank ? _hole2.Rank : _hole1.Rank;
            int gap = highRank - lowRank;

            // Preflop оценка
            if (stage == Stage.StagePreflop)
            {
                if (isPair)
                {
                    // Пары оцениваются по рангу
                    if (highRank >= ERankType.RankAce) return 10;
                    if (highRank >= ERankType.RankKing) return 9;
                    if (highRank >= ERankType.RankQueen) return 8;
                    if (highRank >= ERankType.RankJack) return 7;
                    if (highRank >= ERankType.RankTen) return 6;
                    if (highRank >= ERankType.RankSeven) return 5;
                    return 4;
                }
                else
                {
                    // Высокие карты
                    if (highRank >= ERankType.RankAce && lowRank >= ERankType.RankKing) return 9;
                    if (highRank >= ERankType.RankAce && lowRank >= ERankType.RankQueen) return 8;
                    if (highRank >= ERankType.RankKing && lowRank >= ERankType.RankQueen) return 7;
                    if (highRank >= ERankType.RankAce && lowRank >= ERankType.RankJack) return 7;
                    if (highRank >= ERankType.RankKing && lowRank >= ERankType.RankJack) return 6;
                    if (highRank >= ERankType.RankAce && lowRank >= ERankType.RankTen) return 6;
                    
                    // Связанные карты
                    if (isSuited && gap <= 1 && highRank >= ERankType.RankJack) return 6;
                    if (isSuited && gap <= 2 && highRank >= ERankType.RankTen) return 5;
                    if (gap <= 1 && highRank >= ERankType.RankTen) return 5;
                    
                    return 3;
                }
            }
            else
            {
                // Postflop - упрощенная оценка (в реальности нужно анализировать доску)
                int baseStrength = isPair ? 5 : 3;
                if (highRank >= ERankType.RankAce) baseStrength += 2;
                else if (highRank >= ERankType.RankKing) baseStrength += 1;
                
                // Если есть доска, увеличиваем силу для пар и выше
                if (_board != null && _board.Count > 0)
                {
                    // Упрощенная логика - если есть пара на доске или выше, увеличиваем силу
                    baseStrength += 1;
                }
                
                return Math.Min(10, baseStrength);
            }
        }
    }
}

