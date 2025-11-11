using System;
using System.Collections.Generic;

namespace HoldemPlayerContract
{
    [Serializable]
    public class Hand
    {
        private readonly Card[] _cards;
        private readonly int[] _rankCount;
        private readonly int[] _suitCount;

        private EHandType _rank;
        private ERankType[] _subRank;
        private int _numSubRanks;

        public EHandType HandRank()
        {
            return _rank;
        }

        public int NumSubRanks()
        {
            return _numSubRanks;
        }

        public ERankType SubRank(int level)
        {
            return _subRank[level];
        }

        public Hand(IReadOnlyList<Card> pCards)
        {
            if (pCards.Count < 5)
            {
                throw new Exception("not enough cards to form a hand");
            }

            _cards = new Card[5];
            _rankCount = new int[13];
            _suitCount = new int[4];
            _subRank = new ERankType[5];

            for (var i = 0; i < 5; i++)
            {
                _cards[i] = pCards[i];
            }

            Evaluate();

            for (var i = _numSubRanks; i < 5; i++)
            {
                _subRank[i] = ERankType.RankUnknown;
            }
        }

        public string HandValueStr()
        {
            string strVal = "";
            for (int i = 0; i < 5; i++)
            {
                strVal += _cards[i].ValueStr() + " ";
            }
            return strVal;
        }

        public string HandRankStr()
        {
            return _rank switch
            {
                EHandType.HandStraightFlush => "Straight Flush",
                EHandType.HandFours => "Four of a kind",
                EHandType.HandFullHouse => "Full House",
                EHandType.HandFlush => "Flush",
                EHandType.HandStraight => "Straight",
                EHandType.HandThrees => "Three of a kind",
                EHandType.HandTwoPair => "Two pair",
                EHandType.HandPair => "Pair",
                EHandType.HandRunt => "Runt",
                _ => "???"
            };
        }

        public static int CompareHands(Hand pHand1, Hand pHand2)
        {
            if (pHand1.HandRank() > pHand2.HandRank())
                return -1;
            if (pHand2.HandRank() > pHand1.HandRank())
                return 1;

            int numSubRanks = pHand1.NumSubRanks();
            for (int currSubRank = 0; currSubRank < numSubRanks; currSubRank++)
            {
                if (pHand1.SubRank(currSubRank) > pHand2.SubRank(currSubRank))
                    return -1;
                if (pHand2.SubRank(currSubRank) > pHand1.SubRank(currSubRank))
                    return 1;
            }
            return 0;
        }

        public int Compare(Hand otherHand)
        {
            return CompareHands(otherHand, this);
        }

        public static Hand FindPlayersBestHand(IReadOnlyList<Card> pocketCards, IReadOnlyList<Card> board)
        {
            if (pocketCards.Count != 2)
                throw new Exception("must supply exactly 2 pocket cards");
            if (board.Count < 3)
                throw new Exception("not enough board cards");
            if (board.Count > 5)
                throw new Exception("too many board cards");

            var allCards = new List<Card>(pocketCards);
            allCards.AddRange(board);
            return FindPlayersBestHand(allCards);
        }

        public static Hand FindPlayersBestHand(IReadOnlyList<Card> cards)
        {
            if (cards.Count < 5)
                throw new Exception("not enough cards to find best hand");
            if (cards.Count > 7)
                throw new Exception("too many cards to find best hand");

            var currHandCards = new Card[5];
            var bestHand = new Hand(cards);

            if (cards.Count == 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    int currCard = 0;
                    for (int k = 0; k < 6; k++)
                    {
                        if (k != i)
                        {
                            currHandCards[currCard] = cards[k];
                            currCard++;
                        }
                    }

                    var currHand = new Hand(currHandCards);
                    if (CompareHands(currHand, bestHand) == -1)
                    {
                        bestHand = currHand;
                    }
                }
            }
            else if (cards.Count == 7)
            {
                for (int i = 0; i < 7; i++)
                {
                    for (int j = i + 1; j < 7; j++)
                    {
                        int currCard = 0;
                        for (int k = 0; k < 7; k++)
                        {
                            if (k != i && k != j)
                            {
                                currHandCards[currCard] = cards[k];
                                currCard++;
                            }
                        }

                        var currHand = new Hand(currHandCards);
                        if (CompareHands(currHand, bestHand) == -1)
                        {
                            bestHand = currHand;
                        }
                    }
                }
            }

