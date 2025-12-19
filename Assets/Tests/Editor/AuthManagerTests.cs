using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    /// <summary>
    /// Тесты для AuthManager
    /// </summary>
    public class AuthManagerTests
    {
        private string testUsername = "TestUser_" + System.Guid.NewGuid().ToString().Substring(0, 8);

        [SetUp]
        public void SetUp()
        {
            // Очищаем текущего пользователя перед каждым тестом
            AuthManager.ClearAllUserData();
        }

        [TearDown]
        public void TearDown()
        {
            // Очищаем тестовые данные
            AuthManager.ClearAllUserData();
        }

        [Test]
        public void AuthManager_CurrentUser_IsNull_Initially()
        {
            // Arrange & Act
            var currentUser = AuthManager.CurrentUser;

            // Assert
            Assert.IsNull(currentUser);
        }

        [Test]
        public void AuthManager_IsLoggedIn_ReturnsFalse_Initially()
        {
            // Arrange & Act & Assert
            Assert.IsFalse(AuthManager.IsLoggedIn);
        }

        [Test]
        public void AuthManager_GetUserChips_ReturnsZero_WhenNotLoggedIn()
        {
            // Arrange & Act
            int chips = AuthManager.GetUserChips();

            // Assert
            Assert.AreEqual(0, chips);
        }

        [Test]
        public void AuthManager_SetUserChips_UpdatesChips()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.SetUserChips(5000);

            // Assert
            Assert.AreEqual(5000, AuthManager.GetUserChips());
        }

        [Test]
        public void AuthManager_UpdatePlayerBalance_UpdatesBalance()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.UpdatePlayerBalance(3000);

            // Assert
            Assert.AreEqual(3000, AuthManager.GetUserChips());
        }

        [Test]
        public void AuthManager_AddPlayerXp_IncreasesXP()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername, XP = 100 };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.AddPlayerXp(50);

            // Assert
            Assert.AreEqual(150, AuthManager.CurrentUser.XP);
        }

        [Test]
        public void AuthManager_AddPlayerXp_DoesNotAddNegativeXP()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername, XP = 100 };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.AddPlayerXp(-50);

            // Assert
            Assert.AreEqual(100, AuthManager.CurrentUser.XP, "XP не должна уменьшаться при отрицательном значении");
        }

        [Test]
        public void AuthManager_GetGameSettings_ReturnsDefault_WhenNotLoggedIn()
        {
            // Arrange & Act
            GameSettings settings = AuthManager.GetGameSettings();

            // Assert
            Assert.IsNotNull(settings);
        }

        [Test]
        public void AuthManager_SetGameSettings_UpdatesSettings()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);
            GameSettings newSettings = new GameSettings { masterVolume = 0.5f };

            // Act
            AuthManager.SetGameSettings(newSettings);

            // Assert
            Assert.AreEqual(0.5f, AuthManager.GetGameSettings().masterVolume);
        }

        [Test]
        public void AuthManager_UpdateNickname_UpdatesUsername()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.UpdateNickname("NewNickname");

            // Assert
            Assert.AreEqual("NewNickname", AuthManager.CurrentUser.username);
        }

        [Test]
        public void AuthManager_UpdateNickname_IgnoresEmptyString()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.UpdateNickname("");

            // Assert
            Assert.AreEqual(testUsername, AuthManager.CurrentUser.username);
        }

        [Test]
        public void AuthManager_UpdateAvatar_UpdatesAvatarId()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.UpdateAvatar("new_avatar");

            // Assert
            Assert.AreEqual("new_avatar", AuthManager.CurrentUser.avatarId);
        }

        [Test]
        public void AuthManager_UnlockAchievement_AddsAchievement()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.UnlockAchievement("first_win");

            // Assert
            Assert.IsTrue(AuthManager.CurrentUser.HasAchievement("first_win"));
        }

        [Test]
        public void AuthManager_UnlockAvatar_AddsAvatar()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.UnlockAvatar("premium_avatar");

            // Assert
            Assert.IsTrue(AuthManager.CurrentUser.HasAvatar("premium_avatar"));
        }

        [Test]
        public void AuthManager_UpdateGameStats_UpdatesStats()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.UpdateGameStats(true, 500, 0);

            // Assert
            Assert.AreEqual(1, AuthManager.CurrentUser.totalGamesPlayed);
            Assert.AreEqual(1, AuthManager.CurrentUser.gamesWon);
        }

        [Test]
        public void AuthManager_UpdateHandStats_UpdatesHandStats()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.UpdateHandStats(HandResult.Won, HandAction.Raise);

            // Assert
            Assert.AreEqual(1, AuthManager.CurrentUser.handsPlayed);
            Assert.AreEqual(1, AuthManager.CurrentUser.handsWon);
            Assert.AreEqual(1, AuthManager.CurrentUser.handsRaised);
        }

        [Test]
        public void AuthManager_GetFriends_ReturnsEmptyList_WhenNotLoggedIn()
        {
            // Arrange & Act
            var friends = AuthManager.GetFriends();

            // Assert
            Assert.IsNotNull(friends);
            Assert.AreEqual(0, friends.Count);
        }

        [Test]
        public void AuthManager_GetFriends_ReturnsFriendsList()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            profile.friends.Add("Friend1");
            profile.friends.Add("Friend2");
            AuthManager.SetCurrentUser(profile);

            // Act
            var friends = AuthManager.GetFriends();

            // Assert
            Assert.AreEqual(2, friends.Count);
            Assert.IsTrue(friends.Contains("Friend1"));
            Assert.IsTrue(friends.Contains("Friend2"));
        }

        [Test]
        public void AuthManager_GetIncomingFriendRequests_ReturnsEmptyList_WhenNotLoggedIn()
        {
            // Arrange & Act
            var requests = AuthManager.GetIncomingFriendRequests();

            // Assert
            Assert.IsNotNull(requests);
            Assert.AreEqual(0, requests.Count);
        }

        [Test]
        public void AuthManager_GetOutgoingFriendRequests_ReturnsEmptyList_WhenNotLoggedIn()
        {
            // Arrange & Act
            var requests = AuthManager.GetOutgoingFriendRequests();

            // Assert
            Assert.IsNotNull(requests);
            Assert.AreEqual(0, requests.Count);
        }

        [Test]
        public void AuthManager_SetCurrentUser_SetsUser()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };

            // Act
            AuthManager.SetCurrentUser(profile);

            // Assert
            Assert.IsNotNull(AuthManager.CurrentUser);
            Assert.AreEqual(testUsername, AuthManager.CurrentUser.username);
        }

        [Test]
        public void AuthManager_Logout_ClearsCurrentUser()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);

            // Act
            AuthManager.Logout();

            // Assert
            Assert.IsNull(AuthManager.CurrentUser);
            Assert.IsFalse(AuthManager.IsLoggedIn);
        }

        [Test]
        public void AuthManager_ApplyMatchResult_UpdatesProfile()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            AuthManager.SetCurrentUser(profile);
            var summary = new AuthManager.MatchResultSummary
            {
                isWinner = true,
                finalStack = 2000,
                stackDelta = 1000,
                xpEarned = 100,
                handsPlayed = 5
            };

            // Act
            AuthManager.ApplyMatchResult(summary);

            // Assert
            Assert.AreEqual(1, AuthManager.CurrentUser.totalGamesPlayed);
            Assert.AreEqual(1, AuthManager.CurrentUser.gamesWon);
            Assert.AreEqual(2000, AuthManager.CurrentUser.chips);
            Assert.AreEqual(100, AuthManager.CurrentUser.XP);
        }
    }
}

