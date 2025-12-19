using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Расширенные тесты для CardTypes
    /// </summary>
    public class CardTypesExtendedTests
    {
        [Test]
        public void Card_AllSuits_AreValid()
        {
            // Arrange & Act & Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), Suit.Clubs));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), Suit.Diamonds));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), Suit.Hearts));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), Suit.Spades));
            Assert.AreEqual(4, System.Enum.GetValues(typeof(Suit)).Length);
        }

        [Test]
        public void Card_AllRanks_AreValid()
        {
            // Arrange & Act & Assert
            var ranks = System.Enum.GetValues(typeof(Rank));
            Assert.AreEqual(13, ranks.Length);
            
            foreach (Rank rank in ranks)
            {
                Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), rank));
            }
        }

        [Test]
        public void Card_Equality_IsSymmetric()
        {
            // Arrange
            Card card1 = new Card(Suit.Spades, Rank.Ace);
            Card card2 = new Card(Suit.Spades, Rank.Ace);
            Card card3 = new Card(Suit.Hearts, Rank.Ace);

            // Act & Assert
            Assert.AreEqual(card1.Equals(card2), card2.Equals(card1));
            Assert.AreEqual(card1.Equals(card3), card3.Equals(card1)); // Оба должны быть false
        }

        [Test]
        public void Card_Equality_IsTransitive()
        {
            // Arrange
            Card card1 = new Card(Suit.Spades, Rank.Ace);
            Card card2 = new Card(Suit.Spades, Rank.Ace);
            Card card3 = new Card(Suit.Spades, Rank.Ace);

            // Act & Assert
            if (card1.Equals(card2) && card2.Equals(card3))
            {
                Assert.IsTrue(card1.Equals(card3));
            }
        }

        [Test]
        public void Card_GetHashCode_IsConsistent()
        {
            // Arrange
            Card card = new Card(Suit.Spades, Rank.Ace);

            // Act
            int hash1 = card.GetHashCode();
            int hash2 = card.GetHashCode();

            // Assert
            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void Card_ToString_ContainsRankAndSuit()
        {
            // Arrange
            Card card = new Card(Suit.Spades, Rank.Ace);

            // Act
            string result = card.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
            Assert.IsTrue(result.Contains("Ace") || result.Contains("Spades"));
        }

        [Test]
        public void Card_CanBeUsedInDictionary()
        {
            // Arrange
            var cardDict = new Dictionary<Card, int>();
            Card card1 = new Card(Suit.Spades, Rank.Ace);
            Card card2 = new Card(Suit.Spades, Rank.Ace);
            Card card3 = new Card(Suit.Hearts, Rank.King);

            // Act
            cardDict[card1] = 1;
            cardDict[card2] = 2; // Должно перезаписать
            cardDict[card3] = 3;

            // Assert
            Assert.AreEqual(2, cardDict.Count);
            Assert.AreEqual(2, cardDict[card1]);
            Assert.AreEqual(3, cardDict[card3]);
        }

        [Test]
        public void Card_All52Combinations_AreUnique()
        {
            // Arrange
            var allCards = new HashSet<Card>();

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
            Assert.AreEqual(52, allCards.Count);
        }

        [Test]
        public void Card_Equals_ReturnsFalse_ForNull()
        {
            // Arrange
            Card card = new Card(Suit.Spades, Rank.Ace);

            // Act & Assert
            Assert.IsFalse(card.Equals(null));
        }

        [Test]
        public void Card_Equals_ReturnsFalse_ForDifferentType()
        {
            // Arrange
            Card card = new Card(Suit.Spades, Rank.Ace);

            // Act & Assert
            Assert.IsFalse(card.Equals("not a card"));
        }
    }
}

