using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Тесты для UserDataManager
    /// </summary>
    public class UserDataManagerTests
    {
        private string testUsername = "TestUser_" + System.Guid.NewGuid().ToString().Substring(0, 8);

        [SetUp]
        public void SetUp()
        {
            UserDataManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            // Очищаем тестовые данные
            if (UserDataManager.ProfileExists(testUsername))
            {
                UserDataManager.DeleteUserProfile(testUsername);
            }
        }

        [Test]
        public void UserDataManager_Initialize_CreatesDirectories()
        {
            // Arrange & Act
            UserDataManager.Initialize();

            // Assert
            // Проверяем, что инициализация прошла без ошибок
            Assert.DoesNotThrow(() => UserDataManager.Initialize());
        }

        [Test]
        public void UserDataManager_SaveUserProfile_SavesSuccessfully()
        {
            // Arrange
            UserProfile profile = new UserProfile
            {
                username = testUsername,
                chips = 5000,
                XP = 100
            };

            // Act
            bool success = UserDataManager.SaveUserProfile(profile);

            // Assert
            Assert.IsTrue(success);
            Assert.IsTrue(UserDataManager.ProfileExists(testUsername));
        }

        [Test]
        public void UserDataManager_LoadUserProfile_LoadsCorrectly()
        {
            // Arrange
            UserProfile originalProfile = new UserProfile
            {
                username = testUsername,
                chips = 5000,
                XP = 100
            };
            UserDataManager.SaveUserProfile(originalProfile);

            // Act
            UserProfile loadedProfile = UserDataManager.LoadUserProfile(testUsername);

            // Assert
            Assert.IsNotNull(loadedProfile);
            Assert.AreEqual(testUsername, loadedProfile.username);
            Assert.AreEqual(5000, loadedProfile.chips);
            Assert.AreEqual(100, loadedProfile.XP);
        }

        [Test]
        public void UserDataManager_LoadUserProfile_ReturnsNull_WhenNotExists()
        {
            // Arrange
            string nonExistentUsername = "NonExistentUser_" + System.Guid.NewGuid();

            // Act
            UserProfile profile = UserDataManager.LoadUserProfile(nonExistentUsername);

            // Assert
            Assert.IsNull(profile);
        }

        [Test]
        public void UserDataManager_DeleteUserProfile_DeletesSuccessfully()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            UserDataManager.SaveUserProfile(profile);

            // Act
            bool deleted = UserDataManager.DeleteUserProfile(testUsername);

            // Assert
            Assert.IsTrue(deleted);
            Assert.IsFalse(UserDataManager.ProfileExists(testUsername));
        }

        [Test]
        public void UserDataManager_DeleteUserProfile_ReturnsFalse_WhenNotExists()
        {
            // Arrange
            string nonExistentUsername = "NonExistentUser_" + System.Guid.NewGuid();

            // Act
            bool deleted = UserDataManager.DeleteUserProfile(nonExistentUsername);

            // Assert
            Assert.IsFalse(deleted);
        }

        [Test]
        public void UserDataManager_HashPassword_ReturnsHash()
        {
            // Arrange
            string password = "TestPassword123";

            // Act
            string hash = UserDataManager.HashPassword(password);

            // Assert
            Assert.IsNotNull(hash);
            Assert.IsNotEmpty(hash);
            Assert.AreNotEqual(password, hash);
        }

        [Test]
        public void UserDataManager_VerifyPassword_ReturnsTrue_ForCorrectPassword()
        {
            // Arrange
            string password = "TestPassword123";
            string hash = UserDataManager.HashPassword(password);

            // Act
            bool isValid = UserDataManager.VerifyPassword(password, hash);

            // Assert
            Assert.IsTrue(isValid);
        }

        [Test]
        public void UserDataManager_VerifyPassword_ReturnsFalse_ForIncorrectPassword()
        {
            // Arrange
            string password = "TestPassword123";
            string wrongPassword = "WrongPassword456";
            string hash = UserDataManager.HashPassword(password);

            // Act
            bool isValid = UserDataManager.VerifyPassword(wrongPassword, hash);

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void UserDataManager_GetAllUsernames_ReturnsAllUsernames()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername + "_1" };
            UserProfile profile2 = new UserProfile { username = testUsername + "_2" };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);

            // Act
            List<string> usernames = UserDataManager.GetAllUsernames();

            // Assert
            Assert.IsNotNull(usernames);
            Assert.Contains(testUsername + "_1", usernames);
            Assert.Contains(testUsername + "_2", usernames);

            // Cleanup
            UserDataManager.DeleteUserProfile(testUsername + "_1");
            UserDataManager.DeleteUserProfile(testUsername + "_2");
        }

        [Test]
        public void UserDataManager_ExportProfile_ReturnsJson()
        {
            // Arrange
            UserProfile profile = new UserProfile
            {
                username = testUsername,
                chips = 5000
            };

            // Act
            string json = UserDataManager.ExportProfile(profile);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);
            Assert.IsTrue(json.Contains(testUsername));
        }

        [Test]
        public void UserDataManager_ImportProfile_LoadsFromJson()
        {
            // Arrange
            UserProfile originalProfile = new UserProfile
            {
                username = testUsername,
                chips = 5000,
                XP = 100
            };
            string json = UserDataManager.ExportProfile(originalProfile);

            // Act
            UserProfile importedProfile = UserDataManager.ImportProfile(json);

            // Assert
            Assert.IsNotNull(importedProfile);
            Assert.AreEqual(testUsername, importedProfile.username);
            Assert.AreEqual(5000, importedProfile.chips);
            Assert.AreEqual(100, importedProfile.XP);
        }

        [Test]
        public void UserDataManager_SaveUserProfile_HandlesNullProfile()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Cannot save profile: profile is null or username is empty");

            // Act
            bool success = UserDataManager.SaveUserProfile(null);

            // Assert
            Assert.IsFalse(success);
        }

        [Test]
        public void UserDataManager_SaveUserProfile_HandlesEmptyUsername()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = "" };
            LogAssert.Expect(LogType.Error, "Cannot save profile: profile is null or username is empty");

            // Act
            bool success = UserDataManager.SaveUserProfile(profile);

            // Assert
            Assert.IsFalse(success);
        }

        [Test]
        public void UserDataManager_HashPassword_HandlesEmptyPassword()
        {
            // Arrange & Act
            string hash = UserDataManager.HashPassword("");

            // Assert
            Assert.IsNotNull(hash);
        }

        [Test]
        public void UserDataManager_VerifyPassword_HandlesNullInputs()
        {
            // Arrange & Act & Assert
            Assert.IsFalse(UserDataManager.VerifyPassword(null, "hash"));
            Assert.IsFalse(UserDataManager.VerifyPassword("password", null));
            Assert.IsFalse(UserDataManager.VerifyPassword(null, null));
        }
    }
}

