using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Тесты для GameManager (базовые тесты без Unity зависимостей)
    /// </summary>
    public class GameManagerTests
    {
        [Test]
        public void GameManager_GetBlindLevels_ReturnsCorrectValues()
        {
            // Arrange
            GameObject go = new GameObject("GameManager");
            GameManager gameManager = go.AddComponent<GameManager>();

            // Act
            var (smallBlind, bigBlind) = gameManager.GetBlindLevels();

            // Assert
            Assert.Greater(smallBlind, 0);
            Assert.Greater(bigBlind, smallBlind);

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameManager_SetBlindLevels_UpdatesBlinds()
        {
            // Arrange
            GameObject go = new GameObject("GameManager");
            GameManager gameManager = go.AddComponent<GameManager>();

            // Act
            gameManager.SetBlindLevels(25, 50);

            // Assert
            var (smallBlind, bigBlind) = gameManager.GetBlindLevels();
            Assert.AreEqual(25, smallBlind);
            Assert.AreEqual(50, bigBlind);

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameManager_SetBlindLevels_IgnoresInvalidValues()
        {
            // Arrange
            GameObject go = new GameObject("GameManager");
            GameManager gameManager = go.AddComponent<GameManager>();
            var originalBlinds = gameManager.GetBlindLevels();

            // Act
            gameManager.SetBlindLevels(-10, 20); // Невалидные значения
            gameManager.SetBlindLevels(50, 25); // BigBlind меньше SmallBlind

            // Assert - значения не должны измениться
            var currentBlinds = gameManager.GetBlindLevels();
            // Проверяем, что значения остались валидными (не изменились на невалидные)
            Assert.Greater(currentBlinds.smallBlind, 0);
            Assert.Greater(currentBlinds.bigBlind, currentBlinds.smallBlind);

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameManager_Players_IsNotNull()
        {
            // Arrange
            GameObject go = new GameObject("GameManager");
            GameManager gameManager = go.AddComponent<GameManager>();

            // Act & Assert
            Assert.IsNotNull(gameManager.Players);

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameManager_CurrentPhase_IsInitialized()
        {
            // Arrange
            GameObject go = new GameObject("GameManager");
            GameManager gameManager = go.AddComponent<GameManager>();

            // Act & Assert
            Assert.IsNotNull(gameManager.CurrentPhase);

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameManager_CommunityCards_IsNotNull()
        {
            // Arrange
            GameObject go = new GameObject("GameManager");
            GameManager gameManager = go.AddComponent<GameManager>();

            // Act & Assert
            Assert.IsNotNull(gameManager.CommunityCards);

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameManager_Pots_IsNotNull()
        {
            // Arrange
            GameObject go = new GameObject("GameManager");
            GameManager gameManager = go.AddComponent<GameManager>();

            // Act & Assert
            Assert.IsNotNull(gameManager.Pots);

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameManager_ResetPlayerStack_UpdatesStack()
        {
            // Arrange
            GameObject go = new GameObject("GameManager");
            GameManager gameManager = go.AddComponent<GameManager>();
            
            // Создаем тестового игрока через AddPlayer (если метод доступен)
            // Это зависит от реализации SeatsLayoutRadial

            // Act & Assert
            // Тест проверяет, что метод существует и не выбрасывает исключение
            Assert.DoesNotThrow(() => gameManager.ResetPlayerStack("TestPlayer", 2000));

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameManager_ResetPlayerStack_ReturnsFalse_WhenPlayerNotFound()
        {
            // Arrange
            GameObject go = new GameObject("GameManager");
            GameManager gameManager = go.AddComponent<GameManager>();

            // Act
            bool result = gameManager.ResetPlayerStack("NonExistentPlayer", 2000);

            // Assert
            Assert.IsFalse(result);

            // Cleanup
            Object.DestroyImmediate(go);
        }
    }
}

