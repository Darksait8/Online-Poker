using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Тесты для типов карт (Card, Suit, Rank)
    /// </summary>
    public class CardTypesTests
    {
        [Test]
        public void Card_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            Card card = new Card(Suit.Spades, Rank.Ace);

            // Assert
            Assert.AreEqual(Suit.Spades, card.Suit);
            Assert.AreEqual(Rank.Ace, card.Rank);
        }

        [Test]
        public void Card_Equals_ReturnsTrue_ForSameCard()
        {
            // Arrange
            Card card1 = new Card(Suit.Spades, Rank.Ace);
            Card card2 = new Card(Suit.Spades, Rank.Ace);

            // Act & Assert
            Assert.IsTrue(card1.Equals(card2));
            Assert.IsTrue(card1.Equals((object)card2));
            Assert.AreEqual(card1, card2);
        }

        [Test]
        public void Card_Equals_ReturnsFalse_ForDifferentCards()
        {
            // Arrange
            Card card1 = new Card(Suit.Spades, Rank.Ace);
            Card card2 = new Card(Suit.Hearts, Rank.Ace);
            Card card3 = new Card(Suit.Spades, Rank.King);

            // Act & Assert
            Assert.IsFalse(card1.Equals(card2));
            Assert.IsFalse(card1.Equals(card3));
        }

        [Test]
        public void Card_GetHashCode_ReturnsSame_ForSameCard()
        {
            // Arrange
            Card card1 = new Card(Suit.Spades, Rank.Ace);
            Card card2 = new Card(Suit.Spades, Rank.Ace);

            // Act & Assert
            Assert.AreEqual(card1.GetHashCode(), card2.GetHashCode());
        }

        [Test]
        public void Card_GetHashCode_ReturnsDifferent_ForDifferentCards()
        {
            // Arrange
            Card card1 = new Card(Suit.Spades, Rank.Ace);
            Card card2 = new Card(Suit.Hearts, Rank.Ace);
            Card card3 = new Card(Suit.Spades, Rank.King);

            // Act & Assert
            Assert.AreNotEqual(card1.GetHashCode(), card2.GetHashCode());
            Assert.AreNotEqual(card1.GetHashCode(), card3.GetHashCode());
        }

        [Test]
        public void Card_ToString_ReturnsFormattedString()
        {
            // Arrange
            Card card = new Card(Suit.Spades, Rank.Ace);

            // Act
            string result = card.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("Ace"));
            Assert.IsTrue(result.Contains("Spades"));
        }

        [Test]
        public void Card_CanBeUsedInHashSet()
        {
            // Arrange
            var cardSet = new System.Collections.Generic.HashSet<Card>();
            Card card1 = new Card(Suit.Spades, Rank.Ace);
            Card card2 = new Card(Suit.Spades, Rank.Ace);
            Card card3 = new Card(Suit.Hearts, Rank.King);

            // Act
            cardSet.Add(card1);
            cardSet.Add(card2); // Дубликат
            cardSet.Add(card3);

            // Assert
            Assert.AreEqual(2, cardSet.Count, "HashSet должен содержать только уникальные карты");
            Assert.IsTrue(cardSet.Contains(card1));
            Assert.IsTrue(cardSet.Contains(card2));
            Assert.IsTrue(cardSet.Contains(card3));
        }

        [Test]
        public void Suit_AllValues_AreValid()
        {
            // Arrange & Act & Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), Suit.Clubs));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), Suit.Diamonds));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), Suit.Hearts));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), Suit.Spades));
        }

        [Test]
        public void Rank_AllValues_AreValid()
        {
            // Arrange & Act & Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Two));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Three));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Four));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Five));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Six));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Seven));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Eight));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Nine));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Ten));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Jack));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Queen));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.King));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), Rank.Ace));
        }

        [Test]
        public void Card_All52Cards_AreUnique()
        {
            // Arrange
            var allCards = new System.Collections.Generic.HashSet<Card>();

            // Act
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                {
                    if (rank >= Rank.Two && rank <= Rank.Ace)
                    {
                        allCards.Add(new Card(suit, rank));
                    }
                }
            }

            // Assert
            Assert.AreEqual(52, allCards.Count, "Должно быть ровно 52 уникальные карты");
        }
    }
}

