using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Тесты для класса UserProfile
    /// </summary>
    public class UserProfileTests
    {
        [Test]
        public void UserProfile_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            UserProfile profile = new UserProfile();

            // Assert
            Assert.AreEqual("", profile.username);
            Assert.AreEqual("", profile.email);
            Assert.AreEqual(1000, profile.chips);
            Assert.AreEqual(0, profile.XP);
            Assert.AreEqual(0, profile.totalGamesPlayed);
            Assert.IsNotNull(profile.gameSettings);
            Assert.IsNotNull(profile.achievements);
            Assert.IsNotNull(profile.friends);
        }

        [Test]
        public void UserProfile_UpdateGameStats_TracksWins()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.UpdateGameStats(true, 500, 0);

            // Assert
            Assert.AreEqual(1, profile.totalGamesPlayed);
            Assert.AreEqual(1, profile.gamesWon);
            Assert.AreEqual(0, profile.gamesLost);
            Assert.AreEqual(1500, profile.chips);
            Assert.AreEqual(500, profile.totalWinnings);
            Assert.AreEqual(500, profile.biggestWin);
        }

        [Test]
        public void UserProfile_UpdateGameStats_TracksLosses()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.UpdateGameStats(false, 0, 300);

            // Assert
            Assert.AreEqual(1, profile.totalGamesPlayed);
            Assert.AreEqual(0, profile.gamesWon);
            Assert.AreEqual(1, profile.gamesLost);
            Assert.AreEqual(700, profile.chips);
            Assert.AreEqual(300, profile.totalLosses);
            Assert.AreEqual(300, profile.biggestLoss);
        }

        [Test]
        public void UserProfile_UpdateGameStats_CalculatesWinRate()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.UpdateGameStats(true, 500, 0);
            profile.UpdateGameStats(false, 0, 300);
            profile.UpdateGameStats(true, 200, 0);

            // Assert
            Assert.AreEqual(3, profile.totalGamesPlayed);
            Assert.AreEqual(2, profile.gamesWon);
            Assert.AreEqual(1, profile.gamesLost);
            Assert.Greater(profile.winRate, 0);
            Assert.LessOrEqual(profile.winRate, 100);
        }

        [Test]
        public void UserProfile_UpdateHandStats_TracksHandResults()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.UpdateHandStats(HandResult.Won, HandAction.Raise);
            profile.UpdateHandStats(HandResult.Lost, HandAction.Call);
            profile.UpdateHandStats(HandResult.Folded, HandAction.Fold);

            // Assert
            Assert.AreEqual(3, profile.handsPlayed);
            Assert.AreEqual(1, profile.handsWon);
            Assert.AreEqual(1, profile.handsLost);
            Assert.AreEqual(1, profile.handsFolded);
            Assert.AreEqual(1, profile.handsRaised);
            Assert.AreEqual(1, profile.handsCalled);
        }

        [Test]
        public void UserProfile_UnlockAchievement_AddsAchievement()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.UnlockAchievement("first_win");

            // Assert
            Assert.IsTrue(profile.HasAchievement("first_win"));
            Assert.AreEqual(1, profile.achievements.Count);
        }

        [Test]
        public void UserProfile_UnlockAchievement_DoesNotDuplicate()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.UnlockAchievement("first_win");
            profile.UnlockAchievement("first_win");

            // Assert
            Assert.AreEqual(1, profile.achievements.Count);
        }

        [Test]
        public void UserProfile_UnlockAvatar_AddsAvatar()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.UnlockAvatar("premium_avatar");

            // Assert
            Assert.IsTrue(profile.HasAvatar("premium_avatar"));
            Assert.IsTrue(profile.unlockedAvatars.Contains("premium_avatar"));
        }

        [Test]
        public void UserProfile_SetAvatar_UpdatesAvatarId()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.SetAvatar("new_avatar");

            // Assert
            Assert.AreEqual("new_avatar", profile.avatarId);
            Assert.IsTrue(profile.HasAvatar("new_avatar"));
        }

        [Test]
        public void UserProfile_AddDeposit_IncreasesBalance()
        {
            // Arrange
            UserProfile profile = new UserProfile { weeklyDepositLimit = 10000 };

            // Act
            bool success = profile.AddDeposit(1500);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(2500, profile.chips);
            Assert.AreEqual(1500, profile.currentWeekDeposits);
        }

        [Test]
        public void UserProfile_AddDeposit_RespectsWeeklyLimit()
        {
            // Arrange
            UserProfile profile = new UserProfile { weeklyDepositLimit = 1000 };

            // Act
            bool first = profile.AddDeposit(800);
            bool second = profile.AddDeposit(300);

            // Assert
            Assert.IsTrue(first);
            Assert.IsFalse(second, "Пополнение сверх лимита должно быть отклонено");
            Assert.AreEqual(1800, profile.chips);
        }

        [Test]
        public void UserProfile_GetRemainingWeeklyDeposit_CalculatesCorrectly()
        {
            // Arrange
            UserProfile profile = new UserProfile { weeklyDepositLimit = 5000 };

            // Act
            profile.AddDeposit(1750);
            int remaining = profile.GetRemainingWeeklyDeposit();

            // Assert
            Assert.AreEqual(3250, remaining);
        }

        [Test]
        public void UserProfile_CanDeposit_ReturnsTrue_WhenUnderLimit()
        {
            // Arrange
            UserProfile profile = new UserProfile { weeklyDepositLimit = 5000 };

            // Act
            bool canDeposit = profile.CanDeposit(2000);

            // Assert
            Assert.IsTrue(canDeposit);
        }

        [Test]
        public void UserProfile_CanDeposit_ReturnsFalse_WhenOverLimit()
        {
            // Arrange
            UserProfile profile = new UserProfile { weeklyDepositLimit = 1000 };
            profile.AddDeposit(800);

            // Act
            bool canDeposit = profile.CanDeposit(300);

            // Assert
            Assert.IsFalse(canDeposit);
        }

        [Test]
        public void UserProfile_StartNewSession_ResetsSessionData()
        {
            // Arrange
            UserProfile profile = new UserProfile();
            profile.currentSessionChips = 500;
            profile.currentSessionGames = 5;

            // Act
            profile.StartNewSession();

            // Assert
            Assert.AreEqual(0, profile.currentSessionChips);
            Assert.AreEqual(0, profile.currentSessionGames);
        }

        [Test]
        public void UserProfile_Level_CalculatesCorrectly()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.XP = 1000;

            // Assert
            Assert.Greater(profile.Level, 0);
        }

        [Test]
        public void UserProfile_XpToNextLevel_IsPositive()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act & Assert
            Assert.GreaterOrEqual(profile.XpToNextLevel, 0);
        }

        [Test]
        public void UserProfile_LevelProgress_IsBetween0And1()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act & Assert
            Assert.GreaterOrEqual(profile.LevelProgress, 0);
            Assert.LessOrEqual(profile.LevelProgress, 1);
        }

        [Test]
        public void UserProfile_SetCustomAvatar_SetsCustomAvatarPath()
        {
            // Arrange
            UserProfile profile = new UserProfile();

            // Act
            profile.SetCustomAvatar("/path/to/avatar.png");

            // Assert
            Assert.AreEqual(UserProfile.CustomAvatarId, profile.avatarId);
            Assert.AreEqual("/path/to/avatar.png", profile.customAvatarPath);
        }
    }
}

