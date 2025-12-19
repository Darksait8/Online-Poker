using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Тесты для PokerOddsCalculator
    /// </summary>
    public class PokerOddsCalculatorTests
    {
        [Test]
        public void CalculateOdds_ReturnsDictionary_WhenNoCards()
        {
            // Arrange & Act
            var odds = PokerOddsCalculator.CalculateOdds(null, null);

            // Assert
            Assert.IsNotNull(odds);
            Assert.Greater(odds.Count, 0, "Должны быть возвращены стандартные вероятности");
        }

        [Test]
        public void CalculateOdds_ReturnsDictionary_WhenHoleCardsProvided()
        {
            // Arrange
            Card[] holeCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.Ace)
            };

            // Act
            var odds = PokerOddsCalculator.CalculateOdds(holeCards, null);

            // Assert
            Assert.IsNotNull(odds);
            Assert.Greater(odds.Count, 0);
        }

        [Test]
        public void CalculateOdds_ReturnsDictionary_WhenBoardCardsProvided()
        {
            // Arrange
            Card[] holeCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.King)
            };
            Card[] boardCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Queen),
                new Card(Suit.Spades, Rank.Jack),
                new Card(Suit.Spades, Rank.Ten)
            };

            // Act
            var odds = PokerOddsCalculator.CalculateOdds(holeCards, boardCards);

            // Assert
            Assert.IsNotNull(odds);
            Assert.Greater(odds.Count, 0);
        }

        [Test]
        public void CalculateOdds_HandlesInvalidHoleCards()
        {
            // Arrange
            Card[] invalidHoleCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Ace)
            };

            // Act
            var odds = PokerOddsCalculator.CalculateOdds(invalidHoleCards, null);

            // Assert
            Assert.IsNotNull(odds);
            // Должны вернуться стандартные вероятности
        }

        [Test]
        public void CalculateOdds_HandlesFullBoard()
        {
            // Arrange
            Card[] holeCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.Ace)
            };
            Card[] boardCards = new Card[]
            {
                new Card(Suit.Spades, Rank.King),
                new Card(Suit.Spades, Rank.Queen),
                new Card(Suit.Spades, Rank.Jack),
                new Card(Suit.Spades, Rank.Ten),
                new Card(Suit.Diamonds, Rank.Nine)
            };

            // Act
            var odds = PokerOddsCalculator.CalculateOdds(holeCards, boardCards);

            // Assert
            Assert.IsNotNull(odds);
            // Когда все карты известны, одна комбинация должна иметь 100%
            bool has100Percent = false;
            foreach (var kvp in odds)
            {
                if (kvp.Value >= 99.9)
                {
                    has100Percent = true;
                    break;
                }
            }
            Assert.IsTrue(has100Percent, "Когда все карты известны, должна быть комбинация с 100% вероятностью");
        }

        [Test]
        public void CalculateOdds_ProbabilitiesSumToApproximately100()
        {
            // Arrange
            Card[] holeCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.King)
            };
            Card[] boardCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Queen),
                new Card(Suit.Spades, Rank.Jack),
                new Card(Suit.Spades, Rank.Ten)
            };

            // Act
            var odds = PokerOddsCalculator.CalculateOdds(holeCards, boardCards);
            double total = 0;
            foreach (var kvp in odds)
            {
                total += kvp.Value;
            }

            // Assert
            Assert.GreaterOrEqual(total, 90.0, "Сумма вероятностей должна быть около 100%");
            Assert.LessOrEqual(total, 110.0, "Сумма вероятностей не должна сильно превышать 100%");
        }

        [Test]
        public void CalculateOdds_HandlesPreFlop()
        {
            // Arrange
            Card[] holeCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.Ace)
            };
            Card[] boardCards = new Card[0];

            // Act
            var odds = PokerOddsCalculator.CalculateOdds(holeCards, boardCards);

            // Assert
            Assert.IsNotNull(odds);
            Assert.Greater(odds.Count, 0);
        }

        [Test]
        public void CalculateOdds_HandlesFlop()
        {
            // Arrange
            Card[] holeCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.King)
            };
            Card[] boardCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Queen),
                new Card(Suit.Spades, Rank.Jack),
                new Card(Suit.Spades, Rank.Ten)
            };

            // Act
            var odds = PokerOddsCalculator.CalculateOdds(holeCards, boardCards);

            // Assert
            Assert.IsNotNull(odds);
            Assert.Greater(odds.Count, 0);
        }

        [Test]
        public void CalculateOdds_HandlesTurn()
        {
            // Arrange
            Card[] holeCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.King)
            };
            Card[] boardCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Queen),
                new Card(Suit.Spades, Rank.Jack),
                new Card(Suit.Spades, Rank.Ten),
                new Card(Suit.Diamonds, Rank.Nine)
            };

            // Act
            var odds = PokerOddsCalculator.CalculateOdds(holeCards, boardCards);

            // Assert
            Assert.IsNotNull(odds);
            Assert.Greater(odds.Count, 0);
        }

        [Test]
        public void CalculateOdds_AllProbabilitiesAreNonNegative()
        {
            // Arrange
            Card[] holeCards = new Card[]
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.King)
            };

            // Act
            var odds = PokerOddsCalculator.CalculateOdds(holeCards, null);

            // Assert
            foreach (var kvp in odds)
            {
                Assert.GreaterOrEqual(kvp.Value, 0, $"{kvp.Key} должна иметь неотрицательную вероятность");
            }
        }
    }
}

