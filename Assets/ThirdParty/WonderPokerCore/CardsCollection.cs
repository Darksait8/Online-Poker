using System.Collections.Generic;
using System.Linq;

namespace WonderPokerCore
{
    /// <summary>
    /// Helper wrapper around list of cards with utility methods used by the original poker logic.
    /// </summary>
    public class CardsCollection
    {
        public List<Card> Cards { get; set; } = new();

        public CardsCollection()
        {
        }

        public CardsCollection(IEnumerable<Card> cards)
        {
            Cards = cards?.ToList() ?? new List<Card>();
        }

        public bool AddCard(Card card)
        {
            Cards ??= new List<Card>();
            Cards.Add(card);
            return true;
        }

        public Card TakeOutCard(CardSign sign, CardValue value)
        {
            if (Cards == null || Cards.Count == 0) return null;

            Card match = Cards.Find(c => c.Sign == sign && c.Value == value);
            if (match != null)
            {
                Cards.Remove(match);
            }
            return match;
        }

        public Card TakeOutCard(int index)
        {
            if (Cards == null || Cards.Count == 0 || index < 0 || index >= Cards.Count)
                return null;

            Card card = Cards[index];
            Cards.RemoveAt(index);
            return card;
        }

        public static CardsCollection operator +(CardsCollection first, CardsCollection second)
        {
            var result = new CardsCollection();
            if (first?.Cards != null) result.Cards.AddRange(first.Cards);
            if (second?.Cards != null) result.Cards.AddRange(second.Cards);
            return result;
        }

        public void SortDescending()
        {
            Cards?.Sort((x, y) => y.CompareTo(x));
        }

        public void SortAscending()
        {
            Cards?.Sort();
        }

        public override string ToString() => Cards == null ? string.Empty : string.Join(",", Cards);
    }
}

