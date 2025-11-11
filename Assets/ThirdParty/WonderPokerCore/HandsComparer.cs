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
    }
}

