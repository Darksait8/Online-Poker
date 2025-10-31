using UnityEngine;
using UnityEngine.UI;

public class GameSettingsController : MonoBehaviour
{
    [Header("Звуковые настройки")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle muteToggle;
    
    [Header("Графические настройки")]
    [SerializeField] private Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Dropdown refreshRateDropdown;
    
    [Header("Игровые настройки")]
    [SerializeField] private Toggle autoFoldToggle;
    [SerializeField] private Toggle autoCallToggle;
    [SerializeField] private Toggle showCardsToggle;
    [SerializeField] private Toggle showAnimationsToggle;
    [SerializeField] private Toggle showChatToggle;
    [SerializeField] private Toggle showPlayerStatsToggle;
    
    [Header("Настройки интерфейса")]
    [SerializeField] private Dropdown languageDropdown;
    [SerializeField] private Slider uiScaleSlider;
    [SerializeField] private Toggle showTooltipsToggle;
    [SerializeField] private Toggle compactModeToggle;
    
    [Header("Настройки уведомлений")]
    [SerializeField] private Toggle enableNotificationsToggle;
    [SerializeField] private Toggle soundNotificationsToggle;
    [SerializeField] private Toggle vibrationNotificationsToggle;
    
    [Header("Кнопки")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button applyButton;
    
    private GameSettings currentSettings;
    private bool isInitialized = false;
    
    private void Start()
    {
        LoadSettings();
        SetupUI();
        isInitialized = true;
    }
    
    private void OnDestroy()
    {
        if (isInitialized)
        {
            SaveSettings();
        }
    }
    
    /// <summary>
    /// Загружает настройки из профиля пользователя
    /// </summary>
    public void LoadSettings()
    {
        currentSettings = AuthManager.GetGameSettings();
        ApplySettingsToUI();
    }
    
    /// <summary>
    /// Сохраняет настройки в профиль пользователя
    /// </summary>
    public void SaveSettings()
    {
        if (!isInitialized) return;
        
        UpdateSettingsFromUI();
        AuthManager.SetGameSettings(currentSettings);
        ApplySettingsToGame();
        
        Debug.Log("✅ Game settings saved");
    }
    
    /// <summary>
    /// Сбрасывает настройки к значениям по умолчанию
    /// </summary>
    public void ResetSettings()
    {
        currentSettings = new GameSettings();
        ApplySettingsToUI();
        ApplySettingsToGame();
        
        Debug.Log("🔄 Settings reset to default");
    }
    
    /// <summary>
    /// Применяет настройки к игре
    /// </summary>
    private void ApplySettingsToGame()
    {
        // Применяем звуковые настройки
        AudioListener.volume = currentSettings.muteAll ? 0f : currentSettings.masterVolume;
        
        // Применяем графические настройки
        QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
        Screen.fullScreen = currentSettings.fullscreen;
        
        if (currentSettings.resolutionWidth > 0 && currentSettings.resolutionHeight > 0)
        {
            Screen.SetResolution(currentSettings.resolutionWidth, currentSettings.resolutionHeight, currentSettings.fullscreen, currentSettings.refreshRate);
        }
        
        // Применяем настройки локализации
        LocalizationManager.CurrentLanguage = currentSettings.language;
        
        Debug.Log("🎮 Settings applied to game");
    }
    
    /// <summary>
    /// Настраивает UI элементы
    /// </summary>
    private void SetupUI()
    {
        // Настраиваем слайдеры громкости
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
        
        if (muteToggle != null)
        {
            muteToggle.onValueChanged.AddListener(OnMuteChanged);
        }
        
        // Настраиваем графические настройки
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "Low", "Medium", "High", "Ultra" });
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }
        
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
        
        if (resolutionDropdown != null)
        {
            SetupResolutionDropdown();
        }
        
        if (refreshRateDropdown != null)
        {
            SetupRefreshRateDropdown();
        }
        
        // Настраиваем игровые настройки
        if (autoFoldToggle != null)
            autoFoldToggle.onValueChanged.AddListener(OnAutoFoldChanged);
        
        if (autoCallToggle != null)
            autoCallToggle.onValueChanged.AddListener(OnAutoCallChanged);
        
        if (showCardsToggle != null)
            showCardsToggle.onValueChanged.AddListener(OnShowCardsChanged);
        
        if (showAnimationsToggle != null)
            showAnimationsToggle.onValueChanged.AddListener(OnShowAnimationsChanged);
        
