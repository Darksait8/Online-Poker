using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Тесты для GameSettings
    /// </summary>
    public class GameSettingsTests
    {
        [Test]
        public void GameSettings_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            GameSettings settings = new GameSettings();

            // Assert
            Assert.AreEqual(1f, settings.masterVolume);
            Assert.AreEqual(0.8f, settings.musicVolume);
            Assert.AreEqual(1f, settings.sfxVolume);
            Assert.IsFalse(settings.muteAll);
            Assert.AreEqual(2, settings.qualityLevel);
            Assert.IsTrue(settings.fullscreen);
            Assert.AreEqual(1920, settings.resolutionWidth);
            Assert.AreEqual(1080, settings.resolutionHeight);
            Assert.AreEqual(60, settings.refreshRate);
            Assert.IsFalse(settings.autoFold);
            Assert.IsFalse(settings.autoCall);
            Assert.IsTrue(settings.showCards);
            Assert.IsTrue(settings.showAnimations);
            Assert.AreEqual(AppLanguage.Russian, settings.language);
            Assert.AreEqual(1f, settings.uiScale);
            Assert.IsTrue(settings.showTooltips);
            Assert.IsFalse(settings.compactMode);
            Assert.AreEqual(1f, settings.brightness);
            Assert.AreEqual("default", settings.cardThemeId);
            Assert.IsTrue(settings.enableNotifications);
            Assert.IsTrue(settings.soundNotifications);
            Assert.IsFalse(settings.vibrationNotifications);
        }

        [Test]
        public void GameSettings_VolumeSettings_CanBeModified()
        {
            // Arrange
            GameSettings settings = new GameSettings();

            // Act
            settings.masterVolume = 0.5f;
            settings.musicVolume = 0.3f;
            settings.sfxVolume = 0.7f;

            // Assert
            Assert.AreEqual(0.5f, settings.masterVolume);
            Assert.AreEqual(0.3f, settings.musicVolume);
            Assert.AreEqual(0.7f, settings.sfxVolume);
        }

        [Test]
        public void GameSettings_GraphicsSettings_CanBeModified()
        {
            // Arrange
            GameSettings settings = new GameSettings();

            // Act
            settings.qualityLevel = 0;
            settings.fullscreen = false;
            settings.resolutionWidth = 1280;
            settings.resolutionHeight = 720;
            settings.refreshRate = 144;

            // Assert
            Assert.AreEqual(0, settings.qualityLevel);
            Assert.IsFalse(settings.fullscreen);
            Assert.AreEqual(1280, settings.resolutionWidth);
            Assert.AreEqual(720, settings.resolutionHeight);
            Assert.AreEqual(144, settings.refreshRate);
        }

        [Test]
        public void GameSettings_GameSettings_CanBeModified()
        {
            // Arrange
            GameSettings settings = new GameSettings();

            // Act
            settings.autoFold = true;
            settings.autoCall = true;
            settings.showCards = false;
            settings.showAnimations = false;

            // Assert
            Assert.IsTrue(settings.autoFold);
            Assert.IsTrue(settings.autoCall);
            Assert.IsFalse(settings.showCards);
            Assert.IsFalse(settings.showAnimations);
        }

        [Test]
        public void GameSettings_InterfaceSettings_CanBeModified()
        {
            // Arrange
            GameSettings settings = new GameSettings();

            // Act
            settings.language = AppLanguage.English;
            settings.uiScale = 1.5f;
            settings.showTooltips = false;
            settings.compactMode = true;
            settings.brightness = 0.8f;

            // Assert
            Assert.AreEqual(AppLanguage.English, settings.language);
            Assert.AreEqual(1.5f, settings.uiScale);
            Assert.IsFalse(settings.showTooltips);
            Assert.IsTrue(settings.compactMode);
            Assert.AreEqual(0.8f, settings.brightness);
        }

        [Test]
        public void GameSettings_CardTheme_CanBeModified()
        {
            // Arrange
            GameSettings settings = new GameSettings();

            // Act
            settings.cardThemeId = "premium_theme";

            // Assert
            Assert.AreEqual("premium_theme", settings.cardThemeId);
        }

        [Test]
        public void GameSettings_NotificationSettings_CanBeModified()
        {
            // Arrange
            GameSettings settings = new GameSettings();

            // Act
            settings.enableNotifications = false;
            settings.soundNotifications = false;
            settings.vibrationNotifications = true;

            // Assert
            Assert.IsFalse(settings.enableNotifications);
            Assert.IsFalse(settings.soundNotifications);
            Assert.IsTrue(settings.vibrationNotifications);
        }
    }
}

