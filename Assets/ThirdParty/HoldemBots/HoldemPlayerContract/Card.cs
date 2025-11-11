using System;

namespace HoldemPlayerContract
{
    [Serializable]
    public class Card : IEquatable<Card>
    {
        public Card(ERankType rank, ESuitType suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public readonly ESuitType Suit;
        public readonly ERankType Rank;

        public string SuitStr() => SuitToString(Suit);

        public static string SuitToString(ESuitType suit)
        {
            return suit switch
            {
                ESuitType.SuitClubs => "C",
                ESuitType.SuitHearts => "H",
                ESuitType.SuitSpades => "S",
                ESuitType.SuitDiamonds => "D",
                _ => "?"
            };
        }

        public string RankStr() => RankToString(Rank);

        public static string RankToString(ERankType rank)
        {
            return rank switch
            {
                ERankType.RankAce => "A",
                ERankType.RankKing => "K",
                ERankType.RankQueen => "Q",
                ERankType.RankJack => "J",
                ERankType.RankTen => "T",
                ERankType.RankNine => "9",
                ERankType.RankEight => "8",
                ERankType.RankSeven => "7",
                ERankType.RankSix => "6",
                ERankType.RankFive => "5",
                ERankType.RankFour => "4",
                ERankType.RankThree => "3",
                ERankType.RankTwo => "2",
                _ => "?"
            };
        }

        public string ValueStr() => RankStr() + SuitStr();

        public override string ToString() => ValueStr();

        public override bool Equals(object obj) => Equals(obj as Card);

        public override int GetHashCode() => ((int)Suit * 13) + (int)Rank;

        public bool Equals(Card other)
        {
            if (other == null) return false;
            return Suit == other.Suit && Rank == other.Rank;
        }
    }
}

