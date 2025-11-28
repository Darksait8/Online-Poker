using System;
using System.Collections.Generic;
using System.Linq;

namespace WonderPokerCore
{
    /// <summary>
    /// Combination evaluator copied from the reference project (kept intentionally close to the original logic).
    /// </summary>
    public class HandsComparer : IComparer<CardsCollection>
    {
        public int Compare(CardsCollection x, CardsCollection y)
        {
            throw new NotImplementedException();
        }

        public bool IsRoyalFlush(CardsCollection cards)
        {
            if (cards.Cards.Count < 5) return false;
            cards.SortDescending();
            return cards.Cards[0].Value == CardValue.Ace &&
                   cards.Cards[1].Value == CardValue.King && cards.Cards[1].Sign == cards.Cards[0].Sign &&
                   cards.Cards[2].Value == CardValue.Queen && cards.Cards[2].Sign == cards.Cards[0].Sign &&
                   cards.Cards[3].Value == CardValue.Jack && cards.Cards[3].Sign == cards.Cards[0].Sign &&
                   cards.Cards[4].Value == CardValue.Ten && cards.Cards[4].Sign == cards.Cards[0].Sign;
        }

        public bool IsStraightFlush(CardsCollection cards)
        {
            cards.SortDescending();
            for (int i = 0; i < cards.Cards.Count - 4; i++)
            {
                int value = (int)cards.Cards[i].Value;
                if ((int)cards.Cards[i + 1].Value == value - 1 && cards.Cards[i + 1].Sign == cards.Cards[i].Sign &&
                    (int)cards.Cards[i + 2].Value == value - 2 && cards.Cards[i + 2].Sign == cards.Cards[i].Sign &&
                    (int)cards.Cards[i + 3].Value == value - 3 && cards.Cards[i + 3].Sign == cards.Cards[i].Sign &&
                    (int)cards.Cards[i + 4].Value == value - 4 && cards.Cards[i + 4].Sign == cards.Cards[i].Sign)
                {
                    return true;
                }
            }
            return false;
        }

        public Card HighestCardOfStraightFlush(CardsCollection cards)
        {
            cards.SortDescending();
            for (int i = 0; i < cards.Cards.Count - 4; i++)
            {
                int value = (int)cards.Cards[i].Value;
                if ((int)cards.Cards[i + 1].Value == value - 1 && cards.Cards[i + 1].Sign == cards.Cards[i].Sign &&
                    (int)cards.Cards[i + 2].Value == value - 2 && cards.Cards[i + 2].Sign == cards.Cards[i].Sign &&
                    (int)cards.Cards[i + 3].Value == value - 3 && cards.Cards[i + 3].Sign == cards.Cards[i].Sign &&
                    (int)cards.Cards[i + 4].Value == value - 4 && cards.Cards[i + 4].Sign == cards.Cards[i].Sign)
                {
                    return cards.Cards[i];
                }
            }
            return null;
        }

        public Card HighestCardOfStraight(CardsCollection cards)
        {
            cards.SortDescending();
            for (int i = 0; i < cards.Cards.Count - 4; i++)
            {
                int value = (int)cards.Cards[i].Value;
                if ((int)cards.Cards[i + 1].Value == value - 1 &&
                    (int)cards.Cards[i + 2].Value == value - 2 &&
                    (int)cards.Cards[i + 3].Value == value - 3 &&
                    (int)cards.Cards[i + 4].Value == value - 4)
                {
                    return cards.Cards[i];
                }
            }
            return null;
        }

        public Card CardOfQuads(CardsCollection cards)
        {
            cards.SortDescending();
            for (int i = 0; i < cards.Cards.Count - 3; i++)
            {
                if (cards.Cards[i].Value == cards.Cards[i + 1].Value &&
                    cards.Cards[i].Value == cards.Cards[i + 2].Value &&
                    cards.Cards[i].Value == cards.Cards[i + 3].Value)
                {
                    return cards.Cards[i];
                }
            }
            return null;
        }

        public bool IsQuads(CardsCollection cards) => CardOfQuads(cards) != null;