            return bestHand;
        }

        private void Evaluate()
        {
            SortByRank();
            CalcRankCount();
            CalcSuitCount();

            if (IsStraightFlush(ref _numSubRanks, ref _subRank))
            {
                _rank = EHandType.HandStraightFlush;
                return;
            }

            if (IsFours(ref _numSubRanks, ref _subRank))
            {
                _rank = EHandType.HandFours;
                return;
            }

            if (IsFullHouse(ref _numSubRanks, ref _subRank))
            {
                _rank = EHandType.HandFullHouse;
                return;
            }

            if (IsFlush(ref _numSubRanks, ref _subRank))
            {
                _rank = EHandType.HandFlush;
                return;
            }

            if (IsStraight(ref _numSubRanks, ref _subRank))
            {
                _rank = EHandType.HandStraight;
                return;
            }

            if (IsThrees(ref _numSubRanks, ref _subRank))
            {
                _rank = EHandType.HandThrees;
                return;
            }

            if (IsTwoPair(ref _numSubRanks, ref _subRank))
            {
                _rank = EHandType.HandTwoPair;
                return;
            }

            if (IsPair(ref _numSubRanks, ref _subRank))
            {
                _rank = EHandType.HandPair;
                return;
            }

            _rank = EHandType.HandRunt;
            _numSubRanks = 5;
            for (int i = 0; i < 5; i++)
            {
                _subRank[i] = _cards[i].Rank;
            }
        }

        private bool IsStraightFlush(ref int numSubRanks, ref ERankType[] subRank)
        {
            int flushNumSubRanks = 0;
            var flushSubRank = new ERankType[5];
            return IsStraight(ref numSubRanks, ref subRank) && IsFlush(ref flushNumSubRanks, ref flushSubRank);
        }

