using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuUIController : MonoBehaviour
{
    [Header("Основные элементы")]
    [SerializeField] private MenuPlayLauncher playLauncher;
    [SerializeField] private Button settingsButton;

    [Header("Секции главного меню, которые нужно скрывать при открытии оверлеев")]
    [SerializeField] private GameObject[] mainMenuSections;

    [Header("Панель настроек")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Dropdown languageDropdown;
    [SerializeField] private Dropdown cardThemeDropdown;
    [SerializeField] private Image cardThemePreview;
    [SerializeField] private Button applySettingsButton;

    [Header("Информация о пользователе")]
    [SerializeField] private Image userAvatarImage;
    [SerializeField] private Text userNicknameText;
    [SerializeField] private TMP_Text userNicknameTextTMP;
    [SerializeField] private Button userSettingsButton;
    [SerializeField] private Button friendsButton;
    [SerializeField] private Button messagesButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Text userLevelText;
    [SerializeField] private TMP_Text userLevelTextTMP;
    
    [Header("Баланс (правый верхний угол)")]
    [SerializeField] private Text userBalanceText;
    [SerializeField] private TMP_Text userBalanceTextTMP;
    [SerializeField] private Button replenishButton;
    [SerializeField] private int replenishAmount = 1000; // Количество фишек для пополнения

    [Header("Реклама")]
    [SerializeField] private AdvertisementPanel advertisementPanel;
    
    [Header("Обновление баланса")]
    [SerializeField] private bool updateUsersBalanceOnStart = false; // Отключено - не сбрасываем баланс при запуске
    [SerializeField] private int newDefaultBalance = 1000;
    [SerializeField] private bool resetWeeklyLimitsOnStart = false;

    [Header("Информация о фишках")]
    [SerializeField] private Text chipLegendText;
    [SerializeField] private TMP_Text chipLegendTextTMP;
    [SerializeField] [TextArea(2, 3)] private string chipLegendFormat = "Номиналы фишек: 100 • 200 • 500 • 1000";

    [Header("Навигация")]
    [SerializeField] private Button backToAuthButton;
    [SerializeField] private bool handleEscapeForBack = true;

    [Header("Панель настроек пользователя")]
    [SerializeField] private GameObject userSettingsPanel;
    [SerializeField] private InputField nicknameInput;
    [SerializeField] private Button changeAvatarButton;
    [SerializeField] private Button resetAvatarButton;
    [SerializeField] private Image avatarPreviewImage;
    [SerializeField] private Button saveUserSettingsButton;
    [SerializeField] private Button cancelUserSettingsButton;

    [Header("Панель друзей")]
    [SerializeField] private GameObject friendsPanel;
    [SerializeField] private FriendListController friendsListController;

    [Header("Панель заявок в друзья")]
    [SerializeField] private GameObject friendRequestsPanel;
    [SerializeField] private FriendRequestCenterController friendRequestCenter;

    [Header("Панель лидеров")]
    [SerializeField] private LeaderboardPanel leaderboardPanel;
    
    [Header("Панель пополнения баланса")]
    [SerializeField] private ReplenishBalancePanel replenishBalancePanel;
    
    [Header("Панель правил игры")]
    [SerializeField] private RulesPanel rulesPanel;
    [SerializeField] private Button rulesButton; // Кнопка для открытия панели правил

    private GameSettings cachedSettings;
    private List<CardThemeInfo> themeOptions = new List<CardThemeInfo>();
    private string pendingAvatarId;
    private string pendingCustomAvatarSourcePath;
    private Sprite pendingAvatarPreviewSprite;
    private Texture2D pendingAvatarPreviewTexture;
    private bool previewIsTemporary;

    private void Awake()
    {
        // Обновляем баланс всех пользователей при первом запуске (только если UI элементы привязаны)
        if (updateUsersBalanceOnStart && HasRequiredUIElements())
        {
            UpdateAllUsersBalance();
        }
        
        // Сбрасываем недельные лимиты при запуске (если включено)
        if (resetWeeklyLimitsOnStart && HasRequiredUIElements())
        {
            ResetWeeklyLimitsForAllUsers();
        }
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettingsPanel);
        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettingsPanel);
        if (applySettingsButton != null)
            applySettingsButton.onClick.AddListener(ApplySettings);
        if (userSettingsButton != null)
            userSettingsButton.onClick.AddListener(OpenUserSettingsPanel);
        if (saveUserSettingsButton != null)
            saveUserSettingsButton.onClick.AddListener(SaveUserSettings);
        if (cancelUserSettingsButton != null)
            cancelUserSettingsButton.onClick.AddListener(CloseUserSettingsPanel);
        if (cardThemeDropdown != null)
            cardThemeDropdown.onValueChanged.AddListener(OnCardThemeChanged);
        if (changeAvatarButton != null)
            changeAvatarButton.onClick.AddListener(OnChangeAvatarClicked);
        if (resetAvatarButton != null)
            resetAvatarButton.onClick.AddListener(OnResetAvatarClicked);
        if (friendsButton != null) friendsButton.onClick.AddListener(OpenFriendsPanel);
        if (messagesButton != null) messagesButton.onClick.AddListener(OpenMessagesPanel);
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumePreviewChanged);
        if (backToAuthButton != null) backToAuthButton.onClick.AddListener(OnBackButtonPressed);
        if (leaderboardButton != null) leaderboardButton.onClick.AddListener(OpenLeaderboardPanel);
        if (rulesButton != null) rulesButton.onClick.AddListener(OpenRulesPanel);
        if (replenishButton != null) replenishButton.onClick.AddListener(OnReplenishButtonPressed);

        HideSettingsPanel();
        HideUserSettingsPanel();
        HideFriendsPanel();
        HideRequestsPanel();
        
        // Создаем UI баланса, если он не привязан
        EnsureBalanceUI();
        EnsureMenuButtons(); // Создаем кнопки меню если они не привязаны
        
        // Убеждаемся, что кнопка пополнения правильно подключена
        if (replenishButton != null)
        {
            replenishButton.onClick.RemoveListener(OnReplenishButtonPressed);
            replenishButton.onClick.AddListener(OnReplenishButtonPressed);
            Debug.Log("MainMenuUIController: Кнопка пополнения подключена в Awake");
        }
        else
        {
            Debug.LogWarning("MainMenuUIController: replenishButton не найдена в Awake!");
        }
        
        // Убеждаемся, что кнопки меню правильно подключены
        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveListener(OpenLeaderboardPanel);
            leaderboardButton.onClick.AddListener(OpenLeaderboardPanel);
            Debug.Log("MainMenuUIController: Кнопка таблицы лидеров подключена в Awake");
        }
        if (rulesButton != null)
        {
            rulesButton.onClick.RemoveListener(OpenRulesPanel);
            rulesButton.onClick.AddListener(OpenRulesPanel);
            Debug.Log("MainMenuUIController: Кнопка правил подключена в Awake");
        }
        
        RefreshMainMenuState();
        UpdateChipLegendDisplay();
        RefreshUserInfo(); // Убеждаемся, что информация о пользователе обновлена при загрузке
    }

    private void OnEnable()
    {
        AuthManager.OnUserProfileChanged += HandleProfileChanged;
        AuthManager.OnFriendsChanged += HandleFriendsChanged;
        AuthManager.OnFriendRequestsChanged += HandleFriendRequestsChanged;
        if (friendsListController != null)
            friendsListController.OnCloseRequested += CloseFriendsPanel;
        if (friendRequestCenter != null)
            friendRequestCenter.OnCloseRequested += CloseMessagesPanel;
        EnsureLeaderboardPanel();
        EnsureReplenishBalancePanel();
        EnsureRulesPanel();
        LoadSettingsFromProfile();
        RefreshUserInfo();
        SetupLanguageDropdown();
        SetupCardThemes();
        ApplyProfileAvatarToPreview(AuthManager.CurrentUser);
        UpdateChipLegendDisplay();
        friendsListController?.RefreshList();
        friendRequestCenter?.Refresh();
    }

    private void OnDisable()
    {
        AuthManager.OnUserProfileChanged -= HandleProfileChanged;
        AuthManager.OnFriendsChanged -= HandleFriendsChanged;
        AuthManager.OnFriendRequestsChanged -= HandleFriendRequestsChanged;
        if (friendsListController != null) friendsListController.OnCloseRequested -= CloseFriendsPanel;
        if (friendRequestCenter != null) friendRequestCenter.OnCloseRequested -= CloseMessagesPanel;
        if (backToAuthButton != null) backToAuthButton.onClick.RemoveListener(OnBackButtonPressed);
        if (leaderboardButton != null) leaderboardButton.onClick.RemoveListener(OpenLeaderboardPanel);
        if (rulesButton != null) rulesButton.onClick.RemoveListener(OpenRulesPanel);
        
        if (rulesPanel != null)
            rulesPanel.OnCloseRequested -= HandleRulesCloseRequested;
        if (replenishButton != null) replenishButton.onClick.RemoveListener(OnReplenishButtonPressed);
        ReleaseTemporaryPreview();
        if (leaderboardPanel != null)
            leaderboardPanel.OnCloseRequested -= HandleLeaderboardCloseRequested;
        if (replenishBalancePanel != null)
        {
            replenishBalancePanel.OnAmountSelected -= HandleReplenishAmountSelected;
            replenishBalancePanel.OnCloseRequested -= HandleReplenishCloseRequested;
        }
    }

    private void HandleProfileChanged(UserProfile profile)
    {
        RefreshUserInfo();
        LoadSettingsFromProfile();
        ApplyProfileAvatarToPreview(profile);
        UpdateCardThemePreview();
        UpdateChipLegendDisplay();
        if (leaderboardPanel != null && leaderboardPanel.gameObject.activeSelf)
        {
            BuildLeaderboardSections(out var topBalance, out var topLevel);
            leaderboardPanel.Show(topBalance, topLevel);
        }
    }

    private void HandleFriendsChanged(List<string> friends)
    {
        friendsListController?.RefreshList();
    }

    private void HandleFriendRequestsChanged()
    {
        friendRequestCenter?.Refresh();
    }

    private void Update()
    {
        if (!handleEscapeForBack)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (TryCloseOverlays())
                return;

            HandleBackToAuth();
        }
    }

    private void LoadSettingsFromProfile()
    {
        cachedSettings = AuthManager.GetGameSettings();
        if (cachedSettings == null)
        {
            cachedSettings = new GameSettings();
        }

        if (volumeSlider != null)
            volumeSlider.value = cachedSettings.masterVolume;

        if (brightnessSlider != null)
            brightnessSlider.value = cachedSettings.brightness;

        if (languageDropdown != null)
        {
            int value = (int)cachedSettings.language;
            if (value >= 0 && value < languageDropdown.options.Count)
                languageDropdown.value = value;
            DropdownStyler.Apply(languageDropdown);
            languageDropdown.RefreshShownValue();
        }

        if (cardThemeDropdown != null && themeOptions.Count > 0)
        {
            string id = cachedSettings.cardThemeId;
            int index = themeOptions.FindIndex(t => t.Id == id);
            if (index < 0) index = 0;
            cardThemeDropdown.SetValueWithoutNotify(index);
            DropdownStyler.Apply(cardThemeDropdown);
            cardThemeDropdown.RefreshShownValue();
            UpdateCardThemePreview();
        }
    }

    private void RefreshUserInfo()
    {
        UserProfile profile = AuthManager.CurrentUser;
        
        if (profile == null)
        {
            Debug.LogWarning("MainMenuUIController: Profile is null in RefreshUserInfo!");
            SetOptionalText(userNicknameText, userNicknameTextTMP, "Гость");
            UpdateUserStatsDisplay(null);
            return;
        }
        
        // Отображаем никнейм с уровнем
        string nickname = !string.IsNullOrEmpty(profile.username)
            ? profile.username
            : "Гость";
        
        int xp = profile.XP;
        int level = profile.Level;
        int chips = profile.chips;
        
        Debug.Log($"MainMenuUIController: Обновление информации о пользователе - Ник: {nickname}, XP: {xp}, Уровень: {level}, Баланс: {chips}");
        
        string nicknameWithLevel = $"{nickname} (Уровень {level})";
        
        SetOptionalText(userNicknameText, userNicknameTextTMP, nicknameWithLevel);
        
        // Проверяем, что текст обновился и текст не обрезается
        if (userNicknameText != null)
        {
            Debug.Log($"MainMenuUIController: userNicknameText.text = '{userNicknameText.text}'");
            // Убеждаемся, что текст не обрезается
            userNicknameText.resizeTextForBestFit = false;
            userNicknameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            userNicknameText.verticalOverflow = VerticalWrapMode.Overflow;
            // Принудительно обновляем отображение
            Canvas.ForceUpdateCanvases();
        }
        if (userNicknameTextTMP != null)
        {
            Debug.Log($"MainMenuUIController: userNicknameTextTMP.text = '{userNicknameTextTMP.text}'");
            // Для TextMeshPro
            userNicknameTextTMP.enableWordWrapping = false;
            userNicknameTextTMP.overflowMode = TextOverflowModes.Overflow;
            // Принудительно обновляем отображение
            Canvas.ForceUpdateCanvases();
        }

        if (userAvatarImage != null)
        {
            Sprite sprite = CustomAvatarManager.GetAvatarSprite(profile);
            if (sprite == null)
                sprite = AuthManager.GetCurrentAvatarSprite();
            if (sprite == null)
                sprite = AvatarLibrary.GetAvatarSprite("default");
            userAvatarImage.sprite = sprite;
            userAvatarImage.color = Color.white;
            userAvatarImage.preserveAspect = true;
            if (!userAvatarImage.gameObject.activeSelf)
                userAvatarImage.gameObject.SetActive(true);
            userAvatarImage.enabled = sprite != null;
        }
        UpdateUserStatsDisplay(profile);
    }

    private void SetupLanguageDropdown()
    {
        if (languageDropdown == null)
            return;

        if (languageDropdown.options.Count == 0)
        {
            languageDropdown.options.Add(new Dropdown.OptionData("Русский"));
            languageDropdown.options.Add(new Dropdown.OptionData("English"));
        }

        DropdownStyler.Apply(languageDropdown);
        languageDropdown.RefreshShownValue();
    }

    private void SetupCardThemes()
    {
        if (cardThemeDropdown == null)
            return;

        themeOptions = new List<CardThemeInfo>(CardThemeService.Themes);
        cardThemeDropdown.options.Clear();

        if (themeOptions.Count == 0)
        {
            cardThemeDropdown.options.Add(new Dropdown.OptionData("Нет тем"));
            cardThemeDropdown.interactable = false;
            return;
        }

        foreach (CardThemeInfo theme in themeOptions)
        {
            cardThemeDropdown.options.Add(new Dropdown.OptionData(theme.DisplayName));
        }
        cardThemeDropdown.interactable = true;
        DropdownStyler.Apply(cardThemeDropdown);
        LoadSettingsFromProfile();
    }


    private void OpenSettingsPanel()
    {
        LoadSettingsFromProfile();
        ShowOverlay(settingsPanel);
    }

    private void CloseSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        RefreshMainMenuState();
    }

    private void HideSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OpenUserSettingsPanel()
    {
        if (userSettingsPanel != null)
        {
        var profile = AuthManager.CurrentUser;
        ApplyProfileAvatarToPreview(profile);
        if (userAvatarImage != null)
            userAvatarImage.sprite = CustomAvatarManager.GetAvatarSprite(profile);
            if (nicknameInput != null)
            {
                nicknameInput.text = profile != null ? profile.username : string.Empty;
            }
            ShowOverlay(userSettingsPanel);
        }
    }

    private void CloseUserSettingsPanel()
    {
        if (userSettingsPanel != null)
            userSettingsPanel.SetActive(false);
        var current = AuthManager.CurrentUser;
        ApplyProfileAvatarToPreview(current);
        if (userAvatarImage != null)
            userAvatarImage.sprite = CustomAvatarManager.GetAvatarSprite(current);
        RefreshMainMenuState();
    }

    private void HideUserSettingsPanel()
    {
        if (userSettingsPanel != null)
            userSettingsPanel.SetActive(false);
        var current = AuthManager.CurrentUser;
        ApplyProfileAvatarToPreview(current);
        if (userAvatarImage != null)
            userAvatarImage.sprite = CustomAvatarManager.GetAvatarSprite(current);
    }

    private void OpenFriendsPanel()
    {
        friendsListController?.RefreshList();
        ShowOverlay(friendsPanel);
    }

    public void CloseFriendsPanel()
    {
        if (friendsPanel != null)
            friendsPanel.SetActive(false);
        RefreshMainMenuState();
    }

    private void HideFriendsPanel()
    {
        if (friendsPanel != null)
            friendsPanel.SetActive(false);
    }

    private void OpenMessagesPanel()
    {
        friendRequestCenter?.Refresh();
        ShowOverlay(friendRequestsPanel);
    }

    public void CloseMessagesPanel()
    {
        if (friendRequestsPanel != null)
            friendRequestsPanel.SetActive(false);
        RefreshMainMenuState();
    }

    private void HideRequestsPanel()
    {
        if (friendRequestsPanel != null)
            friendRequestsPanel.SetActive(false);
    }

    private void SaveUserSettings()
    {
        UserProfile profile = AuthManager.CurrentUser;
        string newNickname = null;
        if (nicknameInput != null)
        {
            newNickname = nicknameInput.text;
        }

        bool shouldApplyCustomAvatar = pendingAvatarId == UserProfile.CustomAvatarId &&
                                       !string.IsNullOrEmpty(pendingCustomAvatarSourcePath);
        bool shouldApplyPresetAvatar = !string.IsNullOrEmpty(pendingAvatarId) &&
                                       pendingAvatarId != UserProfile.CustomAvatarId;

        if (shouldApplyCustomAvatar)
        {
            AuthManager.UpdateCustomAvatar(pendingCustomAvatarSourcePath);
            pendingCustomAvatarSourcePath = null;
        }
        else if (shouldApplyPresetAvatar)
        {
            if (profile == null || profile.avatarId != pendingAvatarId)
            {
                AuthManager.UpdateAvatar(pendingAvatarId);
            }
        }

        if (!string.IsNullOrWhiteSpace(newNickname))
            AuthManager.UpdateNickname(newNickname);

        pendingAvatarId = profile != null ? profile.avatarId : "default";

        CloseUserSettingsPanel();
    }

    private void ApplySettings()
    {
        if (cachedSettings == null)
            cachedSettings = new GameSettings();

        if (volumeSlider != null)
        {
            cachedSettings.masterVolume = volumeSlider.value;
            AudioListener.volume = cachedSettings.masterVolume;
        }

        if (brightnessSlider != null)
            cachedSettings.brightness = brightnessSlider.value;

        if (languageDropdown != null)
        {
            cachedSettings.language = (AppLanguage)Mathf.Clamp(languageDropdown.value, 0, 1);
            LocalizationManager.CurrentLanguage = cachedSettings.language;
        }

        if (cardThemeDropdown != null && themeOptions.Count > 0)
        {
            int index = Mathf.Clamp(cardThemeDropdown.value, 0, themeOptions.Count - 1);
            cachedSettings.cardThemeId = themeOptions[index].Id;
        }

        DropdownStyler.Apply(cardThemeDropdown);
        DropdownStyler.Apply(languageDropdown);

        if (AuthManager.IsLoggedIn)
        {
            AuthManager.SetGameSettings(cachedSettings);
        }
        else
        {
            CardThemeService.ApplyTheme(cachedSettings.cardThemeId);
            PlayerPrefs.SetFloat("masterVolume", cachedSettings.masterVolume);
            PlayerPrefs.Save();
        }

        CloseSettingsPanel();
    }

    private void OnCardThemeChanged(int index)
    {
        UpdateCardThemePreview();
    }

    private void UpdateCardThemePreview()
    {
        if (cardThemePreview == null || themeOptions.Count == 0)
            return;

        int index = Mathf.Clamp(cardThemeDropdown.value, 0, themeOptions.Count - 1);
        cardThemePreview.sprite = themeOptions[index].Preview;
        DropdownStyler.Apply(cardThemeDropdown);
    }

    private void OnVolumePreviewChanged(float value)
    {
        AudioListener.volume = value;
    }

    private void OnChangeAvatarClicked()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Выбор аватара", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            ApplyCustomAvatarSelection(path);
        }
