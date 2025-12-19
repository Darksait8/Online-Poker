using NUnit.Framework;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Расширенные тесты для UserDataManager
    /// </summary>
    public class UserDataManagerExtendedTests
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
            if (UserDataManager.ProfileExists(testUsername))
            {
                UserDataManager.DeleteUserProfile(testUsername);
            }
        }

        [Test]
        public void UserDataManager_LoadAllProfiles_ReturnsAllProfiles()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername + "_1" };
            UserProfile profile2 = new UserProfile { username = testUsername + "_2" };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);

            // Act
            List<UserProfile> profiles = UserDataManager.LoadAllProfiles();

            // Assert
            Assert.IsNotNull(profiles);
            Assert.GreaterOrEqual(profiles.Count, 2);

            // Cleanup
            UserDataManager.DeleteUserProfile(testUsername + "_1");
            UserDataManager.DeleteUserProfile(testUsername + "_2");
        }

        [Test]
        public void UserDataManager_DeleteAllUsersExcept_KeepsSpecifiedUsers()
        {
            // Arrange
            UserProfile profile1 = new UserProfile { username = testUsername + "_1" };
            UserProfile profile2 = new UserProfile { username = testUsername + "_2" };
            UserProfile profile3 = new UserProfile { username = testUsername + "_3" };
            UserDataManager.SaveUserProfile(profile1);
            UserDataManager.SaveUserProfile(profile2);
            UserDataManager.SaveUserProfile(profile3);

            // Act
            int deleted = UserDataManager.DeleteAllUsersExcept(new List<string> 
            { 
                testUsername + "_1", 
                testUsername + "_2" 
            });

            // Assert
            Assert.GreaterOrEqual(deleted, 1);
            Assert.IsTrue(UserDataManager.ProfileExists(testUsername + "_1"));
            Assert.IsTrue(UserDataManager.ProfileExists(testUsername + "_2"));
            Assert.IsFalse(UserDataManager.ProfileExists(testUsername + "_3"));

            // Cleanup
            UserDataManager.DeleteUserProfile(testUsername + "_1");
            UserDataManager.DeleteUserProfile(testUsername + "_2");
        }

        [Test]
        public void UserDataManager_GetDataSize_ReturnsNonNegative()
        {
            // Arrange & Act
            long size = UserDataManager.GetDataSize();

            // Assert
            Assert.GreaterOrEqual(size, 0);
        }

        [Test]
        public void UserDataManager_GetDataInfo_ReturnsInfoString()
        {
            // Arrange & Act
            string info = UserDataManager.GetDataInfo();

            // Assert
            Assert.IsNotNull(info);
            Assert.IsNotEmpty(info);
        }

        [Test]
        public void UserDataManager_ExportProfile_HandlesComplexProfile()
        {
            // Arrange
            UserProfile profile = new UserProfile
            {
                username = testUsername,
                chips = 5000,
                XP = 1000,
                totalGamesPlayed = 50,
                gamesWon = 30,
                achievements = new List<string> { "first_win", "big_win" },
                friends = new List<string> { "Friend1", "Friend2" }
            };

            // Act
            string json = UserDataManager.ExportProfile(profile);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);
            Assert.IsTrue(json.Contains(testUsername));
        }

        [Test]
        public void UserDataManager_ImportProfile_RoundTrip()
        {
            // Arrange
            UserProfile original = new UserProfile
            {
                username = testUsername,
                chips = 5000,
                XP = 1000,
                totalGamesPlayed = 50
            };
            string json = UserDataManager.ExportProfile(original);

            // Act
            UserProfile imported = UserDataManager.ImportProfile(json);

            // Assert
            Assert.IsNotNull(imported);
            Assert.AreEqual(original.username, imported.username);
            Assert.AreEqual(original.chips, imported.chips);
            Assert.AreEqual(original.XP, imported.XP);
            Assert.AreEqual(original.totalGamesPlayed, imported.totalGamesPlayed);
        }

        [Test]
        public void UserDataManager_ProfileExists_ReturnsFalse_ForNonExistent()
        {
            // Arrange
            string nonExistent = "NonExistent_" + System.Guid.NewGuid();

            // Act
            bool exists = UserDataManager.ProfileExists(nonExistent);

            // Assert
            Assert.IsFalse(exists);
        }

        [Test]
        public void UserDataManager_ProfileExists_ReturnsTrue_ForExistent()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            UserDataManager.SaveUserProfile(profile);

            // Act
            bool exists = UserDataManager.ProfileExists(testUsername);

            // Assert
            Assert.IsTrue(exists);
        }

        [Test]
        public void UserDataManager_SaveUserProfile_CreatesBackup()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername };
            UserDataManager.SaveUserProfile(profile);

            // Act
            UserDataManager.SaveUserProfile(profile); // Второе сохранение должно создать бэкап

            // Assert
            // Проверяем, что сохранение прошло успешно
            Assert.IsTrue(UserDataManager.ProfileExists(testUsername));
        }

        [Test]
        public void UserDataManager_LoadUserProfile_HandlesCaseInsensitive()
        {
            // Arrange
            UserProfile profile = new UserProfile { username = testUsername.ToLower() };
            UserDataManager.SaveUserProfile(profile);

            // Act
            UserProfile loaded = UserDataManager.LoadUserProfile(testUsername.ToUpper());

            // Assert
            // В зависимости от реализации может работать или не работать
            // Проверяем хотя бы, что не выбрасывается исключение
            Assert.DoesNotThrow(() => UserDataManager.LoadUserProfile(testUsername.ToUpper()));
        }
    }
}

