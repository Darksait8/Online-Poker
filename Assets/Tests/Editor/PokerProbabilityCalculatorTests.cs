using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    /// <summary>
    /// Тесты для PokerProbabilityCalculator
    /// </summary>
    public class PokerProbabilityCalculatorTests
    {
        [Test]
        public void GetAllCombinations_ReturnsAll10Combinations()
        {
            // Arrange & Act
            var combinations = PokerProbabilityCalculator.GetAllCombinations();

            // Assert
            Assert.AreEqual(10, combinations.Count, "Должно быть 10 покерных комбинаций");
        }

        [Test]
        public void GetAllCombinations_ContainsAllExpectedCombinations()
        {
            // Arrange & Act
            var combinations = PokerProbabilityCalculator.GetAllCombinations();
            var combinationNames = combinations.Select(c => c.Name).ToList();

            // Assert
            Assert.Contains("Royal Flush", combinationNames);
            Assert.Contains("Straight Flush", combinationNames);
            Assert.Contains("Four of a Kind", combinationNames);
            Assert.Contains("Full House", combinationNames);
            Assert.Contains("Flush", combinationNames);
            Assert.Contains("Straight", combinationNames);
            Assert.Contains("Three of a Kind", combinationNames);
            Assert.Contains("Two Pair", combinationNames);
            Assert.Contains("One Pair", combinationNames);
            Assert.Contains("High Card", combinationNames);
        }

        [Test]
        public void GetAllCombinations_HasCorrectRanks()
        {
            // Arrange & Act
            var combinations = PokerProbabilityCalculator.GetAllCombinations();

            // Assert
            Assert.AreEqual(1, combinations.First(c => c.Name == "Royal Flush").Rank);
            Assert.AreEqual(2, combinations.First(c => c.Name == "Straight Flush").Rank);
            Assert.AreEqual(3, combinations.First(c => c.Name == "Four of a Kind").Rank);
            Assert.AreEqual(4, combinations.First(c => c.Name == "Full House").Rank);
            Assert.AreEqual(5, combinations.First(c => c.Name == "Flush").Rank);
            Assert.AreEqual(6, combinations.First(c => c.Name == "Straight").Rank);
            Assert.AreEqual(7, combinations.First(c => c.Name == "Three of a Kind").Rank);
            Assert.AreEqual(8, combinations.First(c => c.Name == "Two Pair").Rank);
            Assert.AreEqual(9, combinations.First(c => c.Name == "One Pair").Rank);
            Assert.AreEqual(10, combinations.First(c => c.Name == "High Card").Rank);
        }

        [Test]
        public void GetAllCombinations_ProbabilitiesSumToApproximately100()
        {
            // Arrange & Act
            var combinations = PokerProbabilityCalculator.GetAllCombinations();
            double totalProbability = combinations.Sum(c => c.Probability);

            // Assert
            Assert.GreaterOrEqual(totalProbability, 99.0, "Сумма вероятностей должна быть около 100%");
            Assert.LessOrEqual(totalProbability, 101.0, "Сумма вероятностей не должна превышать 101%");
        }

        [Test]
        public void GetAllCombinations_EachCombinationHasExampleCards()
        {
            // Arrange & Act
            var combinations = PokerProbabilityCalculator.GetAllCombinations();

            // Assert
            foreach (var combo in combinations)
            {
                Assert.IsNotNull(combo.ExampleCards, $"{combo.Name} должна иметь примеры карт");
                Assert.AreEqual(5, combo.ExampleCards.Length, $"{combo.Name} должна иметь 5 примеров карт");
            }
        }

        [Test]
        public void GetAllCombinations_ProbabilitiesArePositive()
        {
            // Arrange & Act
            var combinations = PokerProbabilityCalculator.GetAllCombinations();

            // Assert
            foreach (var combo in combinations)
            {
                Assert.GreaterOrEqual(combo.Probability, 0, $"{combo.Name} должна иметь неотрицательную вероятность");
            }
        }

        [Test]
        public void GetAllCombinations_HasRussianNames()
        {
            // Arrange & Act
            var combinations = PokerProbabilityCalculator.GetAllCombinations();

            // Assert
            foreach (var combo in combinations)
            {
                Assert.IsNotNull(combo.RussianName, $"{combo.Name} должна иметь русское название");
                Assert.IsNotEmpty(combo.RussianName, $"{combo.Name} должна иметь непустое русское название");
            }
        }

        [Test]
        public void GetAllCombinations_HasOdds()
        {
            // Arrange & Act
            var combinations = PokerProbabilityCalculator.GetAllCombinations();

            // Assert
            foreach (var combo in combinations)
            {
                Assert.IsNotNull(combo.Odds, $"{combo.Name} должна иметь шансы");
                Assert.IsNotEmpty(combo.Odds, $"{combo.Name} должна иметь непустые шансы");
            }
        }

        [Test]
        public void FormatProbability_FormatsSmallProbabilities()
        {
            // Arrange & Act
            string result1 = PokerProbabilityCalculator.FormatProbability(0.000154);
            string result2 = PokerProbabilityCalculator.FormatProbability(0.00139);

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.IsTrue(result1.Contains("%") || result1.Contains("<"), "Форматирование должно включать % или <");
        }

        [Test]
        public void FormatProbability_FormatsLargeProbabilities()
        {
            // Arrange & Act
            string result1 = PokerProbabilityCalculator.FormatProbability(42.2569);
            string result2 = PokerProbabilityCalculator.FormatProbability(50.1177);

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.IsTrue(result1.Contains("%"), "Форматирование должно включать %");
        }

        [Test]
        public void FormatProbability_HandlesZero()
        {
            // Arrange & Act
            string result = PokerProbabilityCalculator.FormatProbability(0);

            // Assert
            Assert.IsNotNull(result);
        }

        [Test]
        public void FormatProbability_Handles100()
        {
            // Arrange & Act
            string result = PokerProbabilityCalculator.FormatProbability(100);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("100%", result);
        }

        [Test]
        public void GetAllCombinations_IsSortedByRank()
        {
            // Arrange & Act
            var combinations = PokerProbabilityCalculator.GetAllCombinations();

            // Assert
            for (int i = 0; i < combinations.Count - 1; i++)
            {
                Assert.LessOrEqual(combinations[i].Rank, combinations[i + 1].Rank,
                    "Комбинации должны быть отсортированы по рангу");
            }
        }
    }
}