#else
        Debug.LogWarning("Выбор аватара доступен только в редакторе. Для билдов нужна нативная реализация.");
#endif
    }

    private void OnResetAvatarClicked()
    {
        pendingAvatarId = "default";
        pendingCustomAvatarSourcePath = null;
        Sprite sprite = AvatarLibrary.GetAvatarSprite("default");
        SetPreviewSprite(sprite, false);
    }

    private void ApplyCustomAvatarSelection(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        if (!CustomAvatarManager.TryCreatePreview(filePath, out Sprite sprite, out Texture2D texture))
        {
            Debug.LogWarning("Не удалось загрузить выбранное изображение.");
            return;
        }

        pendingAvatarId = UserProfile.CustomAvatarId;
        pendingCustomAvatarSourcePath = filePath;
        SetPreviewSprite(sprite, true, texture);
    }

    private void ApplyProfileAvatarToPreview(UserProfile profile)
    {
        Sprite sprite = CustomAvatarManager.GetAvatarSprite(profile) ?? AvatarLibrary.GetAvatarSprite("default");
        SetPreviewSprite(sprite, false);
        pendingAvatarId = profile != null ? profile.avatarId : "default";
        pendingCustomAvatarSourcePath = null;
    }

    private void SetPreviewSprite(Sprite sprite, bool isTemporary, Texture2D tempTexture = null)
    {
        ReleaseTemporaryPreview();

        pendingAvatarPreviewSprite = sprite;
        previewIsTemporary = isTemporary;
        pendingAvatarPreviewTexture = isTemporary ? (tempTexture ?? sprite?.texture) : null;

        if (avatarPreviewImage != null)
            avatarPreviewImage.sprite = sprite;
    }

    private void ReleaseTemporaryPreview()
    {
        if (!previewIsTemporary)
            return;

        if (avatarPreviewImage != null && avatarPreviewImage.sprite == pendingAvatarPreviewSprite)
            avatarPreviewImage.sprite = null;

        if (pendingAvatarPreviewSprite != null)
            DestroyRuntimeObject(pendingAvatarPreviewSprite);

        if (pendingAvatarPreviewTexture != null)
            DestroyRuntimeObject(pendingAvatarPreviewTexture);

        pendingAvatarPreviewSprite = null;
        pendingAvatarPreviewTexture = null;
        previewIsTemporary = false;
    }

    private void OnBackButtonPressed()
    {
        if (TryCloseOverlays())
            return;

        HandleBackToAuth();
    }

    private bool TryCloseOverlays()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettingsPanel();
            return true;
        }

        if (userSettingsPanel != null && userSettingsPanel.activeSelf)
        {
            CloseUserSettingsPanel();
            return true;
        }

        if (friendsPanel != null && friendsPanel.activeSelf)
        {
            CloseFriendsPanel();
            return true;
        }

        if (friendRequestsPanel != null && friendRequestsPanel.activeSelf)
        {
            CloseMessagesPanel();
            return true;
        }

        if (leaderboardPanel != null && leaderboardPanel.gameObject.activeSelf)
        {
            CloseLeaderboardPanel();
            return true;
        }

        if (replenishBalancePanel != null && replenishBalancePanel.gameObject.activeSelf)
        {
            HandleReplenishCloseRequested();
            return true;
        }

        if (rulesPanel != null && rulesPanel.gameObject.activeSelf)
        {
            rulesPanel.Hide();
            return true;
        }

        if (playLauncher != null && playLauncher.IsCreatePanelOpen)
        {
            playLauncher.CloseCreatePanel();
            return true;
        }

        return false;
    }

    private void HandleBackToAuth()
    {
        if (AuthManager.IsLoggedIn)
        {
            AuthManager.Logout();
            SceneTransitionManager.Instance?.LoadAuthScene();
        }
        else
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadAuthScene();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Auth");
        }
    }

    private void DestroyRuntimeObject(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    private void ShowOverlay(GameObject panelToShow)
    {
        if (settingsPanel != null && settingsPanel != panelToShow)
            settingsPanel.SetActive(false);
        if (userSettingsPanel != null && userSettingsPanel != panelToShow)
            userSettingsPanel.SetActive(false);
        if (friendsPanel != null && friendsPanel != panelToShow)
            friendsPanel.SetActive(false);
        if (friendRequestsPanel != null && friendRequestsPanel != panelToShow)
            friendRequestsPanel.SetActive(false);
        if (leaderboardPanel != null && leaderboardPanel.gameObject != panelToShow)
            leaderboardPanel.gameObject.SetActive(false);
        if (replenishBalancePanel != null && replenishBalancePanel.gameObject != panelToShow)
            replenishBalancePanel.Hide();
        if (rulesPanel != null && rulesPanel.gameObject != panelToShow)
            rulesPanel.Hide();

        if (panelToShow != null)
            panelToShow.SetActive(true);

        RefreshMainMenuState();
    }

    private void RefreshMainMenuState()
    {
        bool overlayActive = (settingsPanel != null && settingsPanel.activeSelf) ||
                             (userSettingsPanel != null && userSettingsPanel.activeSelf) ||
                             (friendsPanel != null && friendsPanel.activeSelf) ||
                             (friendRequestsPanel != null && friendRequestsPanel.activeSelf) ||
                             (leaderboardPanel != null && leaderboardPanel.gameObject.activeSelf) ||
                             (replenishBalancePanel != null && replenishBalancePanel.gameObject.activeSelf) ||
                             (rulesPanel != null && rulesPanel.gameObject.activeSelf);

        if (overlayActive)
            playLauncher?.CloseCreatePanel();

        if (mainMenuSections != null)
        {
            foreach (GameObject section in mainMenuSections)
            {
                if (section != null)
                    section.SetActive(!overlayActive);
            }
        }

        if (settingsButton != null)
            settingsButton.interactable = !overlayActive;
        if (userSettingsButton != null)
            userSettingsButton.interactable = !overlayActive;
        if (friendsButton != null)
            friendsButton.interactable = !overlayActive;
        if (messagesButton != null)
            messagesButton.interactable = !overlayActive;
        if (leaderboardButton != null)
            leaderboardButton.interactable = !overlayActive;
        if (rulesButton != null)
            rulesButton.interactable = !overlayActive;
        if (backToAuthButton != null)
            backToAuthButton.interactable = !overlayActive;

        if (playLauncher != null)
            playLauncher.SetMainMenuVisible(!overlayActive);
    }

    private void UpdateChipLegendDisplay()
    {
        string legend = string.IsNullOrWhiteSpace(chipLegendFormat)
            ? "Номиналы фишек: 100 • 200 • 500 • 1000"
            : chipLegendFormat;

        if (chipLegendTextTMP != null)
            chipLegendTextTMP.text = legend;
        if (chipLegendText != null)
            chipLegendText.text = legend;
    }

    private void UpdateUserStatsDisplay(UserProfile profile)
    {
        if (profile == null)
        {
            Debug.LogWarning("MainMenuUIController: Profile is null in UpdateUserStatsDisplay!");
            SetOptionalText(userBalanceText, userBalanceTextTMP, "0");
            return;
        }
        
        int chips = profile.chips;
        int level = profile.Level;
        int xp = profile.XP;

        // Отображаем только баланс с форматированием (уровень теперь рядом с ником)
        string balanceValue = FormatBalance(chips);
        string levelValue = $"Уровень {level}";

        Debug.Log($"MainMenuUIController: UpdateUserStatsDisplay - Баланс: {chips}, Уровень: {level}, XP: {xp}");
        Debug.Log($"MainMenuUIController: Отформатированный баланс: '{balanceValue}'");
        
        SetOptionalText(userBalanceText, userBalanceTextTMP, balanceValue);
        SetOptionalText(userLevelText, userLevelTextTMP, levelValue);
        
        // Проверяем, что элементы обновились
        if (userBalanceText != null)
        {
            Debug.Log($"MainMenuUIController: userBalanceText.text = '{userBalanceText.text}'");
            // Принудительно обновляем Canvas
            Canvas.ForceUpdateCanvases();
        }
        if (userBalanceTextTMP != null)
        {
            Debug.Log($"MainMenuUIController: userBalanceTextTMP.text = '{userBalanceTextTMP.text}'");
            // Принудительно обновляем Canvas
            Canvas.ForceUpdateCanvases();
        }
        
        // Отладочное сообщение
        if (userBalanceText == null && userBalanceTextTMP == null)
        {
            Debug.LogWarning("MainMenuUIController: User Balance Text не привязан! Баланс не будет отображаться. Привяжите userBalanceText или userBalanceTextTMP в Inspector Unity для элемента, который показывает баланс в правом верхнем углу.");
        }
        
        if (replenishButton == null)
        {
            Debug.LogWarning("MainMenuUIController: Replenish Button не привязана! Кнопка пополнения не будет работать. Привяжите replenishButton в Inspector.");
        }
    }
    
    private void OnReplenishButtonPressed()
    {
        Debug.Log("MainMenuUIController: OnReplenishButtonPressed вызван");
        EnsureReplenishBalancePanel();
        
        if (replenishBalancePanel == null)
        {
            Debug.LogError("MainMenuUIController: ReplenishBalancePanel не создана! Проверьте, что Canvas существует в сцене.");
            // Fallback на простое пополнение, если панель не создана
            UserProfile currentUser = AuthManager.CurrentUser;
            if (currentUser != null)
            {
                int newBalance = currentUser.chips + replenishAmount;
                AuthManager.UpdatePlayerBalance(newBalance);
                RefreshUserInfo();
            }
            return;
        }
        
        Debug.Log($"MainMenuUIController: Панель найдена, показываю. GameObject активен: {replenishBalancePanel.gameObject.activeSelf}");
        
        // ShowOverlay автоматически скроет все другие панели
        ShowOverlay(replenishBalancePanel.gameObject);
        
        // Убеждаемся, что панель на правильном слое Canvas
        Canvas canvas = replenishBalancePanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // Устанавливаем панель пополнения выше других элементов, но не слишком высоко
            replenishBalancePanel.transform.SetAsLastSibling();
            // Устанавливаем умеренный sortingOrder, чтобы не перекрывать другие важные панели
            if (canvas.sortingOrder < 100)
            {
                canvas.sortingOrder = 100;
            }
            Debug.Log($"MainMenuUIController: Панель перемещена на верхний слой Canvas: {canvas.name}, sortingOrder: {canvas.sortingOrder}");
        }
        
        replenishBalancePanel.Show();
        
        // Принудительно обновляем Canvas
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"MainMenuUIController: Панель показана. GameObject активен: {replenishBalancePanel.gameObject.activeSelf}, Transform sibling index: {replenishBalancePanel.transform.GetSiblingIndex()}");
    }

    private void EnsureReplenishBalancePanel()
    {
        if (replenishBalancePanel == null)
        {
            // Сначала проверяем, может панель уже есть на сцене (создана вручную в Unity)
            replenishBalancePanel = FindObjectOfType<ReplenishBalancePanel>();
            if (replenishBalancePanel != null)
            {
                Debug.Log("MainMenuUIController: ReplenishBalancePanel найдена на сцене (создана вручную в Unity)");
                replenishBalancePanel.OnAmountSelected += HandleReplenishAmountSelected;
                replenishBalancePanel.OnCloseRequested += HandleReplenishCloseRequested;
                return;
            }
            
            // Если панели нет на сцене, создаем её программно
            Debug.Log("MainMenuUIController: Создаю ReplenishBalancePanel программно...");
            Canvas selectedCanvas = null;
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            Debug.Log($"MainMenuUIController: Найдено Canvas: {allCanvases.Length}");
            
            foreach (var canvas in allCanvases)
            {
                if (canvas == null) continue;
                Debug.Log($"MainMenuUIController: Проверяю Canvas: {canvas.name}, RenderMode: {canvas.renderMode}");
                if (selectedCanvas == null)
                    selectedCanvas = canvas;
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    selectedCanvas = canvas;
                    Debug.Log($"MainMenuUIController: Выбран Canvas: {canvas.name}");
                    break;
                }
            }

            if (selectedCanvas != null)
            {
                Debug.Log($"MainMenuUIController: Создаю панель на Canvas: {selectedCanvas.name}");
                replenishBalancePanel = ReplenishBalancePanel.CreateDefault(selectedCanvas.transform);
                if (replenishBalancePanel != null)
                {
                    replenishBalancePanel.OnAmountSelected += HandleReplenishAmountSelected;
                    replenishBalancePanel.OnCloseRequested += HandleReplenishCloseRequested;
                    Debug.Log("MainMenuUIController: ReplenishBalancePanel создана успешно программно! Примечание: она не будет видна в Hierarchy до запуска игры.");
                }
                else
                {
                    Debug.LogError("MainMenuUIController: ReplenishBalancePanel.CreateDefault вернул null!");
                }
            }
            else
            {
                Debug.LogError("MainMenuUIController: Canvas не найден! Не могу создать панель пополнения.");
            }
        }
        else
        {
            Debug.Log("MainMenuUIController: ReplenishBalancePanel уже существует");
        }
    }

    private void EnsureAdvertisementPanel()
    {
        if (advertisementPanel == null)
        {
            Debug.Log("MainMenuUIController: AdvertisementPanel не найдена, создаю программно...");
            
            // Ищем Canvas в сцене
            Canvas selectedCanvas = null;
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            Debug.Log($"MainMenuUIController: Найдено Canvas для рекламы: {allCanvases.Length}");
            
            foreach (var canvas in allCanvases)
            {
                if (canvas == null) continue;
                Debug.Log($"MainMenuUIController: Проверяю Canvas для рекламы: {canvas.name}, RenderMode: {canvas.renderMode}");
                if (selectedCanvas == null)
                    selectedCanvas = canvas;
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    selectedCanvas = canvas;
                    Debug.Log($"MainMenuUIController: Выбран Canvas для рекламы: {canvas.name}");
                    break;
                }
            }

            if (selectedCanvas != null)
            {
                Debug.Log($"MainMenuUIController: Создаю рекламную панель на Canvas: {selectedCanvas.name}");
                advertisementPanel = AdvertisementPanel.CreateDefault(selectedCanvas.transform);
                if (advertisementPanel != null)
                {
                    Debug.Log("MainMenuUIController: AdvertisementPanel создана успешно программно!");
                }
                else
                {
                    Debug.LogError("MainMenuUIController: AdvertisementPanel.CreateDefault вернул null!");
                }
            }
            else
            {
                Debug.LogError("MainMenuUIController: Canvas не найден! Не могу создать рекламную панель.");
            }
        }
        else
        {
            Debug.Log("MainMenuUIController: AdvertisementPanel уже существует");
        }
    }

    private void HandleReplenishAmountSelected(int amount)
    {
        UserProfile currentUser = AuthManager.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogWarning("MainMenuUIController: Cannot replenish - no user logged in");
            return;
        }

        Debug.Log($"MainMenuUIController: Начинаем процесс пополнения на {amount} фишек");
        
        // Сначала показываем рекламу, затем пополняем баланс
        ShowAdvertisementBeforeReplenish(amount);
    }

    private int pendingReplenishAmount = 0;

    private void ShowAdvertisementBeforeReplenish(int amount)
    {
        EnsureAdvertisementPanel();
        
        if (advertisementPanel == null)
        {
            Debug.LogWarning("MainMenuUIController: AdvertisementPanel не создана, пополняем баланс без рекламы");
            CompleteReplenishment(amount);
            return;
        }

        // Сохраняем сумму для пополнения
        pendingReplenishAmount = amount;

        // Подписываемся на события рекламы
        advertisementPanel.OnAdCompleted += OnAdvertisementCompleted;
        advertisementPanel.OnAdClosed += OnAdvertisementClosed;

        // Показываем рекламу в зависимости от суммы пополнения
        advertisementPanel.ShowRandomAd(amount);
        
        Debug.Log($"MainMenuUIController: Показываем рекламу для суммы {amount}");
    }

    private void OnAdvertisementCompleted()
    {
        CompleteReplenishment(pendingReplenishAmount);
    }

    private void OnAdvertisementClosed()
    {
        CompleteReplenishment(pendingReplenishAmount);
    }

    private void CompleteReplenishment(int amount)
    {
        UserProfile currentUser = AuthManager.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogWarning("MainMenuUIController: Cannot replenish - no user logged in");
            return;
        }

        // Отписываемся от событий рекламы
        if (advertisementPanel != null)
        {
            advertisementPanel.OnAdCompleted -= OnAdvertisementCompleted;
            advertisementPanel.OnAdClosed -= OnAdvertisementClosed;
        }

        // Проверяем недельный лимит и пополняем баланс
        if (currentUser.AddDeposit(amount))
        {
            // Сохраняем обновленный профиль
            AuthManager.SaveCurrentUser();
            
            Debug.Log($"MainMenuUIController: Баланс пополнен на {amount} фишек. Новый баланс: {currentUser.chips}");
            Debug.Log($"MainMenuUIController: Использовано за неделю: {currentUser.currentWeekDeposits}/{currentUser.weeklyDepositLimit}");
            
            // Показываем уведомление об успешном пополнении
            ShowDepositSuccessMessage(amount, currentUser.GetRemainingWeeklyDeposit());
        }
        else
        {
            Debug.LogWarning($"MainMenuUIController: Пополнение отклонено - превышен недельный лимит");
            
            // Показываем уведомление об ошибке
            ShowDepositErrorMessage(currentUser.GetRemainingWeeklyDeposit());
        }
        
        // Обновляем отображение
        RefreshUserInfo();
        
        // Сбрасываем сумму
        pendingReplenishAmount = 0;
    }
    
    /// <summary>
    /// Проверяет, привязаны ли необходимые UI элементы
    /// </summary>
    private bool HasRequiredUIElements()
    {
        bool hasBalanceText = userBalanceText != null || userBalanceTextTMP != null;
        bool hasReplenishButton = replenishButton != null;
        
        if (!hasBalanceText)
        {
            Debug.LogWarning("MainMenuUIController: User Balance Text не привязан! Привяжите userBalanceText или userBalanceTextTMP в Inspector для корректной работы.");
        }
        
        if (!hasReplenishButton)
        {
            Debug.LogWarning("MainMenuUIController: Replenish Button не привязана! Привяжите replenishButton в Inspector для корректной работы.");
        }
        
        return hasBalanceText && hasReplenishButton;
    }
    
    private void ShowDepositSuccessMessage(int amount, int remainingLimit)
    {
        string message = $"Баланс пополнен на {amount} фишек!\n";
        if (remainingLimit > 0)
        {
            message += $"Осталось на эту неделю: {remainingLimit} фишек";
        }
        else
        {
            message += "Недельный лимит пополнений исчерпан";
        }
        
        Debug.Log($"MainMenuUIController: {message}");
        // Здесь можно добавить показ UI уведомления
    }
    
    private void ShowDepositErrorMessage(int remainingLimit)
    {
        string message = remainingLimit > 0 
            ? $"Превышен недельный лимит!\nОсталось на эту неделю: {remainingLimit} фишек"
            : "Недельный лимит пополнений исчерпан!\nПопробуйте на следующей неделе";
            
        Debug.LogWarning($"MainMenuUIController: {message}");
        // Здесь можно добавить показ UI уведомления об ошибке
    }
    
    /// <summary>
    /// Сбрасывает недельные лимиты для всех пользователей
    /// </summary>
    [ContextMenu("Сбросить недельные лимиты")]
    private void ResetWeeklyLimitsForAllUsers()
    {
        Debug.Log("MainMenuUIController: Сброс недельных лимитов для всех пользователей...");
        
        List<UserProfile> allProfiles = UserDataManager.LoadAllProfiles();
        int updatedCount = 0;
        
        foreach (UserProfile profile in allProfiles)
        {
            if (profile == null) continue;
            
            int oldDeposits = profile.currentWeekDeposits;
            
            // Сбрасываем недельные ограничения
            profile.currentWeekDeposits = 0;
            profile.weekStartDate = profile.GetStartOfWeek(System.DateTime.Now);
            
            // Сохраняем профиль
            if (UserDataManager.SaveUserProfile(profile))
            {
                updatedCount++;
                Debug.Log($"MainMenuUIController: Сброшены лимиты для {profile.username}: было {oldDeposits}/{profile.weeklyDepositLimit}, теперь 0/{profile.weeklyDepositLimit}");
            }
        }
        
        Debug.Log($"MainMenuUIController: Сброс лимитов завершен для {updatedCount} пользователей");
        
        // Если текущий пользователь авторизован, обновляем его в памяти
        UserProfile currentUser = AuthManager.CurrentUser;
        if (currentUser != null)
        {
            currentUser.currentWeekDeposits = 0;
            currentUser.weekStartDate = currentUser.GetStartOfWeek(System.DateTime.Now);
            
            // Сохраняем текущего пользователя
            AuthManager.SaveCurrentUser();
            
            Debug.Log($"MainMenuUIController: Лимиты текущего пользователя {currentUser.username} также сброшены");
        }
        
        // Обновляем отображение (только если UI элементы привязаны)
        if (HasRequiredUIElements())
        {
            RefreshUserInfo();
        }
    }

    /// <summary>
    /// Обновляет баланс всех пользователей до нового значения по умолчанию
    /// </summary>
    [ContextMenu("Обновить баланс всех пользователей")]
    private void UpdateAllUsersBalance()
    {
        Debug.Log("MainMenuUIController: Начинаем обновление баланса всех пользователей...");
        
        List<UserProfile> allProfiles = UserDataManager.LoadAllProfiles();
        int updatedCount = 0;
        
        foreach (UserProfile profile in allProfiles)
        {
            if (profile == null) continue;
            
            int oldBalance = profile.chips;
            
            // НЕ изменяем баланс - оставляем как есть
            // profile.chips = newDefaultBalance; // ОТКЛЮЧЕНО
            
            // Сбрасываем недельные ограничения для всех пользователей
            profile.currentWeekDeposits = 0;
            profile.weekStartDate = profile.GetStartOfWeek(System.DateTime.Now);
            
            // Сохраняем профиль
            if (UserDataManager.SaveUserProfile(profile))
            {
                updatedCount++;
                Debug.Log($"MainMenuUIController: Обновлен пользователь {profile.username}: {oldBalance} -> {profile.chips} фишек");
            }
            else
            {
                Debug.LogError($"MainMenuUIController: Ошибка сохранения профиля {profile.username}");
            }
        }
        
        Debug.Log($"MainMenuUIController: Обновление завершено. Обновлено {updatedCount} из {allProfiles.Count} пользователей");
        
        // Если текущий пользователь авторизован, обновляем его в памяти
        UserProfile currentUser = AuthManager.CurrentUser;
        if (currentUser != null)
        {
            // Не изменяем баланс - только сбрасываем недельные лимиты
            currentUser.currentWeekDeposits = 0;
            currentUser.weekStartDate = currentUser.GetStartOfWeek(System.DateTime.Now);
            
            // Сохраняем текущего пользователя, что автоматически вызовет событие обновления
            AuthManager.SaveCurrentUser();
            
            Debug.Log($"MainMenuUIController: Текущий пользователь {currentUser.username} также обновлен");
        }
        
        // Обновляем отображение (только если UI элементы привязаны)
        if (HasRequiredUIElements())
        {
            RefreshUserInfo();
        }
    }

    private void HandleReplenishCloseRequested()
    {
        if (replenishBalancePanel != null)
        {
            replenishBalancePanel.Hide();
            RefreshMainMenuState();
        }
    }

    private void EnsureBalanceUI()
    {
        // Если UI элементы уже привязаны, не создаем новые
        if (userBalanceText != null || userBalanceTextTMP != null)
        {
            Debug.Log("MainMenuUIController: UI элементы баланса уже привязаны");
            return;
        }
        
        // Находим Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MainMenuUIController: Canvas не найден! Не могу создать UI баланса.");
            return;
        }
        
        Debug.Log("MainMenuUIController: Создаю UI элементы баланса...");
        
        // Создаем контейнер для баланса в правом верхнем углу
        GameObject balanceContainer = new GameObject("BalanceContainer", typeof(RectTransform));
        RectTransform balanceRect = balanceContainer.GetComponent<RectTransform>();
        balanceRect.SetParent(canvas.transform, false);
        
        // Устанавливаем позицию в правом верхнем углу
        balanceRect.anchorMin = new Vector2(1f, 1f);
        balanceRect.anchorMax = new Vector2(1f, 1f);
        balanceRect.pivot = new Vector2(1f, 1f);
        balanceRect.anchoredPosition = new Vector2(-20f, -20f);
        balanceRect.sizeDelta = new Vector2(300f, 50f);
        
        // Добавляем HorizontalLayoutGroup для размещения элементов
        HorizontalLayoutGroup layoutGroup = balanceContainer.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 10f;
        layoutGroup.childAlignment = TextAnchor.MiddleRight;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        
        // Создаем текстовый элемент для баланса
        GameObject balanceTextObj = new GameObject("BalanceText", typeof(RectTransform));
        RectTransform balanceTextRect = balanceTextObj.GetComponent<RectTransform>();
        balanceTextRect.SetParent(balanceContainer.transform, false);
        balanceTextRect.sizeDelta = new Vector2(100f, 40f);
        
        // Пробуем использовать TextMeshPro, если доступен
        #if TM_PRO
        TextMeshProUGUI tmpText = balanceTextObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "0";
        tmpText.fontSize = 24;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.MidlineRight;
        tmpText.overflowMode = TextOverflowModes.Overflow;
        userBalanceTextTMP = tmpText;
        Debug.Log("MainMenuUIController: Создан TextMeshPro для баланса");
        #else
        // Используем обычный Text
        Text balanceText = balanceTextObj.AddComponent<Text>();
        balanceText.text = "0";
        balanceText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        balanceText.fontSize = 24;
        balanceText.color = Color.white;
        balanceText.alignment = TextAnchor.MiddleRight;
        balanceText.horizontalOverflow = HorizontalWrapMode.Overflow;
        balanceText.verticalOverflow = VerticalWrapMode.Overflow;
        userBalanceText = balanceText;
        Debug.Log("MainMenuUIController: Создан Text для баланса");
        #endif
        
        // Создаем кнопку "пополнить"
        GameObject replenishButtonObj = new GameObject("ReplenishButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform replenishButtonRect = replenishButtonObj.GetComponent<RectTransform>();
        replenishButtonRect.SetParent(balanceContainer.transform, false);
        replenishButtonRect.sizeDelta = new Vector2(120f, 40f);
        
        Image buttonImage = replenishButtonObj.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.7f, 0.3f, 1f); // Зеленый цвет
        
        Button button = replenishButtonObj.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // Создаем текст на кнопке
        GameObject buttonTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.SetParent(replenishButtonObj.transform, false);
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        
        Text buttonText = buttonTextObj.GetComponent<Text>();
        buttonText.text = "пополнить";
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        replenishButton = button;
        
        // Убеждаемся, что кнопка подключена к обработчику
        if (replenishButton != null)
        {
            replenishButton.onClick.RemoveListener(OnReplenishButtonPressed);
            replenishButton.onClick.AddListener(OnReplenishButtonPressed);
            Debug.Log("MainMenuUIController: Кнопка пополнения подключена в EnsureBalanceUI");
        }
        
        Debug.Log("MainMenuUIController: UI элементы баланса созданы успешно!");
    }
    
    private string FormatBalance(int chips)
    {
        // Форматируем баланс с разделителями тысяч для удобства чтения
        return chips.ToString("N0").Replace(",", " ");
    }
    
    private void SetOptionalText(Text legacy, TMP_Text tmp, string value)
    {
        if (tmp != null)
        {
            tmp.text = value;
            // Убеждаемся, что TextMeshPro не обрезает текст
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }
        if (legacy != null)
        {
            legacy.text = value;
            // Убеждаемся, что Text не обрезает текст
            legacy.horizontalOverflow = HorizontalWrapMode.Overflow;
            legacy.verticalOverflow = VerticalWrapMode.Overflow;
            legacy.resizeTextForBestFit = false;
            // Принудительно обновляем Canvas
            Canvas.ForceUpdateCanvases();
        }
    }

    private void BuildLeaderboardSections(out List<LeaderboardEntry> topBalance, out List<LeaderboardEntry> topLevel)
    {
        var profiles = AuthManager.GetAllProfilesSnapshot() ?? new List<UserProfile>();
        var current = AuthManager.CurrentUser;

        Debug.Log($"MainMenuUIController: Загружено профилей для таблицы лидеров: {profiles.Count}");

        // Убираем дубликаты по username (берем последний профиль для каждого username)
        var uniqueProfiles = profiles
            .Where(p => !string.IsNullOrWhiteSpace(p?.username))
            .GroupBy(p => p.username, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(p => p.lastLoginDate).First())
            .ToList();

        Debug.Log($"MainMenuUIController: Уникальных профилей после удаления дубликатов: {uniqueProfiles.Count}");

        var entries = uniqueProfiles.Select(profile => new LeaderboardEntry
        {
            username = string.IsNullOrWhiteSpace(profile.username) ? "Игрок" : profile.username,
            level = profile.Level,
            chips = profile.chips,
            xp = profile.XP,
            isCurrentUser = current != null && string.Equals(profile.username, current.username, StringComparison.OrdinalIgnoreCase)
        }).ToList();

        topBalance = entries
            .OrderByDescending(e => e.chips)
            .ThenByDescending(e => e.level)
            .ThenBy(e => e.username, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        topLevel = entries
            .OrderByDescending(e => e.level)
            .ThenByDescending(e => e.xp)
            .ThenByDescending(e => e.chips)
            .ThenBy(e => e.username, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }

    private void EnsureLeaderboardPanel()
    {
        if (leaderboardPanel == null)
        {
            Canvas selectedCanvas = null;
            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                if (canvas == null) continue;
                if (selectedCanvas == null)
                    selectedCanvas = canvas;
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    selectedCanvas = canvas;
                    break;
                }
            }

            if (selectedCanvas != null)
                leaderboardPanel = LeaderboardPanel.CreateDefault(selectedCanvas.transform);
        }

        if (leaderboardPanel != null)
        {
            leaderboardPanel.Hide();
            leaderboardPanel.OnCloseRequested -= HandleLeaderboardCloseRequested;
            leaderboardPanel.OnCloseRequested += HandleLeaderboardCloseRequested;
        }
    }

    private void OpenLeaderboardPanel()
    {
        Debug.Log("MainMenuUIController: OpenLeaderboardPanel вызван");
        EnsureLeaderboardPanel();
        if (leaderboardPanel == null)
        {
            Debug.LogError("MainMenuUIController: leaderboardPanel is null!");
            return;
        }

        BuildLeaderboardSections(out var topBalance, out var topLevel);
        Debug.Log($"MainMenuUIController: Построены секции. По балансу: {topBalance?.Count ?? 0}, По уровню: {topLevel?.Count ?? 0}");

        ShowOverlay(leaderboardPanel.gameObject);
        leaderboardPanel.Show(topBalance, topLevel);
        
        // Убеждаемся, что панель на правильном слое
        leaderboardPanel.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        
        Debug.Log("MainMenuUIController: Таблица лидеров показана");
    }

    private void CloseLeaderboardPanel()
    {
        if (leaderboardPanel == null)
            return;

        leaderboardPanel.Hide();
        RefreshMainMenuState();
    }

    private void HandleLeaderboardCloseRequested()
    {
        CloseLeaderboardPanel();
    }

    private void EnsureRulesPanel()
    {
        if (rulesPanel == null)
        {
            Canvas selectedCanvas = FindObjectOfType<Canvas>();
            if (selectedCanvas == null)
            {
                Debug.LogWarning("MainMenuUIController: Canvas не найден для создания панели правил!");
                return;
            }

            rulesPanel = RulesPanel.CreateDefault(selectedCanvas.transform);
            if (rulesPanel != null)
            {
                rulesPanel.gameObject.name = "RulesPanel";
                rulesPanel.Hide();
                rulesPanel.OnCloseRequested -= HandleRulesCloseRequested;
                rulesPanel.OnCloseRequested += HandleRulesCloseRequested;
            }
        }
    }

    private void OpenRulesPanel()
    {
        EnsureRulesPanel();
        if (rulesPanel == null)
            return;

        ShowOverlay(rulesPanel.gameObject);
        rulesPanel.Show();
    }

    private void CloseRulesPanel()
    {
        if (rulesPanel == null)
            return;

        rulesPanel.Hide();
        RefreshMainMenuState();
    }

    private void HandleRulesCloseRequested()
    {
        CloseRulesPanel();
    }

    private void EnsureMenuButtons()
    {
        // Если кнопки уже привязаны, не создаем новые
        if (leaderboardButton != null && rulesButton != null)
        {
            Debug.Log("MainMenuUIController: Кнопки меню уже привязаны");
            return;
        }

        // Находим Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("MainMenuUIController: Canvas не найден! Не могу создать кнопки меню.");
            return;
        }

        // Пытаемся найти контейнер с другими кнопками (профиль, друзья, сообщения)
        // Или создаем новый контейнер рядом с аватаром
        Transform buttonsContainer = null;
        
        // Ищем кнопку профиля, чтобы найти родительский контейнер
        if (userSettingsButton != null && userSettingsButton.transform.parent != null)
        {
            buttonsContainer = userSettingsButton.transform.parent;
            Debug.Log("MainMenuUIController: Найден контейнер кнопок через userSettingsButton");
        }
        else if (friendsButton != null && friendsButton.transform.parent != null)
        {
            buttonsContainer = friendsButton.transform.parent;
            Debug.Log("MainMenuUIController: Найден контейнер кнопок через friendsButton");
        }
        else
        {
            // Создаем новый контейнер для кнопок меню
            GameObject containerObj = new GameObject("MenuButtonsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform containerRect = containerObj.GetComponent<RectTransform>();
            containerRect.SetParent(canvas.transform, false);
            
            // Позиционируем контейнер в верхней части экрана, слева от центра
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(0f, 1f);
            containerRect.pivot = new Vector2(0f, 1f);
            containerRect.anchoredPosition = new Vector2(20f, -80f);
            containerRect.sizeDelta = new Vector2(600f, 50f);

            HorizontalLayoutGroup layoutGroup = containerObj.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);

            buttonsContainer = containerRect;
            Debug.Log("MainMenuUIController: Создан новый контейнер для кнопок меню");
        }

        if (buttonsContainer == null)
        {
            Debug.LogError("MainMenuUIController: Не удалось найти или создать контейнер для кнопок меню!");
            return;
        }

        // Создаем кнопку "Таблица лидеров", если её нет
        if (leaderboardButton == null)
        {
            leaderboardButton = CreateMenuButton("LeaderboardButton", "Таблица лидеров", 
                new Color(0.3f, 0.6f, 0.9f, 1f), buttonsContainer);
            Debug.Log("MainMenuUIController: Создана кнопка 'Таблица лидеров'");
        }

        // Создаем кнопку "Правила", если её нет
        if (rulesButton == null)
        {
            rulesButton = CreateMenuButton("RulesButton", "Правила", 
                new Color(0.9f, 0.7f, 0.3f, 1f), buttonsContainer);
            Debug.Log("MainMenuUIController: Создана кнопка 'Правила'");
        }

        // Подключаем обработчики событий
        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveListener(OpenLeaderboardPanel);
            leaderboardButton.onClick.AddListener(OpenLeaderboardPanel);
        }
        if (rulesButton != null)
        {
            rulesButton.onClick.RemoveListener(OpenRulesPanel);
            rulesButton.onClick.AddListener(OpenRulesPanel);
        }
    }

    private Button CreateMenuButton(string name, string label, Color color, Transform parent)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.sizeDelta = new Vector2(150f, 40f);

        Image buttonImage = buttonObj.GetComponent<Image>();
        buttonImage.color = color;

        Button button = buttonObj.GetComponent<Button>();
        button.targetGraphic = buttonImage;

        // Создаем текст на кнопке
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.SetParent(buttonObj.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textObj.GetComponent<Text>();
        buttonText.text = label;
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.horizontalOverflow = HorizontalWrapMode.Wrap;
        buttonText.verticalOverflow = VerticalWrapMode.Truncate;

        return button;
    }
}