        private bool IsFours(ref int numSubRanks, ref ERankType[] subRank)
        {
            numSubRanks = 2;
            for (int currRank = 0; currRank < 13; currRank++)
            {
                if (_rankCount[currRank] == 4)
                {
                    subRank[0] = (ERankType)Enum.ToObject(typeof(ERankType), currRank);
                    for (int currCard = 0; currCard < 5; currCard++)
                    {
                        if (_cards[currCard].Rank != subRank[0])
                        {
                            subRank[1] = _cards[currCard].Rank;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsFullHouse(ref int numSubRanks, ref ERankType[] subRank)
        {
            int threesNumSubRanks = 0;
            var threesSubRank = new ERankType[5];
            int pairNumSubRanks = 0;
            var pairSubRank = new ERankType[5];

            if (IsThrees(ref threesNumSubRanks, ref threesSubRank) &&
                IsPair(ref pairNumSubRanks, ref pairSubRank))
            {
                numSubRanks = 2;
                subRank[0] = threesSubRank[0];
                subRank[1] = pairSubRank[0];
                return true;
            }
            return false;
        }

        private bool IsFlush(ref int numSubRanks, ref ERankType[] subRank)
        {
            numSubRanks = 5;
            for (int i = 0; i < 4; i++)
            {
                if ((_suitCount[i] != 0) && (_suitCount[i] != 5))
                {
                    return false;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                subRank[i] = _cards[i].Rank;
            }
            return true;
        }

        private bool IsStraight(ref int numSubRanks, ref ERankType[] subRank)
        {
            numSubRanks = 1;
            ERankType lowRank = ERankType.RankUnknown;
            ERankType highRank = ERankType.RankUnknown;

            for (int i = 0; i < 13; i++)
            {
                if (_rankCount[i] > 1)
                    return false;

                if (_rankCount[i] == 1)
                {
                    highRank = (ERankType)Enum.ToObject(typeof(ERankType), i);
                    if (lowRank == ERankType.RankUnknown)
                        lowRank = (ERankType)Enum.ToObject(typeof(ERankType), i);
                }
            }

            subRank[0] = highRank;

            if (highRank - lowRank == 4)
                return true;

            if (lowRank == ERankType.RankTwo && highRank == ERankType.RankAce)
            {
                for (int i = (int)ERankType.RankSix; i <= (int)ERankType.RankKing; i++)
                {
                    if (_rankCount[i] > 0)
                    {
                        return false;
                    }
                }
                subRank[0] = ERankType.RankFive;
                return true;
            }

            return false;
        }

        private bool IsThrees(ref int numSubRanks, ref ERankType[] subRank)
        {
            numSubRanks = 3;
            for (ERankType currRank = 0; (int)currRank < 13; currRank++)
            {
                if (_rankCount[(int)currRank] == 3)
                {
                    subRank[0] = currRank;
                    int currSubRank = 1;
                    for (int currCard = 0; currCard < 5; currCard++)
                    {
                        if (_cards[currCard].Rank != subRank[0])
                        {
                            subRank[currSubRank] = _cards[currCard].Rank;
                            currSubRank++;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private bool IsTwoPair(ref int numSubRanks, ref ERankType[] subRank)
        {
            numSubRanks = 3;
            ERankType highPairRank = ERankType.RankUnknown;
            ERankType lowPairRank = ERankType.RankUnknown;
            ERankType oddCardRank = ERankType.RankUnknown;
            int pairCount = 0;

            for (ERankType currRank = 0; (int)currRank < 13; currRank++)
            {
                if (_rankCount[(int)currRank] == 2)
                {
                    pairCount++;
                    if (highPairRank != ERankType.RankUnknown)
                        lowPairRank = highPairRank;
                    highPairRank = currRank;
                }
                else if (_rankCount[(int)currRank] == 1)
                {
                    oddCardRank = currRank;
                }
            }

            if (pairCount == 2)
            {
                subRank[0] = highPairRank;
                subRank[1] = lowPairRank;
                subRank[2] = oddCardRank;
                return true;
            }
            return false;
        }

        private bool IsPair(ref int numSubRanks, ref ERankType[] subRank)
        {
            numSubRanks = 4;
            for (ERankType currRank = 0; (int)currRank < 13; currRank++)
            {
                if (_rankCount[(int)currRank] == 2)
                {
                    subRank[0] = currRank;
                    int currSubRank = 1;
                    for (int currCard = 0; currCard < 5; currCard++)
                    {
                        if (_cards[currCard].Rank != subRank[0])
                        {
                            subRank[currSubRank] = _cards[currCard].Rank;
                            currSubRank++;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private void SortByRank()
        {
            var sortedCards = new Card[5];
            sortedCards[0] = _cards[0];

            for (int j = 1; j < 5; j++)
            {
                var key = _cards[j];
                int i = j - 1;
                while (i >= 0 && sortedCards[i].Rank < key.Rank)
                {
                    sortedCards[i + 1] = sortedCards[i];
                    i--;
                }
                sortedCards[i + 1] = key;
            }

            for (int i = 0; i < 5; i++)
            {
                _cards[i] = sortedCards[i];
            }
        }

        private void CalcRankCount()
        {
            for (int i = 0; i < 13; i++)
            {
                _rankCount[i] = 0;
            }

            for (int i = 0; i < 5; i++)
            {
                _rankCount[(int)_cards[i].Rank]++;
            }
        }

        private void CalcSuitCount()
        {
            for (int i = 0; i < 4; i++)
            {
                _suitCount[i] = 0;
            }

            for (int i = 0; i < 5; i++)
            {
                _suitCount[(int)_cards[i].Suit]++;
            }
        }
    }
}