        public Card GiveCardOfThree(CardsCollection cards)
        {
            cards.SortDescending();
            for (int i = 0; i < cards.Cards.Count - 2; i++)
            {
                if (cards.Cards[i].Value == cards.Cards[i + 1].Value &&
                    cards.Cards[i].Value == cards.Cards[i + 2].Value)
                {
                    return cards.Cards[i];
                }
            }
            return null;
        }

        public Card GiveCardOfPair(CardsCollection cards)
        {
            cards.SortDescending();
            for (int i = 0; i < cards.Cards.Count - 1; i++)
            {
                if (cards.Cards[i].Value == cards.Cards[i + 1].Value)
                {
                    return cards.Cards[i];
                }
            }
            return null;
        }

        public bool IsFullHouse(CardsCollection cards)
        {
            cards.SortDescending();
            int counterThree = 0;
            int counterPairs = 0;
            for (int i = 0; i < cards.Cards.Count - 1; i++)
            {
                if (counterThree == 0 &&
                    i + 2 < cards.Cards.Count &&
                    cards.Cards[i].Value == cards.Cards[i + 1].Value &&
                    cards.Cards[i].Value == cards.Cards[i + 2].Value)
                {
                    counterThree++;
                    i += 2;
                    continue;
                }

                if (cards.Cards[i].Value == cards.Cards[i + 1].Value)
                {
                    counterPairs++;
                    i++;
                }
            }
            return counterThree == 1 && counterPairs > 0;
        }

        public bool IsFlush(CardsCollection cards)
        {
            int hearts = cards.Cards.Count(c => c.Sign == CardSign.Heart);
            int spades = cards.Cards.Count(c => c.Sign == CardSign.Spade);
            int diamonds = cards.Cards.Count(c => c.Sign == CardSign.Diamond);
            int clubs = cards.Cards.Count(c => c.Sign == CardSign.Club);
            return hearts >= 5 || spades >= 5 || diamonds >= 5 || clubs >= 5;
        }

        public bool IsStraight(CardsCollection cards)
        {
            cards.SortDescending();
            for (int i = 0; i < cards.Cards.Count - 4; i++)
            {
                int value = (int)cards.Cards[i].Value;
                if ((int)cards.Cards[i + 1].Value == value - 1 &&
                    (int)cards.Cards[i + 2].Value == value - 2 &&
                    (int)cards.Cards[i + 3].Value == value - 3 &&
                    (int)cards.Cards[i + 4].Value == value - 4)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsThreeOfKind(CardsCollection cards) => GiveCardOfThree(cards) != null;

        public bool IsTwoPairs(CardsCollection cards)
        {
            cards.SortDescending();
            int pairs = 0;
            for (int i = 0; i < cards.Cards.Count - 1; i++)
            {
                if (cards.Cards[i].Value == cards.Cards[i + 1].Value)
                {
                    pairs++;
                    i++;
                }
            }
            return pairs >= 2;
        }

        public bool IsOnePair(CardsCollection cards) => GiveCardOfPair(cards) != null;

        /// <summary>
        /// Returns value 1 (best) .. 10 (high card) describing strength of the hand.
        /// Lower value == stronger combination (kept consistent with original implementation).
        /// </summary>
        public int EvaluateHand(CardsCollection cards)
        {
            if (IsRoyalFlush(cards)) return 1;
            if (IsStraightFlush(cards)) return 2;
            if (IsQuads(cards)) return 3;
            if (IsFullHouse(cards)) return 4;
            if (IsFlush(cards)) return 5;
            if (IsStraight(cards)) return 6;
            if (IsThreeOfKind(cards)) return 7;
            if (IsTwoPairs(cards)) return 8;
            if (IsOnePair(cards)) return 9;
            return 10;
        }

        /// <summary>
        /// Находит лучшую комбинацию из 7 карт (2 карманные + 5 общих)
        /// </summary>
        public CardsCollection FindBestHand(CardsCollection allCards)
        {
            if (allCards.Cards.Count < 5)
                return allCards;
            
            if (allCards.Cards.Count == 5)
                return allCards;

            CardsCollection bestHand = null;
            int bestScore = int.MaxValue;

            // Если 6 карт - пробуем исключить каждую карту
            if (allCards.Cards.Count == 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    var hand = new CardsCollection();
                    for (int k = 0; k < 6; k++)
                    {
                        if (k != i)
                            hand.Cards.Add(allCards.Cards[k]);
                    }
                    int score = EvaluateHand(hand);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestHand = hand;
                    }
                    else if (score == bestScore)
                    {
                        // При одинаковом ранге сравниваем детально
                        if (CompareHandsDetailed(hand, bestHand) < 0)
                        {
                            bestHand = hand;
                        }
                    }
                }
            }
            // Если 7 карт - пробуем исключить каждую пару карт
            else if (allCards.Cards.Count == 7)
            {
                for (int i = 0; i < 7; i++)
                {
                    for (int j = i + 1; j < 7; j++)
                    {
                        var hand = new CardsCollection();
                        for (int k = 0; k < 7; k++)
                        {
                            if (k != i && k != j)
                                hand.Cards.Add(allCards.Cards[k]);
                        }
                        int score = EvaluateHand(hand);
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestHand = hand;
                        }
                        else if (score == bestScore)
                        {
                            // При одинаковом ранге сравниваем детально
                            if (CompareHandsDetailed(hand, bestHand) < 0)
                            {
                                bestHand = hand;
                            }
                        }
                    }
                }
            }

