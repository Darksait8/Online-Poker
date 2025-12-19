using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Дополнительные тесты для Deck для полного покрытия
    /// </summary>
    public class DeckExtendedTests
    {
        private Deck deck;

        [SetUp]
        public void SetUp()
        {
            deck = new Deck();
        }

        [Test]
        public void Deck_Reset_CreatesAll52Cards()
        {
            // Arrange & Act
            deck.Reset();
            var allCards = new HashSet<Card>();

            // Act
            for (int i = 0; i < 52; i++)
            {
                Card card = deck.DrawCard();
                allCards.Add(card);
            }

            // Assert
            Assert.AreEqual(52, allCards.Count, "Должно быть ровно 52 уникальные карты");
        }

        [Test]
        public void Deck_Shuffle_ProducesDifferentOrder_OnMultipleShuffles()
        {
            // Arrange
            deck.Reset();
            var firstShuffle = new List<Card>();
            for (int i = 0; i < 5; i++)
            {
                firstShuffle.Add(deck.DrawCard());
            }

            // Act
            deck.Reset();
            deck.Shuffle();
            var secondShuffle = new List<Card>();
            for (int i = 0; i < 5; i++)
            {
                secondShuffle.Add(deck.DrawCard());
            }

            deck.Reset();
            deck.Shuffle();
            var thirdShuffle = new List<Card>();
            for (int i = 0; i < 5; i++)
            {
                thirdShuffle.Add(deck.DrawCard());
            }

            // Assert
            bool allSame = true;
            for (int i = 0; i < 5; i++)
            {
                if (!firstShuffle[i].Equals(secondShuffle[i]) || 
                    !secondShuffle[i].Equals(thirdShuffle[i]))
                {
                    allSame = false;
                    break;
                }
            }
            Assert.IsFalse(allSame, "Перетасовки должны давать разные порядки");
        }

        [Test]
        public void Deck_DrawCard_ReturnsValidCard()
        {
            // Arrange
            deck.Reset();

            // Act
            Card card = deck.DrawCard();

            // Assert
            Assert.IsNotNull(card);
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), card.Suit));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), card.Rank));
        }

        [Test]
        public void Deck_CanDraw_ReturnsCorrectValues()
        {
            // Arrange
            deck.Reset();

            // Act & Assert
            Assert.IsTrue(deck.CanDraw(1));
            Assert.IsTrue(deck.CanDraw(52));
            Assert.IsFalse(deck.CanDraw(53));

            // Вытягиваем 10 карт
            for (int i = 0; i < 10; i++)
            {
                deck.DrawCard();
            }

            Assert.IsTrue(deck.CanDraw(42));
            Assert.IsFalse(deck.CanDraw(43));
        }

        [Test]
        public void Deck_Draw_ThrowsWhenEmpty()
        {
            // Arrange
            deck.Reset();
            for (int i = 0; i < 52; i++)
            {
                deck.Draw();
            }

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => deck.Draw());
        }

        [Test]
        public void Deck_MultipleResets_WorkCorrectly()
        {
            // Arrange & Act & Assert
            for (int reset = 0; reset < 5; reset++)
            {
                deck.Reset();
                int cardsDrawn = 0;
                for (int i = 0; i < 52; i++)
                {
                    Assert.DoesNotThrow(() => deck.DrawCard());
                    cardsDrawn++;
                }
                Assert.AreEqual(52, cardsDrawn, $"Reset #{reset + 1} должен позволить вытянуть 52 карты");
            }
        }

        [Test]
        public void Deck_AllCardsAreUnique_AfterReset()
        {
            // Arrange
            deck.Reset();
            var seenCards = new HashSet<Card>();

            // Act
            for (int i = 0; i < 52; i++)
            {
                Card card = deck.DrawCard();
                bool added = seenCards.Add(card);
                Assert.IsTrue(added, $"Карта {card} уже была вытянута ранее");
            }

            // Assert
            Assert.AreEqual(52, seenCards.Count);
        }
    }
}

