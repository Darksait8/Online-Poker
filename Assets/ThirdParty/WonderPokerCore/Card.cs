using System;

namespace WonderPokerCore
{
    public enum CardSign
    {
        Club,
        Diamond,
        Heart,
        Spade
    }

    public enum CardValue
    {
        Two = 2,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King,
        Ace
    }

    public enum CardColor
    {
        Black,
        Red
    }

    /// <summary>
    /// Immutable representation of a single playing card.
    /// </summary>
    public class Card : IComparable<Card>
    {
        public CardSign Sign { get; }
        public CardValue Value { get; }

        public Card(CardSign sign, CardValue value)
        {
            Sign = sign;
            Value = value;
        }

        public CardColor GetCardColor()
        {
            return Sign is CardSign.Diamond or CardSign.Heart
                ? CardColor.Red
                : CardColor.Black;
        }

        public string GetShortSign() =>
            Sign switch
            {
                CardSign.Spade => "♠",
                CardSign.Heart => "♥",
                CardSign.Diamond => "♦",
                CardSign.Club => "♣",
                _ => "?"
            };

        public string GetShortValue()
        {
            if (Value is >= CardValue.Two and <= CardValue.Ten)
                return ((int)Value).ToString();

            if (Value is >= CardValue.Jack and <= CardValue.Ace)
            {
                string text = Value.ToString();
                return text[0].ToString();
            }

            return "?";
        }

        public string GetShortName() => $"{GetShortValue()}{GetShortSign()}";

        public string GetName() => $"{Value} {Sign}";

        public override string ToString() => $"{(int)Value} {(int)Sign}";

        public int CompareTo(Card other)
        {
            if (other == null) return 1;

            int valueComparison = Value.CompareTo(other.Value);
            if (valueComparison != 0)
                return valueComparison;

            return Sign.CompareTo(other.Sign);
        }

        public static bool operator ==(Card left, Card right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(Card left, Card right) => !(left == right);

        public static bool operator >(Card left, Card right) =>
            left is not null && left.CompareTo(right) > 0;

        public static bool operator <(Card left, Card right) =>
            left is not null && left.CompareTo(right) < 0;

        public override bool Equals(object obj)
        {
            if (obj is not Card other) return false;
            return Sign == other.Sign && Value == other.Value;
        }

        public override int GetHashCode() => HashCode.Combine((int)Sign, (int)Value);
    }
}

