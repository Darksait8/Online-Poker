using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Тесты для функционала друзей в AuthManager
    /// </summary>
    public class AuthManagerFriendsTests
    {
        private string testUsername1 = "TestUser1_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        private string testUsername2 = "TestUser2_" + System.Guid.NewGuid().ToString().Substring(0, 8);

        [SetUp]
        public void SetUp()
        {
            AuthManager.ClearAllUserData();
        }

        [TearDown]
        public void TearDown()
        {
            AuthManager.ClearAllUserData();
        }

        [Test]
        public void AuthManager_TrySendFriendRequest_Success_WhenValid()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername1 };
            UserProfile profile2 = new UserProfile { username = testUsername2 };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);
            AuthManager.SetCurrentUser(profile1);

            // Act
            bool success = AuthManager.TrySendFriendRequest(testUsername2, out string error);

            // Assert
            Assert.IsTrue(success, error);
            Assert.IsEmpty(error);
            Assert.AreEqual(1, AuthManager.GetOutgoingFriendRequests().Count);
        }

        [Test]
        public void AuthManager_TrySendFriendRequest_Fails_WhenNotLoggedIn()
        {
            // Arrange & Act
            bool success = AuthManager.TrySendFriendRequest(testUsername2, out string error);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void AuthManager_TrySendFriendRequest_Fails_WhenSelf()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername1 };
            UserDataManager.SaveUserProfile(profile);
            AuthManager.SetCurrentUser(profile);

            // Act
            bool success = AuthManager.TrySendFriendRequest(testUsername1, out string error);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void AuthManager_TryAcceptFriendRequest_Success_WhenValid()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername1 };
            UserProfile profile2 = new UserProfile { username = testUsername2 };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);
            AuthManager.SetCurrentUser(profile1);
            AuthManager.TrySendFriendRequest(testUsername2, out _);
            // Перезагружаем профиль пользователя 2 из базы данных, чтобы получить входящую заявку
            UserProfile profile2Updated = UserDataManager.LoadUserProfile(testUsername2);
            AuthManager.SetCurrentUser(profile2Updated);

            // Act
            bool success = AuthManager.TryAcceptFriendRequest(testUsername1, out string error);

            // Assert
            Assert.IsTrue(success, error);
            Assert.IsEmpty(error);
            Assert.AreEqual(1, AuthManager.GetFriends().Count);
        }

        [Test]
        public void AuthManager_TryDeclineFriendRequest_Success_WhenValid()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername1 };
            UserProfile profile2 = new UserProfile { username = testUsername2 };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);
            AuthManager.SetCurrentUser(profile1);
            AuthManager.TrySendFriendRequest(testUsername2, out _);
            // Перезагружаем профиль пользователя 2 из базы данных
            UserProfile profile2Updated = UserDataManager.LoadUserProfile(testUsername2);
            AuthManager.SetCurrentUser(profile2Updated);

            // Act
            bool success = AuthManager.TryDeclineFriendRequest(testUsername1, out string error);

            // Assert
            Assert.IsTrue(success, error);
            Assert.IsEmpty(error);
            Assert.AreEqual(0, AuthManager.GetIncomingFriendRequests().Count);
        }

        [Test]
        public void AuthManager_TryCancelFriendRequest_Success_WhenValid()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername1 };
            UserProfile profile2 = new UserProfile { username = testUsername2 };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);
            AuthManager.SetCurrentUser(profile1);
            AuthManager.TrySendFriendRequest(testUsername2, out _);

            // Act
            bool success = AuthManager.TryCancelFriendRequest(testUsername2, out string error);

            // Assert
            Assert.IsTrue(success, error);
            Assert.IsEmpty(error);
            Assert.AreEqual(0, AuthManager.GetOutgoingFriendRequests().Count);
        }

        [Test]
        public void AuthManager_TryRemoveFriend_Success_WhenValid()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername1 };
            UserProfile profile2 = new UserProfile { username = testUsername2 };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);
            AuthManager.SetCurrentUser(profile1);
            AuthManager.TrySendFriendRequest(testUsername2, out _);
            // Перезагружаем профиль пользователя 2 из базы данных
            UserProfile profile2Updated = UserDataManager.LoadUserProfile(testUsername2);
            AuthManager.SetCurrentUser(profile2Updated);
            AuthManager.TryAcceptFriendRequest(testUsername1, out _);
            // Перезагружаем профиль пользователя 1 из базы данных после принятия заявки
            UserProfile profile1Updated = UserDataManager.LoadUserProfile(testUsername1);
            AuthManager.SetCurrentUser(profile1Updated);

            // Act
            bool success = AuthManager.TryRemoveFriend(testUsername2, out string error);

            // Assert
            Assert.IsTrue(success, error);
            Assert.IsEmpty(error);
            Assert.AreEqual(0, AuthManager.GetFriends().Count);
        }

        [Test]
        public void AuthManager_TrySendFriendRequest_Fails_WhenAlreadyFriends()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername1 };
            UserProfile profile2 = new UserProfile { username = testUsername2 };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);
            AuthManager.SetCurrentUser(profile1);
            AuthManager.TrySendFriendRequest(testUsername2, out _);
            AuthManager.SetCurrentUser(profile2);
            AuthManager.TryAcceptFriendRequest(testUsername1, out _);
            AuthManager.SetCurrentUser(profile1);

            // Act
            bool success = AuthManager.TrySendFriendRequest(testUsername2, out string error);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void AuthManager_TrySendFriendRequest_Fails_WhenRequestAlreadySent()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername1 };
            UserProfile profile2 = new UserProfile { username = testUsername2 };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);
            AuthManager.SetCurrentUser(profile1);
            AuthManager.TrySendFriendRequest(testUsername2, out _);

            // Act
            bool success = AuthManager.TrySendFriendRequest(testUsername2, out string error);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNotEmpty(error);
        }
    }
}

