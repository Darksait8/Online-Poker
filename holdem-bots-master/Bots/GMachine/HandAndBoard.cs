using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace GMachine
{
    internal class HandAndBoard
    {
        public PocketCards HoleCards {get; set; }

        public List<Card>  Board {get; set; }

        public Hand CurrentBestHand {get; set; }
        public Hand BestHandAfterDraw {get; set; }

        public bool IsStraightDraw {get; set; }

        public List<ERankType> RanksToMakeStraight {get; set; }

        public bool IsFlushDraw {get; set; }

        public ESuitType SuitToMakeFlush {get; set; }

        public bool IsStraightFlushDraw {get; set; }

        public List<Card> CardsToMakeStraightFlush {get; set; }

        private int[] _rankCount;
        private int[] _suitCount;

        public void Evaluate()
        {
            CurrentBestHand = Hand.FindPlayersBestHand(HoleCards.CardList, Board);

            IsStraightDraw = false;
            IsFlushDraw = false;
            IsStraightFlushDraw = false;
            RanksToMakeStraight = new List<ERankType>();

            List<Card> cards = new List<Card>();

            cards.AddRange(HoleCards.CardList);
            cards.AddRange(Board.ToList());

            // Check for flush draw
            _rankCount = new int[13];
            _suitCount = new int[4];

            // count how many of each rank and suit
            foreach(Card c in cards)
            {
                _rankCount[(int)c.Rank]++;
                _suitCount[(int)c.Suit]++;
            }

            //if at least 4 in same suit then flush draw
            for(int i=0; i<4; i++)
            {
                if(_suitCount[i] >= 4)
                {
                    IsFlushDraw = true;
                    SuitToMakeFlush = (ESuitType)i;
                }
            }

            // if 4 cards in a 5 card range then straight draw
            for(ERankType rank = ERankType.RankFive; rank <= ERankType.RankAce;  rank++)
            {
                int numInRange = 0;
                ERankType rankNotInRange = ERankType.RankUnknown;

                for(int j=0; j<5; j++)
                {
                    ERankType currRank;
                    
                    if(rank - j < 0)
                    {
                        currRank = ERankType.RankAce;
                    }
                    else
                    {
                        currRank = rank - j;
                    }
                    
                    if(_rankCount[(int)currRank] > 0)
                    {
                        numInRange++;
                    }
                    else
                    {
                        rankNotInRange = currRank;
                    }
                }

                if(numInRange == 4)
                {
                    IsStraightDraw = true;
                    RanksToMakeStraight.Add(rankNotInRange);
                }
            }
        }
    }
}
