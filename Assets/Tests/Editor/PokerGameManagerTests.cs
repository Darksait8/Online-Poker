using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Tests
{
    /// <summary>
    /// Тесты для PokerGameManager
    /// </summary>
    public class PokerGameManagerTests
    {
        private GameObject gameManagerObject;
        private PokerGameManager gameManager;

        [SetUp]
        public void SetUp()
        {
            gameManagerObject = new GameObject("PokerGameManager");
            gameManager = gameManagerObject.AddComponent<PokerGameManager>();
            // Инициализируем минимальное состояние для работы методов
            gameManager.players = new List<PokerGameManager.Player>();
            // Создаем двух игроков, чтобы предотвратить немедленный вызов EndBettingRound()
            // Создаем mock UI объекты, чтобы избежать NullReferenceException
            GameObject uiObject1 = new GameObject("MockUI1");
            var mockUI1 = uiObject1.AddComponent<NewBehaviourScript>();
            var testPlayer1 = new PokerGameManager.Player 
            { 
                Name = "TestPlayer1", 
                Stack = 1000, 
                IsBot = false,
                Folded = false,
                Bet = 0,
                UI = mockUI1
            };
            gameManager.players.Add(testPlayer1);
            
            GameObject uiObject2 = new GameObject("MockUI2");
            var mockUI2 = uiObject2.AddComponent<NewBehaviourScript>();
            var testPlayer2 = new PokerGameManager.Player 
            { 
                Name = "TestPlayer2", 
                Stack = 1000, 
                IsBot = true, // Второй игрок - бот, чтобы не требовал UI взаимодействия
                Folded = false,
                Bet = 0,
                UI = mockUI2
            };
            gameManager.players.Add(testPlayer2);
            
            // Инициализируем необходимые поля для работы методов через рефлексию
            // так как они могут быть private
            SetPrivateField(gameManager, "currentPlayer", 0);
            SetPrivateField(gameManager, "currentBet", 0);
            SetPrivateField(gameManager, "pot", 0);
            SetPrivateField(gameManager, "raises", 0);
            SetPrivateField(gameManager, "bettingRound", 0);
            SetPrivateField(gameManager, "dealer", 0);
            // Инициализируем hasActed - будет устанавливаться в каждом тесте отдельно
            SetPrivateField(gameManager, "hasActed", new List<bool> { false, false });
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Удаляем все созданные объекты
            // UI объекты будут удалены вместе с gameManagerObject, так как они созданы отдельно
            if (gameManager != null && gameManager.players != null)
            {
                foreach (var player in gameManager.players)
                {
                    if (player.UI != null && player.UI.gameObject != null)
                    {
                        Object.DestroyImmediate(player.UI.gameObject);
                    }
                }
            }
            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }
        }

        [Test]
        public void PokerGameManager_InitialStack_IsSet()
        {
            // Arrange
            gameManager.initialStack = 2000;

            // Act & Assert
            Assert.AreEqual(2000, gameManager.initialStack);
        }

        [Test]
        public void PokerGameManager_SmallBlind_IsSet()
        {
            // Arrange
            gameManager.smallBlind = 25;

            // Act & Assert
            Assert.AreEqual(25, gameManager.smallBlind);
        }

        [Test]
        public void PokerGameManager_BigBlind_IsSet()
        {
            // Arrange
            gameManager.bigBlind = 50;

            // Act & Assert
            Assert.AreEqual(50, gameManager.bigBlind);
        }

        [Test]
        public void PokerGameManager_MinPlayers_IsSet()
        {
            // Arrange
            gameManager.minPlayers = 3;

            // Act & Assert
            Assert.AreEqual(3, gameManager.minPlayers);
        }

        [Test]
        public void PokerGameManager_MaxPlayers_IsSet()
        {
            // Arrange
            gameManager.maxPlayers = 8;

            // Act & Assert
            Assert.AreEqual(8, gameManager.maxPlayers);
        }

        [Test]
        public void PokerGameManager_Players_IsInitialized()
        {
            // Arrange & Act
            var players = gameManager.players;

            // Assert
            Assert.IsNotNull(players);
        }

        [Test]
        public void PokerGameManager_PublicFold_DoesNotThrow()
        {
            // Arrange - устанавливаем hasActed в false для текущего игрока, чтобы метод мог выполниться
            SetPrivateField(gameManager, "hasActed", new List<bool> { false, false });
            
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.PublicFold());
        }

        [Test]
        public void PokerGameManager_PublicCall_DoesNotThrow()
        {
            // Arrange - устанавливаем hasActed в false для текущего игрока
            SetPrivateField(gameManager, "hasActed", new List<bool> { false, false });
            
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.PublicCall());
        }

        [Test]
        public void PokerGameManager_PublicCheck_DoesNotThrow()
        {
            // Arrange - устанавливаем hasActed в false для текущего игрока
            SetPrivateField(gameManager, "hasActed", new List<bool> { false, false });
            
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.PublicCheck());
        }

        [Test]
        public void PokerGameManager_PublicRaiseWithAmount_DoesNotThrow()
        {
            // Arrange - устанавливаем hasActed в false для текущего игрока
            SetPrivateField(gameManager, "hasActed", new List<bool> { false, false });
            
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.PublicRaiseWithAmount(100));
        }

        [Test]
        public void PokerGameManager_DoAction_HandlesFold()
        {
            // Arrange - устанавливаем hasActed в false для текущего игрока
            SetPrivateField(gameManager, "hasActed", new List<bool> { false, false });
            
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.DoAction("fold"));
        }

        [Test]
        public void PokerGameManager_DoAction_HandlesCall()
        {
            // Arrange - устанавливаем hasActed в false для текущего игрока
            SetPrivateField(gameManager, "hasActed", new List<bool> { false, false });
            
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.DoAction("call"));
        }

        [Test]
        public void PokerGameManager_DoAction_HandlesCheck()
        {
            // Arrange - устанавливаем hasActed в false для текущего игрока
            SetPrivateField(gameManager, "hasActed", new List<bool> { false, false });
            
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.DoAction("check"));
        }

        [Test]
        public void PokerGameManager_DoAction_HandlesRaise()
        {
            // Arrange - устанавливаем hasActed в false для текущего игрока
            SetPrivateField(gameManager, "hasActed", new List<bool> { false, false });
            
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.DoAction("raise", 100));
        }
    }
}

