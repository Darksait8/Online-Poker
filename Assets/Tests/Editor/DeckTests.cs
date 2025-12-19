using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    /// <summary>
    /// Тесты для класса Deck (колода карт)
    /// </summary>
    public class DeckTests
    {
        private Deck deck;

        [SetUp]
        public void SetUp()
        {
            deck = new Deck();
        }

        [Test]
        public void Deck_Reset_Creates52Cards()
        {
            // Arrange & Act
            deck.Reset();

            // Assert
            // Проверяем, что можно вытянуть 52 карты
            for (int i = 0; i < 52; i++)
            {
                Assert.DoesNotThrow(() => deck.DrawCard(), $"Не удалось вытянуть карту {i + 1}");
            }
        }

        [Test]
        public void Deck_DrawCard_ThrowsWhenEmpty()
        {
            // Arrange
            deck.Reset();

            // Act - вытягиваем все карты
            for (int i = 0; i < 52; i++)
            {
                deck.DrawCard();
            }

            // Assert
            Assert.Throws<System.InvalidOperationException>(() => deck.DrawCard(), 
                "Должно быть исключение при попытке вытянуть карту из пустой колоды");
        }

        [Test]
        public void Deck_Shuffle_ChangesCardOrder()
        {
            // Arrange
            deck.Reset();
            var firstOrder = new Card[5];
            for (int i = 0; i < 5; i++)
            {
                firstOrder[i] = deck.DrawCard();
            }

            // Act
            deck.Reset();
            deck.Shuffle();
            var secondOrder = new Card[5];
            for (int i = 0; i < 5; i++)
            {
                secondOrder[i] = deck.DrawCard();
            }

            // Assert - порядок должен отличаться (с высокой вероятностью)
            bool ordersDifferent = false;
            for (int i = 0; i < 5; i++)
            {
                if (!firstOrder[i].Equals(secondOrder[i]))
                {
                    ordersDifferent = true;
                    break;
                }
            }
            Assert.IsTrue(ordersDifferent, "Перетасовка должна изменять порядок карт");
        }

        [Test]
        public void Deck_CanDraw_ReturnsTrueWhenCardsAvailable()
        {
            // Arrange
            deck.Reset();

            // Assert
            Assert.IsTrue(deck.CanDraw(1), "Должна быть возможность вытянуть карту");
            Assert.IsTrue(deck.CanDraw(52), "Должна быть возможность вытянуть все карты");
        }

        [Test]
        public void Deck_CanDraw_ReturnsFalseWhenNotEnoughCards()
        {
            // Arrange
            deck.Reset();
            for (int i = 0; i < 50; i++)
            {
                deck.DrawCard();
            }

            // Assert
            Assert.IsTrue(deck.CanDraw(2), "Должна быть возможность вытянуть 2 карты");
            Assert.IsFalse(deck.CanDraw(3), "Не должно быть возможности вытянуть 3 карты");
        }

        [Test]
        public void Deck_Draw_ReturnsValidCard()
        {
            // Arrange
            deck.Reset();

            // Act
            Card card = deck.Draw();

            // Assert
            Assert.IsNotNull(card, "Вытянутая карта не должна быть null");
            Assert.IsTrue(System.Enum.IsDefined(typeof(Suit), card.Suit), "Масть должна быть валидной");
            Assert.IsTrue(System.Enum.IsDefined(typeof(Rank), card.Rank), "Достоинство должно быть валидным");
        }

        [Test]
        public void Deck_Reset_AllowsMultipleResets()
        {
            // Arrange & Act & Assert
            for (int i = 0; i < 3; i++)
            {
                deck.Reset();
                Assert.DoesNotThrow(() =>
                {
                    for (int j = 0; j < 52; j++)
                    {
                        deck.DrawCard();
                    }
                }, $"Reset #{i + 1} должен работать корректно");
            }
        }

        [Test]
        public void Deck_DrawCard_NoDuplicateCards()
        {
            // Arrange
            deck.Reset();
            var drawnCards = new System.Collections.Generic.HashSet<Card>();

            // Act
            for (int i = 0; i < 52; i++)
            {
                Card card = deck.DrawCard();
                drawnCards.Add(card);
            }

            // Assert
            Assert.AreEqual(52, drawnCards.Count, "Все 52 карты должны быть уникальными");
        }
    }
}

