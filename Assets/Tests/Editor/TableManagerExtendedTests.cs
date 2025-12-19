using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Расширенные тесты для TableManager
    /// </summary>
    public class TableManagerExtendedTests
    {
        private GameObject tableManagerObject;
        private TableManager tableManager;

        [SetUp]
        public void SetUp()
        {
            tableManagerObject = new GameObject("TableManager");
            tableManager = tableManagerObject.AddComponent<TableManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (tableManagerObject != null)
            {
                Object.DestroyImmediate(tableManagerObject);
            }
        }

        [Test]
        public void TableManager_JoinPlayer_ReturnsFalse_WhenPlayerAlreadyJoined()
        {
            // Arrange
            string playerName = "TestPlayer";
            tableManager.JoinPlayer(playerName, 1000);

            // Act
            bool result = tableManager.JoinPlayer(playerName, 1000);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TableManager_LeavePlayer_ReturnsFalse_WhenPlayerNotJoined()
        {
            // Arrange
            string playerName = "NonExistentPlayer";

            // Act
            bool result = tableManager.LeavePlayer(playerName);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TableManager_EnsureMinPlayers_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => tableManager.EnsureMinPlayers());
        }

        [Test]
        public void TableManager_JoinPlayer_HandlesMultiplePlayers()
        {
            // Arrange & Act
            bool player1 = tableManager.JoinPlayer("Player1", 1000);
            bool player2 = tableManager.JoinPlayer("Player2", 1000);
            bool player3 = tableManager.JoinPlayer("Player3", 1000);

            // Assert
            // Проверяем, что методы не выбрасывают исключения
            Assert.DoesNotThrow(() => 
            {
                tableManager.JoinPlayer("Player4", 1000);
            });
        }

        [Test]
        public void TableManager_LeavePlayer_HandlesMultipleLeaves()
        {
            // Arrange
            tableManager.JoinPlayer("Player1", 1000);
            tableManager.JoinPlayer("Player2", 1000);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                tableManager.LeavePlayer("Player1");
                tableManager.LeavePlayer("Player2");
            });
        }
    }
}

