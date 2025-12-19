using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Тесты для PlayerStatus enum
    /// </summary>
    public class PlayerStatusTests
    {
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
        public void PlayerStatus_CanBeCompared()
        {
            // Arrange
            PlayerStatus status1 = PlayerStatus.Active;
            PlayerStatus status2 = PlayerStatus.Active;
            PlayerStatus status3 = PlayerStatus.Folded;

            // Act & Assert
            Assert.AreEqual(status1, status2);
            Assert.AreNotEqual(status1, status3);
        }
    }
}

