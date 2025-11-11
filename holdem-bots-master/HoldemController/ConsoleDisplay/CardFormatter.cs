using System;
using System.Linq;
using HoldemPlayerContract;

namespace HoldemController.ConsoleDisplay
{
    internal static class CardFormatter
    {
        private static readonly string[] _cardFormat =
        {
            "{0}  ",
            " {1} ",
            "  {0}"
        };

        public static string[] FormatCard(Card card, out ConsoleColor color)
        {
            if (card != null)
            {
                var rank = GetRank(card.Rank);
                var symbol = GetSymbol(card.Suit);
                color = GetSuitColor(card.Suit);
                return _cardFormat.Select(l => string.Format(l, rank, symbol)).ToArray();
            }
            color = ConsoleColor.White;
            return _cardFormat.Select(l => string.Format(l, " ", " ")).ToArray();
        }

        public static string[] NoCard()
        {
            ConsoleColor color;
            return FormatCard(null, out color);
        }

        public static ConsoleColor GetSuitColor(ESuitType suit)
        {
            return suit == ESuitType.SuitDiamonds || suit == ESuitType.SuitHearts ? ConsoleColor.Red : ConsoleColor.Black;
        }

        private static string GetSymbol(ESuitType suit)
        {
            switch (suit)
            {
                case ESuitType.SuitClubs:
                    return "\u2663";
                case ESuitType.SuitHearts:
                    return "\u2665";
                case ESuitType.SuitSpades:
                    return "\u2660";
                case ESuitType.SuitDiamonds:
                    return "\u2666";
                case ESuitType.SuitUnknown:
                    return " ";
                default:
                    throw new ArgumentOutOfRangeException(nameof(suit), suit, null);
            }
        }

        private static string GetRank(ERankType rank)
        {
            switch (rank)
            {
                case ERankType.RankTwo:
                    return "2";
                case ERankType.RankThree:
                    return "3";
                case ERankType.RankFour:
                    return "4";
                case ERankType.RankFive:
                    return "5";
                case ERankType.RankSix:
                    return "6";
                case ERankType.RankSeven:
                    return "7";
                case ERankType.RankEight:
                    return "8";
                case ERankType.RankNine:
                    return "9";
                case ERankType.RankTen:
                    return "T";
                case ERankType.RankJack:
                    return "J";
                case ERankType.RankQueen:
                    return "Q";
                case ERankType.RankKing:
                    return "K";
                case ERankType.RankAce:
                    return "A";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
            }
        }
    }
}