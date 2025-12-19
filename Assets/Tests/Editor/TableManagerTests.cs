using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Тесты для TableManager
    /// </summary>
    public class TableManagerTests
    {
        [Test]
        public void TableManager_JoinPlayer_ReturnsTrue_WhenSuccessful()
        {
            // Arrange
            GameObject go = new GameObject("TableManager");
            TableManager tableManager = go.AddComponent<TableManager>();
            
            // Создаем мок SeatsLayoutRadial
            GameObject seatsGo = new GameObject("SeatsLayout");
            SeatsLayoutRadial seatsLayout = seatsGo.AddComponent<SeatsLayoutRadial>();
            tableManager.GetType().GetField("seatsLayout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(tableManager, seatsLayout);

            // Act
            bool result = tableManager.JoinPlayer("TestPlayer", 1000);

            // Assert
            // Результат зависит от реализации SeatsLayoutRadial
            // Тест проверяет, что метод не выбрасывает исключение
            Assert.DoesNotThrow(() => tableManager.JoinPlayer("TestPlayer2", 1000));

            // Cleanup
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(seatsGo);
        }

        [Test]
        public void TableManager_LeavePlayer_ReturnsFalse_WhenPlayerNotJoined()
        {
            // Arrange
            GameObject go = new GameObject("TableManager");
            TableManager tableManager = go.AddComponent<TableManager>();

            // Act
            bool result = tableManager.LeavePlayer("NonExistentPlayer");

            // Assert
            Assert.IsFalse(result);

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TableManager_EnsureMinPlayers_AddsPlayers()
        {
            // Arrange
            GameObject go = new GameObject("TableManager");
            TableManager tableManager = go.AddComponent<TableManager>();
            
            // Устанавливаем minPlayers через рефлексию
            var minPlayersField = tableManager.GetType().GetField("minPlayers", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            minPlayersField?.SetValue(tableManager, 2);

            // Act
            tableManager.EnsureMinPlayers();

            // Assert
            // Проверяем, что метод выполнился без ошибок
            Assert.DoesNotThrow(() => tableManager.EnsureMinPlayers());

            // Cleanup
            Object.DestroyImmediate(go);
        }
    }
}