            return bestHand ?? allCards;
        }

        /// <summary>
        /// Сравнивает две руки детально. Возвращает: -1 если hand1 лучше, 1 если hand2 лучше, 0 если равны
        /// </summary>
        public int CompareHandsDetailed(CardsCollection hand1, CardsCollection hand2)
        {
            int score1 = EvaluateHand(hand1);
            int score2 = EvaluateHand(hand2);

            if (score1 < score2) return -1; // hand1 лучше
            if (score1 > score2) return 1;  // hand2 лучше

            // Одинаковый ранг - сравниваем детально
            hand1.SortDescending();
            hand2.SortDescending();

            // Royal Flush - всегда равны
            if (score1 == 1) return 0;

            // Straight Flush - сравниваем старшую карту
            if (score1 == 2)
            {
                var high1 = HighestCardOfStraightFlush(hand1);
                var high2 = HighestCardOfStraightFlush(hand2);
                if (high1 == null || high2 == null) return 0;
                return high2.Value.CompareTo(high1.Value);
            }

            // Quads (каре) - сравниваем карту каре, потом кикер
            if (score1 == 3)
            {
                var quad1 = CardOfQuads(hand1);
                var quad2 = CardOfQuads(hand2);
                if (quad1 == null || quad2 == null) return 0;
                int quadCompare = quad2.Value.CompareTo(quad1.Value);
                if (quadCompare != 0) return quadCompare;

                // Сравниваем кикер
                var kicker1 = hand1.Cards.First(c => c.Value != quad1.Value);
                var kicker2 = hand2.Cards.First(c => c.Value != quad2.Value);
                return kicker2.Value.CompareTo(kicker1.Value);
            }

            // Full House - сравниваем тройку, потом пару
            if (score1 == 4)
            {
                var three1 = GiveCardOfThree(hand1);
                var three2 = GiveCardOfThree(hand2);
                if (three1 == null || three2 == null) return 0;
                int threeCompare = three2.Value.CompareTo(three1.Value);
                if (threeCompare != 0) return threeCompare;

                var pair1 = hand1.Cards.First(c => c.Value != three1.Value && hand1.Cards.Count(c2 => c2.Value == c.Value) >= 2);
                var pair2 = hand2.Cards.First(c => c.Value != three2.Value && hand2.Cards.Count(c2 => c2.Value == c.Value) >= 2);
                if (pair1 == null || pair2 == null) return 0;
                return pair2.Value.CompareTo(pair1.Value);
            }

            // Flush - сравниваем по старшим картам
            if (score1 == 5)
            {
                for (int i = 0; i < Math.Min(hand1.Cards.Count, hand2.Cards.Count); i++)
                {
                    int compare = hand2.Cards[i].Value.CompareTo(hand1.Cards[i].Value);
                    if (compare != 0) return compare;
                }
                return 0;
            }

            // Straight - сравниваем старшую карту
            if (score1 == 6)
            {
                var high1 = HighestCardOfStraight(hand1);
                var high2 = HighestCardOfStraight(hand2);
                if (high1 == null || high2 == null) return 0;
                return high2.Value.CompareTo(high1.Value);
            }

            // Three of a Kind - сравниваем тройку, потом кикеры
            if (score1 == 7)
            {
                var three1 = GiveCardOfThree(hand1);
                var three2 = GiveCardOfThree(hand2);
                if (three1 == null || three2 == null) return 0;
                int threeCompare = three2.Value.CompareTo(three1.Value);
                if (threeCompare != 0) return threeCompare;

                // Сравниваем кикеры
                var kickers1 = hand1.Cards.Where(c => c.Value != three1.Value).OrderByDescending(c => c.Value).ToList();
                var kickers2 = hand2.Cards.Where(c => c.Value != three2.Value).OrderByDescending(c => c.Value).ToList();
                for (int i = 0; i < Math.Min(kickers1.Count, kickers2.Count); i++)
                {
                    int compare = kickers2[i].Value.CompareTo(kickers1[i].Value);
                    if (compare != 0) return compare;
                }
                return 0;
            }

            // Two Pairs - сравниваем старшую пару, потом младшую, потом кикер
            if (score1 == 8)
            {
                hand1.SortDescending();
                hand2.SortDescending();
                
                // Находим пары
                var pair1Values = new List<CardValue>();
                var pair2Values = new List<CardValue>();
                
                for (int i = 0; i < hand1.Cards.Count - 1; i++)
                {
                    if (hand1.Cards[i].Value == hand1.Cards[i + 1].Value && !pair1Values.Contains(hand1.Cards[i].Value))
                        pair1Values.Add(hand1.Cards[i].Value);
                }
                for (int i = 0; i < hand2.Cards.Count - 1; i++)
                {
                    if (hand2.Cards[i].Value == hand2.Cards[i + 1].Value && !pair2Values.Contains(hand2.Cards[i].Value))
                        pair2Values.Add(hand2.Cards[i].Value);
                }
                
                pair1Values = pair1Values.OrderByDescending(v => v).ToList();
                pair2Values = pair2Values.OrderByDescending(v => v).ToList();
                
                if (pair1Values.Count < 2 || pair2Values.Count < 2) return 0;

                // Старшая пара
                int compare = pair2Values[0].CompareTo(pair1Values[0]);
                if (compare != 0) return compare;

                // Младшая пара
                compare = pair2Values[1].CompareTo(pair1Values[1]);
                if (compare != 0) return compare;

                // Кикер
                var kicker1 = hand1.Cards.FirstOrDefault(c => c.Value != pair1Values[0] && c.Value != pair1Values[1]);
                var kicker2 = hand2.Cards.FirstOrDefault(c => c.Value != pair2Values[0] && c.Value != pair2Values[1]);
                if (kicker1 == null || kicker2 == null) return 0;
                return kicker2.Value.CompareTo(kicker1.Value);
            }

            // One Pair - сравниваем пару, потом кикеры
            if (score1 == 9)
            {
                var pair1 = GiveCardOfPair(hand1);
                var pair2 = GiveCardOfPair(hand2);
                if (pair1 == null || pair2 == null) return 0;
                int pairCompare = pair2.Value.CompareTo(pair1.Value);
                if (pairCompare != 0) return pairCompare;

                // Сравниваем кикеры
                var kickers1 = hand1.Cards.Where(c => c.Value != pair1.Value).OrderByDescending(c => c.Value).ToList();
                var kickers2 = hand2.Cards.Where(c => c.Value != pair2.Value).OrderByDescending(c => c.Value).ToList();
                for (int i = 0; i < Math.Min(kickers1.Count, kickers2.Count); i++)
                {
                    int compare = kickers2[i].Value.CompareTo(kickers1[i].Value);
                    if (compare != 0) return compare;
                }
                return 0;
            }

            // High Card - сравниваем по старшим картам
            for (int i = 0; i < Math.Min(hand1.Cards.Count, hand2.Cards.Count); i++)
            {
                int compare = hand2.Cards[i].Value.CompareTo(hand1.Cards[i].Value);
                if (compare != 0) return compare;
            }
            return 0;
        }
    }
}

