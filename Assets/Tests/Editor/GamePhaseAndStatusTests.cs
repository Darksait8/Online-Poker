using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Тесты для GamePhase и PlayerStatus enums
    /// </summary>
    public class GamePhaseAndStatusTests
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
        public void GamePhase_CanBeConvertedToString()
        {
            // Arrange & Act
            string waiting = GamePhase.WaitingToStart.ToString();
            string preflop = GamePhase.PreFlop.ToString();
            string showdown = GamePhase.Showdown.ToString();

            // Assert
            Assert.IsNotNull(waiting);
            Assert.IsNotNull(preflop);
            Assert.IsNotNull(showdown);
            Assert.IsNotEmpty(waiting);
        }

        [Test]
        public void PlayerStatus_AllValues_AreDefined()
        {
            // Arrange & Act & Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerStatus), PlayerStatus.Active));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerStatus), PlayerStatus.Folded));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerStatus), PlayerStatus.AllIn));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerStatus), PlayerStatus.SittingOut));
        }

        [Test]
        public void PlayerStatus_CanBeConvertedToString()
        {
            // Arrange & Act
            string active = PlayerStatus.Active.ToString();
            string folded = PlayerStatus.Folded.ToString();
            string allIn = PlayerStatus.AllIn.ToString();

            // Assert
            Assert.IsNotNull(active);
            Assert.IsNotNull(folded);
            Assert.IsNotNull(allIn);
            Assert.IsNotEmpty(active);
        }

        [Test]
        public void GamePhase_CanBeParsed()
        {
            // Arrange & Act
            bool parsed1 = System.Enum.TryParse<GamePhase>("PreFlop", out GamePhase phase1);
            bool parsed2 = System.Enum.TryParse<GamePhase>("Showdown", out GamePhase phase2);

            // Assert
            Assert.IsTrue(parsed1);
            Assert.IsTrue(parsed2);
            Assert.AreEqual(GamePhase.PreFlop, phase1);
            Assert.AreEqual(GamePhase.Showdown, phase2);
        }

        [Test]
        public void PlayerStatus_CanBeParsed()
        {
            // Arrange & Act
            bool parsed1 = System.Enum.TryParse<PlayerStatus>("Active", out PlayerStatus status1);
            bool parsed2 = System.Enum.TryParse<PlayerStatus>("Folded", out PlayerStatus status2);

            // Assert
            Assert.IsTrue(parsed1);
            Assert.IsTrue(parsed2);
            Assert.AreEqual(PlayerStatus.Active, status1);
            Assert.AreEqual(PlayerStatus.Folded, status2);
        }
    }
}