        if (showChatToggle != null)
            showChatToggle.onValueChanged.AddListener(OnShowChatChanged);
        
        if (showPlayerStatsToggle != null)
            showPlayerStatsToggle.onValueChanged.AddListener(OnShowPlayerStatsChanged);
        
        // Настраиваем настройки интерфейса
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "Русский", "English" });
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
        
        if (uiScaleSlider != null)
        {
            uiScaleSlider.minValue = 0.5f;
            uiScaleSlider.maxValue = 2f;
            uiScaleSlider.onValueChanged.AddListener(OnUiScaleChanged);
        }
        
        if (showTooltipsToggle != null)
            showTooltipsToggle.onValueChanged.AddListener(OnShowTooltipsChanged);
        
        if (compactModeToggle != null)
            compactModeToggle.onValueChanged.AddListener(OnCompactModeChanged);
        
        // Настраиваем настройки уведомлений
        if (enableNotificationsToggle != null)
            enableNotificationsToggle.onValueChanged.AddListener(OnEnableNotificationsChanged);
        
        if (soundNotificationsToggle != null)
            soundNotificationsToggle.onValueChanged.AddListener(OnSoundNotificationsChanged);
        
        if (vibrationNotificationsToggle != null)
            vibrationNotificationsToggle.onValueChanged.AddListener(OnVibrationNotificationsChanged);
        
        // Настраиваем кнопки
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveSettings);
        
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSettings);
        
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettingsToGame);
    }
    
    /// <summary>
    /// Настраивает dropdown разрешений
    /// </summary>
    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        
        resolutionDropdown.ClearOptions();
        
        // Получаем доступные разрешения
        Resolution[] resolutions = Screen.resolutions;
        System.Collections.Generic.List<string> resolutionOptions = new System.Collections.Generic.List<string>();
        
        foreach (Resolution res in resolutions)
        {
            string option = $"{res.width}x{res.height}";
            if (!resolutionOptions.Contains(option))
            {
                resolutionOptions.Add(option);
            }
        }
        
        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }
    
    /// <summary>
    /// Настраивает dropdown частоты обновления
    /// </summary>
    private void SetupRefreshRateDropdown()
    {
        if (refreshRateDropdown == null) return;
        
        refreshRateDropdown.ClearOptions();
        refreshRateDropdown.AddOptions(new System.Collections.Generic.List<string> { "60 Hz", "75 Hz", "120 Hz", "144 Hz" });
        refreshRateDropdown.onValueChanged.AddListener(OnRefreshRateChanged);
    }
    
    /// <summary>
    /// Применяет настройки к UI элементам
    /// </summary>
    private void ApplySettingsToUI()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = currentSettings.masterVolume;
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = currentSettings.musicVolume;
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = currentSettings.sfxVolume;
        
        if (muteToggle != null)
            muteToggle.isOn = currentSettings.muteAll;
        
        if (qualityDropdown != null)
            qualityDropdown.value = currentSettings.qualityLevel;
        
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = currentSettings.fullscreen;
        
        if (autoFoldToggle != null)
            autoFoldToggle.isOn = currentSettings.autoFold;
        
        if (autoCallToggle != null)
            autoCallToggle.isOn = currentSettings.autoCall;
        
        if (showCardsToggle != null)
            showCardsToggle.isOn = currentSettings.showCards;
        
        if (showAnimationsToggle != null)
            showAnimationsToggle.isOn = currentSettings.showAnimations;
        
        if (showChatToggle != null)
            showChatToggle.isOn = currentSettings.showChat;
        
        if (showPlayerStatsToggle != null)
            showPlayerStatsToggle.isOn = currentSettings.showPlayerStats;
        
        if (languageDropdown != null)
            languageDropdown.value = (int)currentSettings.language;
        
        if (uiScaleSlider != null)
            uiScaleSlider.value = currentSettings.uiScale;
        
        if (showTooltipsToggle != null)
            showTooltipsToggle.isOn = currentSettings.showTooltips;
        
        if (compactModeToggle != null)
            compactModeToggle.isOn = currentSettings.compactMode;
        
        if (enableNotificationsToggle != null)
            enableNotificationsToggle.isOn = currentSettings.enableNotifications;
        
        if (soundNotificationsToggle != null)
            soundNotificationsToggle.isOn = currentSettings.soundNotifications;
        
        if (vibrationNotificationsToggle != null)
            vibrationNotificationsToggle.isOn = currentSettings.vibrationNotifications;
    }
    
    /// <summary>
    /// Обновляет настройки из UI элементов
    /// </summary>
    private void UpdateSettingsFromUI()
    {
        if (masterVolumeSlider != null)
            currentSettings.masterVolume = masterVolumeSlider.value;
        
        if (musicVolumeSlider != null)
            currentSettings.musicVolume = musicVolumeSlider.value;
        
        if (sfxVolumeSlider != null)
            currentSettings.sfxVolume = sfxVolumeSlider.value;
        
        if (muteToggle != null)
            currentSettings.muteAll = muteToggle.isOn;
        
        if (qualityDropdown != null)
            currentSettings.qualityLevel = qualityDropdown.value;
        
        if (fullscreenToggle != null)
            currentSettings.fullscreen = fullscreenToggle.isOn;
        
        if (autoFoldToggle != null)
            currentSettings.autoFold = autoFoldToggle.isOn;
        
        if (autoCallToggle != null)
            currentSettings.autoCall = autoCallToggle.isOn;
        
        if (showCardsToggle != null)
            currentSettings.showCards = showCardsToggle.isOn;
        
        if (showAnimationsToggle != null)
            currentSettings.showAnimations = showAnimationsToggle.isOn;
        
        if (showChatToggle != null)
            currentSettings.showChat = showChatToggle.isOn;
        
        if (showPlayerStatsToggle != null)
            currentSettings.showPlayerStats = showPlayerStatsToggle.isOn;
        
        if (languageDropdown != null)
            currentSettings.language = (AppLanguage)languageDropdown.value;
        
        if (uiScaleSlider != null)
            currentSettings.uiScale = uiScaleSlider.value;
        
        if (showTooltipsToggle != null)
            currentSettings.showTooltips = showTooltipsToggle.isOn;
        
        if (compactModeToggle != null)
            currentSettings.compactMode = compactModeToggle.isOn;
        
        if (enableNotificationsToggle != null)
            currentSettings.enableNotifications = enableNotificationsToggle.isOn;
        
        if (soundNotificationsToggle != null)
            currentSettings.soundNotifications = soundNotificationsToggle.isOn;
        
        if (vibrationNotificationsToggle != null)
            currentSettings.vibrationNotifications = vibrationNotificationsToggle.isOn;
    }
    
    // Обработчики событий UI
    private void OnMasterVolumeChanged(float value) { if (isInitialized) currentSettings.masterVolume = value; }
    private void OnMusicVolumeChanged(float value) { if (isInitialized) currentSettings.musicVolume = value; }
    private void OnSfxVolumeChanged(float value) { if (isInitialized) currentSettings.sfxVolume = value; }
    private void OnMuteChanged(bool value) { if (isInitialized) currentSettings.muteAll = value; }
    private void OnQualityChanged(int value) { if (isInitialized) currentSettings.qualityLevel = value; }
    private void OnFullscreenChanged(bool value) { if (isInitialized) currentSettings.fullscreen = value; }
    private void OnResolutionChanged(int value) { /* Реализация изменения разрешения */ }
    private void OnRefreshRateChanged(int value) { /* Реализация изменения частоты обновления */ }
    private void OnAutoFoldChanged(bool value) { if (isInitialized) currentSettings.autoFold = value; }
    private void OnAutoCallChanged(bool value) { if (isInitialized) currentSettings.autoCall = value; }
    private void OnShowCardsChanged(bool value) { if (isInitialized) currentSettings.showCards = value; }
    private void OnShowAnimationsChanged(bool value) { if (isInitialized) currentSettings.showAnimations = value; }
    private void OnShowChatChanged(bool value) { if (isInitialized) currentSettings.showChat = value; }
    private void OnShowPlayerStatsChanged(bool value) { if (isInitialized) currentSettings.showPlayerStats = value; }
    private void OnLanguageChanged(int value) { if (isInitialized) currentSettings.language = (AppLanguage)value; }
    private void OnUiScaleChanged(float value) { if (isInitialized) currentSettings.uiScale = value; }
    private void OnShowTooltipsChanged(bool value) { if (isInitialized) currentSettings.showTooltips = value; }
    private void OnCompactModeChanged(bool value) { if (isInitialized) currentSettings.compactMode = value; }
    private void OnEnableNotificationsChanged(bool value) { if (isInitialized) currentSettings.enableNotifications = value; }
    private void OnSoundNotificationsChanged(bool value) { if (isInitialized) currentSettings.soundNotifications = value; }
    private void OnVibrationNotificationsChanged(bool value) { if (isInitialized) currentSettings.vibrationNotifications = value; }
}
