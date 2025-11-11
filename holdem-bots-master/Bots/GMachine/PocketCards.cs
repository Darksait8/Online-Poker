using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace GMachine
{
    internal class PocketCards
    {
        public List<Card> CardList;
        public Card HighCard {get; }
        public Card LowCard {get; }
        public bool IsPair {get; }
        public bool IsSuited {get; }
        public int GapBetweenHighAndLow {get; }

        public PocketCards(Card card1, Card card2)
        {
            if(card1.Rank > card2.Rank)
            {
                HighCard = card1;
                LowCard = card2;
            }
            else
            {
                LowCard = card1;
                HighCard = card2;
            }

            CardList = new List<Card>();
            CardList.Add(HighCard);
            CardList.Add(LowCard);

            IsPair = (HighCard.Rank == LowCard.Rank);
            IsSuited = (HighCard.Suit == LowCard.Suit);
            GapBetweenHighAndLow = HighCard.Rank - LowCard.Rank;
        }


        public string RankDescription()
        {
            string sRankDescription = HighCard.RankStr() + LowCard.RankStr();

            if(HighCard.Rank != LowCard.Rank)
            {
                if(HighCard.Suit == LowCard.Suit)
                {
                    sRankDescription += "s";
                }
                else
                {
                    sRankDescription += "o";
                }
            }

            return sRankDescription;
        }
    }
}
