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
    [SerializeField] private Button userSettingsButton;
    [SerializeField] private Button friendsButton;
    [SerializeField] private Button messagesButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Text userLevelText;
    [SerializeField] private TMP_Text userLevelTextTMP;
    [SerializeField] private Text userBalanceText;
    [SerializeField] private TMP_Text userBalanceTextTMP;

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

    private GameSettings cachedSettings;
    private List<CardThemeInfo> themeOptions = new List<CardThemeInfo>();
    private string pendingAvatarId;
    private string pendingCustomAvatarSourcePath;
    private Sprite pendingAvatarPreviewSprite;
    private Texture2D pendingAvatarPreviewTexture;
    private bool previewIsTemporary;

    private void Awake()
    {
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

        HideSettingsPanel();
        HideUserSettingsPanel();
        HideFriendsPanel();
        HideRequestsPanel();
        RefreshMainMenuState();
        UpdateChipLegendDisplay();
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
        ReleaseTemporaryPreview();
        if (leaderboardPanel != null)
            leaderboardPanel.OnCloseRequested -= HandleLeaderboardCloseRequested;
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
        if (userNicknameText != null)
        {
            userNicknameText.text = profile != null && !string.IsNullOrEmpty(profile.username)
                ? profile.username
                : "Гость";
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

    private void DestroyRuntimeObject(Object obj)
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
                             (leaderboardPanel != null && leaderboardPanel.gameObject.activeSelf);

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
        int chips = profile?.chips ?? 0;
        int level = profile?.Level ?? 1;

        string balanceValue = $"Баланс: {chips}";
        string levelValue = $"Уровень: {level}";

        SetOptionalText(userBalanceText, userBalanceTextTMP, balanceValue);
        SetOptionalText(userLevelText, userLevelTextTMP, levelValue);
    }

    private void SetOptionalText(Text legacy, TMP_Text tmp, string value)
    {
        if (tmp != null)
            tmp.text = value;
        if (legacy != null)
            legacy.text = value;
    }

    private void BuildLeaderboardSections(out List<LeaderboardEntry> topBalance, out List<LeaderboardEntry> topLevel)
    {
        var profiles = AuthManager.GetAllProfilesSnapshot() ?? new List<UserProfile>();
        var current = AuthManager.CurrentUser;

        var entries = profiles.Select(profile => new LeaderboardEntry
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
        EnsureLeaderboardPanel();
        if (leaderboardPanel == null)
            return;

        BuildLeaderboardSections(out var topBalance, out var topLevel);

        ShowOverlay(leaderboardPanel.gameObject);
        leaderboardPanel.Show(topBalance, topLevel);
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
}
