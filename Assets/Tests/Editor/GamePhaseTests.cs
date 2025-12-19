using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Тесты для GamePhase enum
    /// </summary>
    public class GamePhaseTests
    {
        [Test]
        public void GamePhase_AllValues_AreDefined()
        {
            // Arrange & Act & Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.WaitingToStart));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.PreFlop));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.PreFlopBetting));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.Flop));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.FlopBetting));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.Turn));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.TurnBetting));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.River));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.RiverBetting));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.Showdown));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GamePhase), GamePhase.HandComplete));
        }

        [Test]
        public void GamePhase_CanBeCompared()
        {
            // Arrange
            GamePhase phase1 = GamePhase.PreFlop;
            GamePhase phase2 = GamePhase.PreFlop;
            GamePhase phase3 = GamePhase.Flop;

            // Act & Assert
            Assert.AreEqual(phase1, phase2);
            Assert.AreNotEqual(phase1, phase3);
        }
    }
}

