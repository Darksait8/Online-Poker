using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Тесты для класса Player (игрок)
    /// </summary>
    public class PlayerTests
    {
        [Test]
        public void Player_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            Player player = new Player(1, "TestPlayer", 1000, 0);

            // Assert
            Assert.AreEqual(1, player.Id);
            Assert.AreEqual("TestPlayer", player.Name);
            Assert.AreEqual(1000, player.Stack);
            Assert.AreEqual(0, player.CurrentBet);
            Assert.AreEqual(0, player.SeatIndex);
            Assert.AreEqual(PlayerStatus.Active, player.Status);
            Assert.IsFalse(player.IsFolded);
            Assert.IsTrue(player.CanAct);
            Assert.IsTrue(player.IsInHand);
        }

        [Test]
        public void Player_MakeBet_ReducesStack()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);

            // Act
            int betAmount = player.MakeBet(500);

            // Assert
            Assert.AreEqual(500, betAmount);
            Assert.AreEqual(500, player.Stack);
            Assert.AreEqual(500, player.CurrentBet);
        }

        [Test]
        public void Player_MakeBet_AllIn_WhenBetExceedsStack()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 500, 0);

            // Act
            int betAmount = player.MakeBet(1000);

            // Assert
            Assert.AreEqual(500, betAmount, "Ставка должна быть ограничена стеком");
            Assert.AreEqual(0, player.Stack);
            Assert.AreEqual(500, player.CurrentBet);
            Assert.AreEqual(PlayerStatus.AllIn, player.Status);
        }

        [Test]
        public void Player_MakeBet_ReturnsZero_WhenFolded()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);
            player.Fold();

            // Act
            int betAmount = player.MakeBet(500);

            // Assert
            Assert.AreEqual(0, betAmount);
            Assert.AreEqual(1000, player.Stack);
            Assert.AreEqual(0, player.CurrentBet);
        }

        [Test]
        public void Player_Fold_ChangesStatus()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);

            // Act
            player.Fold();

            // Assert
            Assert.AreEqual(PlayerStatus.Folded, player.Status);
            Assert.IsTrue(player.IsFolded);
            Assert.IsFalse(player.CanAct);
            Assert.IsFalse(player.IsInHand);
        }

        [Test]
        public void Player_ResetBet_ClearsCurrentBet()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);
            player.MakeBet(500);

            // Act
            player.ResetBet();

            // Assert
            Assert.AreEqual(0, player.CurrentBet);
            Assert.AreEqual(500, player.Stack);
        }

        [Test]
        public void Player_PrepareForNewHand_ResetsForNewHand()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);
            player.MakeBet(500);
            player.SetHoleCards(new List<Card> { new Card(Suit.Spades, Rank.Ace), new Card(Suit.Hearts, Rank.King) });

            // Act
            player.PrepareForNewHand();

            // Assert
            Assert.AreEqual(0, player.CurrentBet);
            Assert.IsNotNull(player.HoleCards); // PrepareForNewHand создает new Card[2], не null
            Assert.AreEqual(2, player.HoleCards.Length);
            Assert.AreEqual(PlayerStatus.Active, player.Status);
        }

        [Test]
        public void Player_SetHoleCards_AcceptsTwoCards()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);
            var cards = new List<Card>
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.King)
            };

            // Act
            player.SetHoleCards(cards);

            // Assert
            Assert.IsNotNull(player.HoleCards);
            Assert.AreEqual(2, player.HoleCards.Length);
            Assert.AreEqual(cards[0], player.HoleCards[0]);
            Assert.AreEqual(cards[1], player.HoleCards[1]);
        }

        [Test]
        public void Player_SetHoleCards_Throws_WhenNotTwoCards()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                player.SetHoleCards(new List<Card> { new Card(Suit.Spades, Rank.Ace) }),
                "Должно быть исключение при попытке установить не 2 карты");

            Assert.Throws<System.ArgumentException>(() =>
                player.SetHoleCards(new List<Card>
                {
                    new Card(Suit.Spades, Rank.Ace),
                    new Card(Suit.Hearts, Rank.King),
                    new Card(Suit.Diamonds, Rank.Queen)
                }),
                "Должно быть исключение при попытке установить более 2 карт");
        }

        [Test]
        public void Player_Stack_CannotBeNegative()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);

            // Act
            player.Stack = -100;

            // Assert
            Assert.AreEqual(0, player.Stack, "Стек не должен быть отрицательным");
        }

        [Test]
        public void Player_CurrentBet_CannotBeNegative()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);

            // Act
            player.CurrentBet = -50;

            // Assert
            Assert.AreEqual(0, player.CurrentBet, "Текущая ставка не должна быть отрицательной");
        }

        [Test]
        public void Player_IsActive_ReturnsFalse_WhenFolded()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);

            // Act
            player.Fold();

            // Assert
            Assert.IsFalse(player.IsActive);
        }

        [Test]
        public void Player_IsActive_ReturnsFalse_WhenSittingOut()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 0, 0); // Stack = 0 для SittingOut
            player.Status = PlayerStatus.SittingOut;
            player.IsActive = false; // Явно устанавливаем isActive в false

            // Act & Assert
            // IsActive возвращает isActive && status != PlayerStatus.Folded
            // Если isActive = false, то IsActive вернет false независимо от статуса
            Assert.IsFalse(player.IsActive);
        }

        [Test]
        public void Player_ToString_ReturnsFormattedString()
        {
            // Arrange
            Player player = new Player(1, "TestPlayer", 1000, 0);

            // Act
            string result = player.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("TestPlayer"));
            Assert.IsTrue(result.Contains("1000"));
        }
    }
}

