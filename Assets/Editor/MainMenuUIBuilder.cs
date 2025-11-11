using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuUIBuilder : EditorWindow
{
    [MenuItem("Tools/Poker/Main Menu UI Builder")]
    public static void ShowWindow()
    {
        GetWindow<MainMenuUIBuilder>("Main Menu UI Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Генератор главного меню", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Создать/Пересоздать главное меню", GUILayout.Height(30)))
        {
            CreateMainMenuUI();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Команда пересоздаёт Canvas и полностью настраивает рабочее меню: \n- кнопки 'Присоединиться', 'Создать стол', 'Назад'\n- панель создания стола с вводом блайнда и выбором мест\n- компонент MenuPlayLauncher с привязанными ссылками.", MessageType.Info);
    }

    private static void CreateMainMenuUI()
    {        
        Canvas canvas = EnsureCanvas();

        // Удаляем старый объект меню, если он есть
        Transform oldMenu = canvas.transform.Find("MainMenuUI");
        if (oldMenu != null)
        {
            Undo.DestroyObjectImmediate(oldMenu.gameObject);
        }

        GameObject root = new GameObject("MainMenuUI");
        Undo.RegisterCreatedObjectUndo(root, "Create MainMenuUI root");
        root.transform.SetParent(canvas.transform, false);

        // Фоновая панель
        GameObject menuBackground = CreateImagePanel("MenuBackground", root.transform, Color.white);
        RectTransform bgRect = menuBackground.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(800, 520);

        // Панель с основными кнопками
        GameObject primaryPanel = new GameObject("PrimaryButtons", typeof(Image));
        Undo.RegisterCreatedObjectUndo(primaryPanel, "Create PrimaryButtons");
        primaryPanel.transform.SetParent(root.transform, false);
        Image primaryImage = primaryPanel.GetComponent<Image>();
        primaryImage.color = new Color(1f, 1f, 1f, 0.9f);
        RectTransform primaryRect = primaryPanel.GetComponent<RectTransform>();
        primaryRect.anchorMin = new Vector2(0.5f, 0.5f);
        primaryRect.anchorMax = new Vector2(0.5f, 0.5f);
        primaryRect.sizeDelta = new Vector2(380, 300);
        primaryRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup primaryLayout = primaryPanel.AddComponent<VerticalLayoutGroup>();
        primaryLayout.spacing = 20f;
        primaryLayout.padding = new RectOffset(40, 40, 40, 40);
        primaryLayout.childAlignment = TextAnchor.MiddleCenter;
        ContentSizeFitter primaryFitter = primaryPanel.AddComponent<ContentSizeFitter>();
        primaryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button joinButton = CreateButton("JoinDefaultButton", primaryPanel.transform, "присоединиться к столу");
        Button openCreateButton = CreateButton("OpenCreatePanelButton", primaryPanel.transform, "создать стол");
        Button settingsButton = CreateButton("SettingsButton", primaryPanel.transform, "настройки");
        Button backButton = CreateButton("BackButton", primaryPanel.transform, "назад");

        // Панель создания стола
        GameObject createPanel = new GameObject("CreateTablePanel", typeof(Image));
        Undo.RegisterCreatedObjectUndo(createPanel, "Create CreateTablePanel");
        createPanel.transform.SetParent(root.transform, false);
        Image createImage = createPanel.GetComponent<Image>();
        createImage.color = new Color(1f, 1f, 1f, 0.95f);
        RectTransform createRect = createPanel.GetComponent<RectTransform>();
        createRect.anchorMin = new Vector2(0.5f, 0.5f);
        createRect.anchorMax = new Vector2(0.5f, 0.5f);
        createRect.sizeDelta = new Vector2(480, 360);
        createRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup createLayout = createPanel.AddComponent<VerticalLayoutGroup>();
        createLayout.spacing = 16f;
        createLayout.padding = new RectOffset(40, 40, 40, 40);
        createLayout.childAlignment = TextAnchor.MiddleCenter;

        ContentSizeFitter createFitter = createPanel.AddComponent<ContentSizeFitter>();
        createFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText("CreateTitle", createPanel.transform, "настройки стола", 26);
        InputField blindInput = CreateInputField("BigBlindInput", createPanel.transform, "введите начальный блайнд", "2000");
        Dropdown seatsDropdown = CreateDropdown("MaxSeatsDropdown", createPanel.transform, "выберите количество мест", 2, 9);
        Button createPlayButton = CreateButton("CreateAndPlayButton", createPanel.transform, "создать и играть");
        Button cancelButton = CreateButton("CancelCreateButton", createPanel.transform, "назад");
        createPanel.SetActive(false);

        // Привязка MenuPlayLauncher
        MenuPlayLauncher launcher = root.GetComponent<MenuPlayLauncher>();
        if (launcher == null)
        {
            launcher = root.AddComponent<MenuPlayLauncher>();
        }

        SerializedObject so = new SerializedObject(launcher);
        so.FindProperty("joinDefaultTableButton").objectReferenceValue = joinButton;
        so.FindProperty("openCreateTableButton").objectReferenceValue = openCreateButton;
        so.FindProperty("createPanel").objectReferenceValue = createPanel;
        so.FindProperty("bigBlindInput").objectReferenceValue = blindInput;
        so.FindProperty("maxSeatsDropdown").objectReferenceValue = seatsDropdown;
        so.FindProperty("createAndPlayButton").objectReferenceValue = createPlayButton;
        so.FindProperty("cancelCreateButton").objectReferenceValue = cancelButton;
        so.FindProperty("primaryButtons").objectReferenceValue = primaryPanel;
        so.FindProperty("menuBackground").objectReferenceValue = menuBackground;
        so.FindProperty("gameSceneName").stringValue = "Main";
        so.ApplyModifiedProperties();

        // Пользовательский хедер
        GameObject userHeader = CreateUserHeader(root.transform, out Image headerAvatar, out Text headerNickname, out Button openUserSettingsButton, out Button friendsButton, out Button messagesButton);

        // Панель настроек пользователя
        GameObject userSettingsPanel = CreateUserSettingsPanel(root.transform,
            out InputField nicknameInput,
            out Button changeAvatarButton,
            out Button resetAvatarButton,
            out Image avatarPreview,
            out Button saveUserSettingsButton,
            out Button cancelUserSettingsButton);

        GameObject friendsPanel = CreateFriendsPanel(root.transform, out FriendListController friendsController);
        GameObject friendRequestsPanel = CreateFriendRequestsPanel(root.transform, out FriendRequestCenterController requestsController);

        // Панель общих настроек
        GameObject settingsPanel = CreateSettingsPanel(root.transform,
            out Slider volumeSlider,
            out Slider brightnessSlider,
            out Dropdown languageDropdown,
            out Dropdown cardThemeDropdown,
            out Image cardPreviewImage,
            out Button applySettingsButton,
            out Button closeSettingsButton);
 
        // Яркостный оверлей
        Transform oldOverlay = canvas.transform.Find("BrightnessOverlay");
        if (oldOverlay != null)
        {
            Undo.DestroyObjectImmediate(oldOverlay.gameObject);
        }
        Image brightnessOverlay = CreateBrightnessOverlay(canvas.transform);

        // Контроллер главного меню
        MainMenuUIController uiController = root.GetComponent<MainMenuUIController>();
        if (uiController == null)
        {
            uiController = root.AddComponent<MainMenuUIController>();
        }

        SerializedObject controllerSO = new SerializedObject(uiController);
        controllerSO.FindProperty("playLauncher").objectReferenceValue = launcher;
        controllerSO.FindProperty("settingsButton").objectReferenceValue = settingsButton;
        controllerSO.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        controllerSO.FindProperty("closeSettingsButton").objectReferenceValue = closeSettingsButton;
        controllerSO.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
        controllerSO.FindProperty("brightnessSlider").objectReferenceValue = brightnessSlider;
        controllerSO.FindProperty("languageDropdown").objectReferenceValue = languageDropdown;
        controllerSO.FindProperty("cardThemeDropdown").objectReferenceValue = cardThemeDropdown;
        controllerSO.FindProperty("cardThemePreview").objectReferenceValue = cardPreviewImage;
        controllerSO.FindProperty("applySettingsButton").objectReferenceValue = applySettingsButton;
        controllerSO.FindProperty("userAvatarImage").objectReferenceValue = headerAvatar;
        controllerSO.FindProperty("userNicknameText").objectReferenceValue = headerNickname;
        controllerSO.FindProperty("userSettingsButton").objectReferenceValue = openUserSettingsButton;
        controllerSO.FindProperty("friendsButton").objectReferenceValue = friendsButton;
        controllerSO.FindProperty("messagesButton").objectReferenceValue = messagesButton;
        controllerSO.FindProperty("userSettingsPanel").objectReferenceValue = userSettingsPanel;
        controllerSO.FindProperty("nicknameInput").objectReferenceValue = nicknameInput;
        controllerSO.FindProperty("changeAvatarButton").objectReferenceValue = changeAvatarButton;
        controllerSO.FindProperty("resetAvatarButton").objectReferenceValue = resetAvatarButton;
        controllerSO.FindProperty("avatarPreviewImage").objectReferenceValue = avatarPreview;
        controllerSO.FindProperty("saveUserSettingsButton").objectReferenceValue = saveUserSettingsButton;
        controllerSO.FindProperty("cancelUserSettingsButton").objectReferenceValue = cancelUserSettingsButton;
        controllerSO.FindProperty("friendsPanel").objectReferenceValue = friendsPanel;
        controllerSO.FindProperty("friendsListController").objectReferenceValue = friendsController;
        controllerSO.FindProperty("friendRequestsPanel").objectReferenceValue = friendRequestsPanel;
        controllerSO.FindProperty("friendRequestCenter").objectReferenceValue = requestsController;
        var sectionsProp = controllerSO.FindProperty("mainMenuSections");
        sectionsProp.arraySize = 3;
        sectionsProp.GetArrayElementAtIndex(0).objectReferenceValue = menuBackground;
        sectionsProp.GetArrayElementAtIndex(1).objectReferenceValue = primaryPanel;
        sectionsProp.GetArrayElementAtIndex(2).objectReferenceValue = userHeader;
        controllerSO.ApplyModifiedProperties();

        // Контроллер настроек
        MainMenuSettingsController settingsController = root.GetComponent<MainMenuSettingsController>();
        if (settingsController == null)
        {
            settingsController = root.AddComponent<MainMenuSettingsController>();
        }

        SerializedObject settingsSO = new SerializedObject(settingsController);
        settingsSO.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
        settingsSO.FindProperty("brightnessSlider").objectReferenceValue = brightnessSlider;
        settingsSO.FindProperty("brightnessOverlay").objectReferenceValue = brightnessOverlay;
        settingsSO.FindProperty("languageDropdown").objectReferenceValue = languageDropdown;
        settingsSO.ApplyModifiedProperties();

        // Локализация
        AutoLocalizationByName localization = root.GetComponent<AutoLocalizationByName>();
        if (localization == null)
        {
            localization = root.AddComponent<AutoLocalizationByName>();
        }

        SerializedObject locSO = new SerializedObject(localization);
        SerializedProperty entriesProp = locSO.FindProperty("entries");
        entriesProp.arraySize = 32;
        SetLocalizationEntry(entriesProp, 0, "JoinDefaultButton", "присоединиться к столу", "Join table");
        SetLocalizationEntry(entriesProp, 1, "OpenCreatePanelButton", "создать стол", "Create table");
        SetLocalizationEntry(entriesProp, 2, "SettingsButton", "настройки", "Settings");
        SetLocalizationEntry(entriesProp, 3, "BackButton", "назад", "Back");
        SetLocalizationEntry(entriesProp, 4, "CreateTitle", "настройки стола", "Table settings");
        SetLocalizationEntry(entriesProp, 5, "CreateAndPlayButton", "создать и играть", "Create & play");
        SetLocalizationEntry(entriesProp, 6, "CancelCreateButton", "назад", "Back");
        SetLocalizationEntry(entriesProp, 7, "SettingsTitle", "Настройки", "Settings");
        SetLocalizationEntry(entriesProp, 8, "MasterVolumeSlider", "громкость", "Volume");
        SetLocalizationEntry(entriesProp, 9, "BrightnessSlider", "яркость", "Brightness");
        SetLocalizationEntry(entriesProp, 10, "LanguageDropdown", "язык", "Language");
        SetLocalizationEntry(entriesProp, 11, "CardThemeDropdown", "оформление карт", "Card theme");
        SetLocalizationEntry(entriesProp, 12, "ApplySettingsButton", "применить", "Apply");
        SetLocalizationEntry(entriesProp, 13, "CloseSettingsButton", "закрыть", "Close");
        SetLocalizationEntry(entriesProp, 14, "UserSettingsButton", "профиль", "Profile");
        SetLocalizationEntry(entriesProp, 15, "UserSettingsTitle", "Профиль игрока", "Player profile");
        SetLocalizationEntry(entriesProp, 16, "ChangeAvatarButton", "сменить аватар", "Change avatar");
        SetLocalizationEntry(entriesProp, 17, "ResetAvatarButton", "сбросить аватар", "Reset avatar");
        SetLocalizationEntry(entriesProp, 18, "SaveUserSettingsButton", "сохранить", "Save");
        SetLocalizationEntry(entriesProp, 19, "CancelUserSettingsButton", "отмена", "Cancel");
        SetLocalizationEntry(entriesProp, 20, "FriendsButton", "друзья", "Friends");
        SetLocalizationEntry(entriesProp, 21, "MessagesButton", "сообщения", "Messages");
        SetLocalizationEntry(entriesProp, 22, "FriendsTitle", "Друзья", "Friends");
        SetLocalizationEntry(entriesProp, 23, "AddFriendButton", "добавить", "Add");
        SetLocalizationEntry(entriesProp, 24, "CloseFriendsButton", "закрыть", "Close");
        SetLocalizationEntry(entriesProp, 25, "RequestsTitle", "Заявки в друзья", "Friend requests");
        SetLocalizationEntry(entriesProp, 26, "IncomingLabel", "Входящие заявки", "Incoming");
        SetLocalizationEntry(entriesProp, 27, "OutgoingLabel", "Отправленные заявки", "Outgoing");
        SetLocalizationEntry(entriesProp, 28, "CloseRequestsButton", "закрыть", "Close");
        SetLocalizationEntry(entriesProp, 29, "AcceptButton", "принять", "Accept");
        SetLocalizationEntry(entriesProp, 30, "DeclineButton", "отклонить", "Decline");
        SetLocalizationEntry(entriesProp, 31, "CancelButton", "отменить", "Cancel");
        locSO.ApplyModifiedProperties();
 
        // Назначаем "назад" на главной панели, чтобы закрывать игру (можно повесить выход в будущее)
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() => Debug.Log("Назад: повесьте сюда нужное действие"));

        // Гарантируем наличие SceneTransitionManager
        EnsureSceneTransitionManager();

        Debug.Log("✅ Главное меню создано и настроено.\nCanvas: " + canvas.gameObject.name + "\nRoot: " + root.name);
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        return canvas;
    }

    private static GameObject CreateImagePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create " + name);
        panel.transform.SetParent(parent, false);

        Image image = panel.GetComponent<Image>();
        image.color = color;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600, 400);
        rect.anchoredPosition = Vector2.zero;

        return panel;
    }

    private static Text CreateText(string name, Transform parent, string text, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(Text));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        Text txt = go.GetComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 40);

        return txt;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        GameObject go = new GameObject(name, typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.55f, 0.2f, 1f);

        Button button = go.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(Text));
        Undo.RegisterCreatedObjectUndo(textGO, "Create text for " + name);
        textGO.transform.SetParent(go.transform, false);
        Text text = textGO.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320, 60);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static InputField CreateInputField(string name, Transform parent, string placeholder, string defaultValue)
    {
        GameObject go = new GameObject(name, typeof(Image), typeof(InputField));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 1f);

        InputField input = go.GetComponent<InputField>();
        input.text = defaultValue;
        input.keyboardType = TouchScreenKeyboardType.NumberPad;
        input.contentType = InputField.ContentType.IntegerNumber;

        // Placeholder
        GameObject placeholderGO = new GameObject("Placeholder", typeof(Text));
        placeholderGO.transform.SetParent(go.transform, false);
        Text placeholderText = placeholderGO.GetComponent<Text>();
        placeholderText.text = placeholder;
        placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        placeholderText.fontSize = 18;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        placeholderText.alignment = TextAnchor.MiddleLeft;

        // Текстовое поле
        GameObject textGO = new GameObject("Text", typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        Text text = textGO.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 20;
        text.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        text.alignment = TextAnchor.MiddleLeft;

        input.placeholder = placeholderText;
        input.textComponent = text;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360, 50);

        RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0, 0);
        placeholderRect.anchorMax = new Vector2(1, 1);
        placeholderRect.offsetMin = new Vector2(15, 0);
        placeholderRect.offsetMax = new Vector2(-15, 0);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(15, 0);
        textRect.offsetMax = new Vector2(-15, 0);

        return input;
    }

    private static Dropdown CreateDropdown(string name, Transform parent, string label, int minValue, int maxValue)
    {
        Dropdown dropdown = CreateDropdownBase(name, parent, label);
        dropdown.options.Clear();
        for (int i = minValue; i <= maxValue; i++)
        {
            dropdown.options.Add(new Dropdown.OptionData(i.ToString()));
        }
        dropdown.value = 0;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static Dropdown CreateDropdownContainer(string name, Transform parent, string label)
    {
        Dropdown dropdown = CreateDropdownBase(name, parent, label);
        dropdown.options.Clear();
        dropdown.captionText.text = "";
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static Dropdown CreateDropdownBase(string name, Transform parent, string label)
    {
        GameObject container = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(container, "Create " + name);
        container.transform.SetParent(parent, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.sizeDelta = new Vector2(420f, 110f);

        Text labelText = CreateText("Label", container.transform, label, 20);
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = new Vector2(360f, 28f);
        labelRect.anchoredPosition = Vector2.zero;

        DefaultControls.Resources resources = new DefaultControls.Resources();
        GameObject dropdownGO = DefaultControls.CreateDropdown(resources);
        dropdownGO.name = "Dropdown";
        Undo.RegisterCreatedObjectUndo(dropdownGO, "Create dropdown for " + name);
        dropdownGO.transform.SetParent(container.transform, false);

        RectTransform dropRect = dropdownGO.GetComponent<RectTransform>();
        dropRect.anchorMin = new Vector2(0.5f, 1f);
        dropRect.anchorMax = new Vector2(0.5f, 1f);
        dropRect.pivot = new Vector2(0.5f, 1f);
        dropRect.sizeDelta = new Vector2(360f, 52f);
        dropRect.anchoredPosition = new Vector2(0f, -42f);

        Dropdown dropdown = dropdownGO.GetComponent<Dropdown>();

        if (dropdown.captionText != null)
        {
            dropdown.captionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            dropdown.captionText.fontSize = 20;
            dropdown.captionText.color = Color.white;
            dropdown.captionText.alignment = TextAnchor.MiddleLeft;
            RectTransform captionRect = dropdown.captionText.rectTransform;
            captionRect.offsetMin = new Vector2(15f, 0f);
            captionRect.offsetMax = new Vector2(-25f, 0f);
            if (dropdown.captionImage != null)
                dropdown.captionImage.enabled = false;
        }

        if (dropdown.template != null)
        {
            RectTransform templateRect = dropdown.template;
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.sizeDelta = new Vector2(0f, 160f);

            Transform itemTransform = templateRect.Find("Viewport/Content/Item");
            if (itemTransform != null)
            {
                Text itemLabel = itemTransform.Find("Item Label")?.GetComponent<Text>() ?? itemTransform.Find("Label")?.GetComponent<Text>();
                if (itemLabel != null)
                {
                    itemLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    itemLabel.fontSize = 20;
                    itemLabel.color = Color.black;
                    itemLabel.alignment = TextAnchor.MiddleLeft;
                }

                Toggle itemToggle = itemTransform.GetComponent<Toggle>();
                if (itemToggle != null)
                {
                    Image itemBackground = itemToggle.targetGraphic as Image;
                    if (itemBackground != null)
                        itemBackground.color = Color.white;

                    Image itemCheckmark = itemToggle.graphic as Image;
                    if (itemCheckmark != null)
                        itemCheckmark.color = new Color(0.15f, 0.45f, 0.2f, 1f);
                }
            }
        }

        DropdownStyler.Apply(dropdown);

        return dropdown;
    }

    private static Slider CreateSliderControl(string name, Transform parent, string label)
    {
        GameObject container = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(container, "Create " + name);
        container.transform.SetParent(parent, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.sizeDelta = new Vector2(420f, 90f);

        Text labelText = CreateText("Label", container.transform, label, 20);
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = new Vector2(360f, 28f);
        labelRect.anchoredPosition = Vector2.zero;

        GameObject sliderGO = new GameObject("Slider", typeof(Slider));
        Undo.RegisterCreatedObjectUndo(sliderGO, "Create slider for " + name);
        sliderGO.transform.SetParent(container.transform, false);
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 1f);
        sliderRect.anchorMax = new Vector2(0.5f, 1f);
        sliderRect.pivot = new Vector2(0.5f, 1f);
        sliderRect.sizeDelta = new Vector2(360f, 38f);
        sliderRect.anchoredPosition = new Vector2(0f, -40f);

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;

        GameObject backgroundGO = new GameObject("Background", typeof(Image));
        backgroundGO.transform.SetParent(sliderGO.transform, false);
        Image backgroundImage = backgroundGO.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.25f);
        RectTransform backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.35f);
        backgroundRect.anchorMax = new Vector2(1f, 0.65f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.35f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.65f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject fillGO = new GameObject("Fill", typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        Image fillImage = fillGO.GetComponent<Image>();
        fillImage.color = new Color(0.2f, 0.55f, 0.2f, 1f);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handleGO = new GameObject("Handle", typeof(Image));
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        Image handleImage = handleGO.GetComponent<Image>();
        handleImage.color = Color.white;
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 20f);
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private static InputField CreateTextInputField(string name, Transform parent, string placeholder, string defaultValue)
    {
        GameObject go = new GameObject(name, typeof(Image), typeof(InputField));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 1f);

        InputField input = go.GetComponent<InputField>();
        input.text = defaultValue;
        input.contentType = InputField.ContentType.Standard;
        input.lineType = InputField.LineType.SingleLine;

        GameObject placeholderGO = new GameObject("Placeholder", typeof(Text));
        placeholderGO.transform.SetParent(go.transform, false);
        Text placeholderText = placeholderGO.GetComponent<Text>();
        placeholderText.text = placeholder;
        placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        placeholderText.fontSize = 18;
        placeholderText.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        placeholderText.alignment = TextAnchor.MiddleLeft;

        GameObject textGO = new GameObject("Text", typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        Text text = textGO.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 20;
        text.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        text.alignment = TextAnchor.MiddleLeft;

        input.placeholder = placeholderText;
        input.textComponent = text;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360, 50);

        RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0, 0);
        placeholderRect.anchorMax = new Vector2(1, 1);
        placeholderRect.offsetMin = new Vector2(15, 0);
        placeholderRect.offsetMax = new Vector2(-15, 0);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(15, 0);
        textRect.offsetMax = new Vector2(-15, 0);

        return input;
    }

    private static Image CreatePreviewImage(string name, Transform parent, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 1f);
        image.preserveAspect = true;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        return image;
    }

    private static void SetButtonLayout(Button button, float width, float height)
    {
        if (button == null) return;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
            layout = button.gameObject.AddComponent<LayoutElement>();

        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.minWidth = width;
        layout.minHeight = height;
    }

    private static void EnsureSceneTransitionManager()
    {
        SceneTransitionManager manager = Object.FindObjectOfType<SceneTransitionManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("SceneTransitionManager");
            Undo.RegisterCreatedObjectUndo(go, "Create SceneTransitionManager");
            go.AddComponent<SceneTransitionManager>();
        }
    }

private static GameObject CreateUserHeader(Transform parent, out Image avatarImage, out Text nicknameText, out Button settingsButton, out Button friendsButton, out Button messagesButton)
    {
        GameObject header = new GameObject("UserHeader", typeof(Image));
        Undo.RegisterCreatedObjectUndo(header, "Create UserHeader");
        header.transform.SetParent(parent, false);
        Image headerImage = header.GetComponent<Image>();
        headerImage.color = new Color(0f, 0f, 0f, 0.25f);

        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(100f, -40f);
        rect.sizeDelta = new Vector2(480f, 110f);

        GameObject avatarGO = new GameObject("Avatar", typeof(Image));
        Undo.RegisterCreatedObjectUndo(avatarGO, "Create Avatar");
        avatarGO.transform.SetParent(header.transform, false);
        avatarImage = avatarGO.GetComponent<Image>();
        avatarImage.color = Color.white;
        RectTransform avatarRect = avatarGO.GetComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0f, 0.5f);
        avatarRect.anchorMax = new Vector2(0f, 0.5f);
        avatarRect.pivot = new Vector2(0.5f, 0.5f);
        avatarRect.sizeDelta = new Vector2(72f, 72f);
        avatarRect.anchoredPosition = new Vector2(60f, 0f);

        GameObject nicknameGO = new GameObject("NicknameText", typeof(Text));
        Undo.RegisterCreatedObjectUndo(nicknameGO, "Create NicknameText");
        nicknameGO.transform.SetParent(header.transform, false);
        nicknameText = nicknameGO.GetComponent<Text>();
        nicknameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        nicknameText.fontSize = 26;
        nicknameText.alignment = TextAnchor.MiddleLeft;
        nicknameText.color = Color.white;
        nicknameText.text = "nickname";
        RectTransform nicknameRect = nicknameGO.GetComponent<RectTransform>();
        nicknameRect.anchorMin = new Vector2(0f, 0.5f);
        nicknameRect.anchorMax = new Vector2(0f, 0.5f);
        nicknameRect.pivot = new Vector2(0f, 0.5f);
        nicknameRect.sizeDelta = new Vector2(200f, 50f);
        nicknameRect.anchoredPosition = new Vector2(120f, 0f);

        GameObject buttonGO = new GameObject("UserSettingsButton", typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(buttonGO, "Create UserSettingsButton");
        buttonGO.transform.SetParent(header.transform, false);
        Image buttonImage = buttonGO.GetComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.5f, 0.85f, 1f);
        settingsButton = buttonGO.GetComponent<Button>();
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(0f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(150f, 56f);
        buttonRect.anchoredPosition = new Vector2(260f, 0f);

        GameObject buttonTextGO = new GameObject("Text", typeof(Text));
        Undo.RegisterCreatedObjectUndo(buttonTextGO, "Create text for UserSettingsButton");
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        Text buttonText = buttonTextGO.GetComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 20;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.text = "профиль";
        RectTransform buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

    GameObject friendsGO = new GameObject("FriendsButton", typeof(Image), typeof(Button));
    Undo.RegisterCreatedObjectUndo(friendsGO, "Create FriendsButton");
    friendsGO.transform.SetParent(header.transform, false);
    Image friendsImage = friendsGO.GetComponent<Image>();
    friendsImage.color = new Color(0.2f, 0.6f, 0.35f, 1f);
    friendsButton = friendsGO.GetComponent<Button>();
    RectTransform friendsRect = friendsGO.GetComponent<RectTransform>();
    friendsRect.anchorMin = new Vector2(0f, 0.5f);
    friendsRect.anchorMax = new Vector2(0f, 0.5f);
    friendsRect.pivot = new Vector2(0.5f, 0.5f);
    friendsRect.sizeDelta = new Vector2(150f, 56f);
    friendsRect.anchoredPosition = new Vector2(410f, 0f);

    GameObject friendsTextGO = new GameObject("Text", typeof(Text));
    Undo.RegisterCreatedObjectUndo(friendsTextGO, "Create text for FriendsButton");
    friendsTextGO.transform.SetParent(friendsGO.transform, false);
    Text friendsText = friendsTextGO.GetComponent<Text>();
    friendsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    friendsText.fontSize = 20;
    friendsText.color = Color.white;
    friendsText.alignment = TextAnchor.MiddleCenter;
    friendsText.text = "друзья";
    RectTransform friendsTextRect = friendsTextGO.GetComponent<RectTransform>();
    friendsTextRect.anchorMin = Vector2.zero;
    friendsTextRect.anchorMax = Vector2.one;
    friendsTextRect.offsetMin = Vector2.zero;
    friendsTextRect.offsetMax = Vector2.zero;

    GameObject messagesGO = new GameObject("MessagesButton", typeof(Image), typeof(Button));
    Undo.RegisterCreatedObjectUndo(messagesGO, "Create MessagesButton");
    messagesGO.transform.SetParent(header.transform, false);
    Image messagesImage = messagesGO.GetComponent<Image>();
    messagesImage.color = new Color(0.35f, 0.5f, 0.8f, 1f);
    messagesButton = messagesGO.GetComponent<Button>();
    RectTransform messagesRect = messagesGO.GetComponent<RectTransform>();
    messagesRect.anchorMin = new Vector2(0f, 0.5f);
    messagesRect.anchorMax = new Vector2(0f, 0.5f);
    messagesRect.pivot = new Vector2(0.5f, 0.5f);
    messagesRect.sizeDelta = new Vector2(150f, 56f);
    messagesRect.anchoredPosition = new Vector2(560f, 0f);

    GameObject messagesTextGO = new GameObject("Text", typeof(Text));
    Undo.RegisterCreatedObjectUndo(messagesTextGO, "Create text for MessagesButton");
    messagesTextGO.transform.SetParent(messagesGO.transform, false);
    Text messagesText = messagesTextGO.GetComponent<Text>();
    messagesText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    messagesText.fontSize = 20;
    messagesText.color = Color.white;
    messagesText.alignment = TextAnchor.MiddleCenter;
    messagesText.text = "сообщения";
    RectTransform messagesTextRect = messagesTextGO.GetComponent<RectTransform>();
    messagesTextRect.anchorMin = Vector2.zero;
    messagesTextRect.anchorMax = Vector2.one;
    messagesTextRect.offsetMin = Vector2.zero;
    messagesTextRect.offsetMax = Vector2.zero;

        return header;
    }

    private static GameObject CreateUserSettingsPanel(Transform parent,
        out InputField nicknameInput,
        out Button changeAvatarButton,
        out Button resetAvatarButton,
        out Image avatarPreview,
        out Button saveButton,
        out Button cancelButton)
    {
        GameObject panel = new GameObject("UserSettingsPanel", typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create UserSettingsPanel");
        panel.transform.SetParent(parent, false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.08f, 0.92f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 30f);
        rect.sizeDelta = new Vector2(560f, 520f);

        GameObject contentGO = new GameObject("Content");
        Undo.RegisterCreatedObjectUndo(contentGO, "Create UserSettings Content");
        contentGO.transform.SetParent(panel.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(40f, 40f);
        contentRect.offsetMax = new Vector2(-40f, -40f);

        CreateText("UserSettingsTitle", contentGO.transform, "Профиль игрока", 26).color = Color.white;

        nicknameInput = CreateTextInputField("NicknameInput", contentGO.transform, "введите никнейм", "player");
        RectTransform nickRect = nicknameInput.GetComponent<RectTransform>();
        nickRect.anchorMin = new Vector2(0.5f, 1f);
        nickRect.anchorMax = new Vector2(0.5f, 1f);
        nickRect.pivot = new Vector2(0.5f, 1f);
        nickRect.anchoredPosition = new Vector2(0f, -90f);
        nickRect.sizeDelta = new Vector2(360f, 52f);

        GameObject avatarPreviewGO = new GameObject("AvatarPreview", typeof(Image));
        Undo.RegisterCreatedObjectUndo(avatarPreviewGO, "Create AvatarPreview");
        avatarPreviewGO.transform.SetParent(contentGO.transform, false);
        avatarPreview = avatarPreviewGO.GetComponent<Image>();
        avatarPreview.color = new Color(0.18f, 0.18f, 0.18f, 1f);
        RectTransform avatarPreviewRect = avatarPreviewGO.GetComponent<RectTransform>();
        avatarPreviewRect.anchorMin = new Vector2(0.5f, 0.5f);
        avatarPreviewRect.anchorMax = new Vector2(0.5f, 0.5f);
        avatarPreviewRect.pivot = new Vector2(0.5f, 0.5f);
        avatarPreviewRect.sizeDelta = new Vector2(160f, 160f);
        avatarPreviewRect.anchoredPosition = new Vector2(-110f, -20f);

        changeAvatarButton = CreateButton("ChangeAvatarButton", contentGO.transform, "сменить аватар");
        SetButtonLayout(changeAvatarButton, 220f, 52f);
        RectTransform changeRect = changeAvatarButton.GetComponent<RectTransform>();
        changeRect.anchorMin = new Vector2(0.5f, 0.5f);
        changeRect.anchorMax = new Vector2(0.5f, 0.5f);
        changeRect.pivot = new Vector2(0.5f, 0.5f);
        changeRect.anchoredPosition = new Vector2(130f, 30f);

        resetAvatarButton = CreateButton("ResetAvatarButton", contentGO.transform, "сбросить аватар");
        SetButtonLayout(resetAvatarButton, 220f, 52f);
        RectTransform resetRect = resetAvatarButton.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0.5f, 0.5f);
        resetRect.anchorMax = new Vector2(0.5f, 0.5f);
        resetRect.pivot = new Vector2(0.5f, 0.5f);
        resetRect.anchoredPosition = new Vector2(130f, -40f);

        saveButton = CreateButton("SaveUserSettingsButton", contentGO.transform, "сохранить");
        SetButtonLayout(saveButton, 200f, 54f);
        RectTransform saveRect = saveButton.GetComponent<RectTransform>();
        saveRect.anchorMin = new Vector2(0.5f, 0f);
        saveRect.anchorMax = new Vector2(0.5f, 0f);
        saveRect.pivot = new Vector2(0.5f, 0f);
        saveRect.anchoredPosition = new Vector2(-110f, 30f);

        cancelButton = CreateButton("CancelUserSettingsButton", contentGO.transform, "отмена");
        SetButtonLayout(cancelButton, 200f, 54f);
        RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0f);
        cancelRect.anchorMax = new Vector2(0.5f, 0f);
        cancelRect.pivot = new Vector2(0.5f, 0f);
        cancelRect.anchoredPosition = new Vector2(110f, 30f);

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateFriendsPanel(Transform parent, out FriendListController controller)
    {
        GameObject panel = new GameObject("FriendsPanel", typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create FriendsPanel");
        panel.transform.SetParent(parent, false);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.08f, 0.07f, 0.94f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(900f, 600f);

        GameObject titleGO = new GameObject("FriendsTitle", typeof(Text));
        Undo.RegisterCreatedObjectUndo(titleGO, "Create FriendsTitle");
        titleGO.transform.SetParent(panel.transform, false);
        Text titleText = titleGO.GetComponent<Text>();
        titleText.text = "Друзья";
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 32;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -30f);
        titleRect.sizeDelta = new Vector2(400f, 50f);

        InputField addInput = CreateTextInputField("AddFriendInput", panel.transform, "имя пользователя", "");
        RectTransform addInputRect = addInput.GetComponent<RectTransform>();
        addInputRect.anchorMin = new Vector2(0f, 1f);
        addInputRect.anchorMax = new Vector2(0f, 1f);
        addInputRect.pivot = new Vector2(0f, 1f);
        addInputRect.anchoredPosition = new Vector2(40f, -110f);
        addInputRect.sizeDelta = new Vector2(520f, 56f);

        Button addButton = CreateButton("AddFriendButton", panel.transform, "добавить");
        SetButtonLayout(addButton, 220f, 56f);
        RectTransform addButtonRect = addButton.GetComponent<RectTransform>();
        addButtonRect.anchorMin = new Vector2(1f, 1f);
        addButtonRect.anchorMax = new Vector2(1f, 1f);
        addButtonRect.pivot = new Vector2(1f, 1f);
        addButtonRect.anchoredPosition = new Vector2(-40f, -110f);

        Text statusText = CreateText("StatusText", panel.transform, string.Empty, 18);
        statusText.color = new Color(0.82f, 0.88f, 0.82f, 1f);
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(0.6f, 0f);
        statusRect.pivot = new Vector2(0f, 0f);
        statusRect.anchoredPosition = new Vector2(40f, 30f);
        statusRect.sizeDelta = new Vector2(420f, 30f);

        Button closeButton = CreateButton("CloseFriendsButton", panel.transform, "закрыть");
        SetButtonLayout(closeButton, 240f, 56f);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.anchoredPosition = new Vector2(-40f, 30f);

        GameObject listBackground = new GameObject("ListBackground", typeof(Image));
        Undo.RegisterCreatedObjectUndo(listBackground, "Create Friends List Background");
        listBackground.transform.SetParent(panel.transform, false);
        Image listImage = listBackground.GetComponent<Image>();
        listImage.color = new Color(0f, 0f, 0f, 0.35f);
        RectTransform listRect = listBackground.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 0f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.offsetMin = new Vector2(40f, 110f);
        listRect.offsetMax = new Vector2(-40f, -110f);

        GameObject scrollGO = new GameObject("ScrollView", typeof(Image), typeof(ScrollRect));
        Undo.RegisterCreatedObjectUndo(scrollGO, "Create Friends Scroll");
        scrollGO.transform.SetParent(listBackground.transform, false);
        Image scrollImage = scrollGO.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0.25f);
        RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(12f, 12f);
        scrollRect.offsetMax = new Vector2(-12f, -12f);

        GameObject viewport = new GameObject("Viewport", typeof(RectMask2D), typeof(Image));
        Undo.RegisterCreatedObjectUndo(viewport, "Create Friends Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.15f);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        GameObject itemsRootGO = new GameObject("Items", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(itemsRootGO, "Create Friends Items");
        itemsRootGO.transform.SetParent(viewport.transform, false);
        VerticalLayoutGroup itemsLayout = itemsRootGO.GetComponent<VerticalLayoutGroup>();
        itemsLayout.spacing = 10f;
        itemsLayout.padding = new RectOffset(12, 12, 12, 12);
        itemsLayout.childAlignment = TextAnchor.UpperLeft;
        itemsLayout.childControlHeight = true;
        itemsLayout.childControlWidth = true;
        ContentSizeFitter itemsFitter = itemsRootGO.GetComponent<ContentSizeFitter>();
        itemsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        itemsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        RectTransform itemsRect = itemsRootGO.GetComponent<RectTransform>();
        itemsRect.anchorMin = new Vector2(0f, 1f);
        itemsRect.anchorMax = new Vector2(1f, 1f);
        itemsRect.pivot = new Vector2(0.5f, 1f);
        itemsRect.sizeDelta = new Vector2(0f, 0f);

        ScrollRect scrollComponent = scrollGO.GetComponent<ScrollRect>();
        scrollComponent.viewport = viewportRect;
        scrollComponent.content = itemsRect;
        scrollComponent.horizontal = false;

        GameObject friendTemplate = new GameObject("FriendItemTemplate", typeof(Image));
        Undo.RegisterCreatedObjectUndo(friendTemplate, "Create Friend Template");
        friendTemplate.transform.SetParent(itemsRootGO.transform, false);
        Image templateImage = friendTemplate.GetComponent<Image>();
        templateImage.color = new Color(0.2f, 0.45f, 0.3f, 0.95f);
        RectTransform templateRect = friendTemplate.GetComponent<RectTransform>();
        templateRect.sizeDelta = new Vector2(0f, 64f);

        HorizontalLayoutGroup templateLayout = friendTemplate.AddComponent<HorizontalLayoutGroup>();
        templateLayout.padding = new RectOffset(16, 16, 10, 10);
        templateLayout.spacing = 12f;
        templateLayout.childAlignment = TextAnchor.MiddleLeft;
        templateLayout.childControlHeight = true;
        templateLayout.childControlWidth = false;

        Text friendName = CreateText("Name", friendTemplate.transform, "username", 22);
        friendName.color = Color.white;
        RectTransform nameRect = friendName.GetComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(260f, 40f);

        Button removeButton = CreateButton("RemoveButton", friendTemplate.transform, "удалить");
        SetButtonLayout(removeButton, 160f, 44f);

        friendTemplate.SetActive(false);

        controller = panel.AddComponent<FriendListController>();
        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("itemsRoot").objectReferenceValue = itemsRootGO.transform;
        controllerSO.FindProperty("itemTemplate").objectReferenceValue = friendTemplate;
        controllerSO.FindProperty("addFriendInput").objectReferenceValue = addInput;
        controllerSO.FindProperty("addFriendButton").objectReferenceValue = addButton;
        controllerSO.FindProperty("statusText").objectReferenceValue = statusText;
        controllerSO.FindProperty("closeButton").objectReferenceValue = closeButton;
        controllerSO.ApplyModifiedProperties();

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateFriendRequestsPanel(Transform parent, out FriendRequestCenterController controller)
    {
        GameObject panel = new GameObject("FriendRequestsPanel", typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create FriendRequestsPanel");
        panel.transform.SetParent(parent, false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.08f, 0.92f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(720f, 560f);

        GameObject contentGO = new GameObject("Content");
        Undo.RegisterCreatedObjectUndo(contentGO, "Create Friend Requests Content");
        contentGO.transform.SetParent(panel.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(40f, 40f);
        contentRect.offsetMax = new Vector2(-40f, -40f);

        Text title = CreateText("RequestsTitle", contentGO.transform, "Заявки в друзья", 26);
        title.color = Color.white;
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(480f, 40f);

        Text incomingLabel = CreateText("IncomingLabel", contentGO.transform, "Входящие заявки", 20);
        incomingLabel.color = Color.white;
        RectTransform incomingLabelRect = incomingLabel.GetComponent<RectTransform>();
        incomingLabelRect.anchorMin = new Vector2(0f, 1f);
        incomingLabelRect.anchorMax = new Vector2(0f, 1f);
        incomingLabelRect.pivot = new Vector2(0f, 1f);
        incomingLabelRect.anchoredPosition = new Vector2(0f, -80f);
        incomingLabelRect.sizeDelta = new Vector2(300f, 28f);

        Transform incomingRoot = CreateRequestsScroll(contentGO.transform, "IncomingScroll", -120f, 220f, out GameObject incomingTemplate, includeAcceptButtons: true);

        Text outgoingLabel = CreateText("OutgoingLabel", contentGO.transform, "Отправленные заявки", 20);
        outgoingLabel.color = Color.white;
        RectTransform outgoingLabelRect = outgoingLabel.GetComponent<RectTransform>();
        outgoingLabelRect.anchorMin = new Vector2(0f, 1f);
        outgoingLabelRect.anchorMax = new Vector2(0f, 1f);
        outgoingLabelRect.pivot = new Vector2(0f, 1f);
        outgoingLabelRect.anchoredPosition = new Vector2(0f, -340f);
        outgoingLabelRect.sizeDelta = new Vector2(300f, 28f);

        Transform outgoingRoot = CreateRequestsScroll(contentGO.transform, "OutgoingScroll", -360f, 220f, out GameObject outgoingTemplate, includeAcceptButtons: false);

        Text statusText = CreateText("StatusText", contentGO.transform, "", 18);
        statusText.color = new Color(0.8f, 0.85f, 0.8f, 1f);
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(0.6f, 0f);
        statusRect.pivot = new Vector2(0f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, -10f);
        statusRect.sizeDelta = new Vector2(400f, 24f);

        Button closeButton = CreateButton("CloseRequestsButton", contentGO.transform, "закрыть");
        SetButtonLayout(closeButton, 240f, 56f);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, -10f);

        controller = panel.AddComponent<FriendRequestCenterController>();
        SerializedObject ctrlSO = new SerializedObject(controller);
        ctrlSO.FindProperty("incomingRoot").objectReferenceValue = incomingRoot;
        ctrlSO.FindProperty("outgoingRoot").objectReferenceValue = outgoingRoot;
        ctrlSO.FindProperty("incomingTemplate").objectReferenceValue = incomingTemplate;
        ctrlSO.FindProperty("outgoingTemplate").objectReferenceValue = outgoingTemplate;
        ctrlSO.FindProperty("statusText").objectReferenceValue = statusText;
        ctrlSO.FindProperty("closeButton").objectReferenceValue = closeButton;
        ctrlSO.ApplyModifiedProperties();

        panel.SetActive(false);
        return panel;
    }

    private static Transform CreateRequestsScroll(Transform parent, string name, float topOffset, float height, out GameObject itemTemplate, bool includeAcceptButtons)
    {
        GameObject scrollGO = new GameObject(name, typeof(Image), typeof(ScrollRect));
        Undo.RegisterCreatedObjectUndo(scrollGO, "Create " + name);
        scrollGO.transform.SetParent(parent, false);
        Image scrollImage = scrollGO.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0.25f);
        RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 1f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.pivot = new Vector2(0f, 1f);
        scrollRect.anchoredPosition = new Vector2(0f, topOffset);
        scrollRect.sizeDelta = new Vector2(0f, height);

        GameObject viewport = new GameObject("Viewport", typeof(RectMask2D), typeof(Image));
        Undo.RegisterCreatedObjectUndo(viewport, "Create viewport for " + name);
        viewport.transform.SetParent(scrollGO.transform, false);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.15f);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        GameObject content = new GameObject("Items", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(content, "Create request content for " + name);
        content.transform.SetParent(viewport.transform, false);
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        ScrollRect scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;

        itemTemplate = new GameObject(includeAcceptButtons ? "IncomingItemTemplate" : "OutgoingItemTemplate", typeof(Image));
        Undo.RegisterCreatedObjectUndo(itemTemplate, "Create request item template");
        itemTemplate.transform.SetParent(content.transform, false);
        Image templateImage = itemTemplate.GetComponent<Image>();
        templateImage.color = includeAcceptButtons ? new Color(0.25f, 0.35f, 0.55f, 0.9f) : new Color(0.2f, 0.5f, 0.3f, 0.9f);
        RectTransform templateRect = itemTemplate.GetComponent<RectTransform>();
        templateRect.sizeDelta = new Vector2(0f, 60f);

        HorizontalLayoutGroup templateLayout = itemTemplate.AddComponent<HorizontalLayoutGroup>();
        templateLayout.padding = new RectOffset(12, 12, 8, 8);
        templateLayout.spacing = 12f;
        templateLayout.childAlignment = TextAnchor.MiddleLeft;
        templateLayout.childControlHeight = true;
        templateLayout.childControlWidth = false;

        Text nameLabel = CreateText("Name", itemTemplate.transform, "username", 20);
        nameLabel.color = Color.white;
        RectTransform nameRect = nameLabel.GetComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(220f, 36f);

        if (includeAcceptButtons)
        {
            Button acceptButton = CreateButton("AcceptButton", itemTemplate.transform, "принять");
            SetButtonLayout(acceptButton, 140f, 40f);

            Button declineButton = CreateButton("DeclineButton", itemTemplate.transform, "отклонить");
            SetButtonLayout(declineButton, 140f, 40f);
        }
        else
        {
            Text infoLabel = CreateText("Info", itemTemplate.transform, "отправлено 00:00", 16);
            infoLabel.color = new Color(0.85f, 0.9f, 0.85f, 1f);
            RectTransform infoRect = infoLabel.GetComponent<RectTransform>();
            infoRect.sizeDelta = new Vector2(240f, 32f);

            Button cancelButton = CreateButton("CancelButton", itemTemplate.transform, "отменить");
            SetButtonLayout(cancelButton, 140f, 40f);
        }

        itemTemplate.SetActive(false);

        return content.transform;
    }

    private static GameObject CreateSettingsPanel(Transform parent,
        out Slider volumeSlider,
        out Slider brightnessSlider,
        out Dropdown languageDropdown,
        out Dropdown cardThemeDropdown,
        out Image cardPreview,
        out Button applyButton,
        out Button closeButton)
    {
        GameObject panel = new GameObject("SettingsPanel", typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create SettingsPanel");
        panel.transform.SetParent(parent, false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(10f, 33f);
        rect.sizeDelta = new Vector2(766.627f, 566.452f);

        GameObject contentGO = new GameObject("Content");
        Undo.RegisterCreatedObjectUndo(contentGO, "Create Settings Content");
        contentGO.transform.SetParent(panel.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(30f, 30f);
        contentRect.offsetMax = new Vector2(-30f, -30f);

        Text title = CreateText("SettingsTitle", contentGO.transform, "Настройки", 26);
        title.color = Color.white;
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(360f, 40f);

        volumeSlider = CreateSliderControl("MasterVolumeSlider", contentGO.transform, "громкость");
        RectTransform volumeRect = volumeSlider.transform.parent.GetComponent<RectTransform>();
        volumeRect.anchoredPosition = new Vector2(0f, -80f);

        brightnessSlider = CreateSliderControl("BrightnessSlider", contentGO.transform, "яркость");
        RectTransform brightnessRect = brightnessSlider.transform.parent.GetComponent<RectTransform>();
        brightnessRect.anchoredPosition = new Vector2(0f, -175f);

        languageDropdown = CreateDropdownContainer("LanguageDropdown", contentGO.transform, "язык");
        RectTransform languageRect = languageDropdown.transform.parent.GetComponent<RectTransform>();
        languageRect.anchoredPosition = new Vector2(-120f, -270f);

        cardThemeDropdown = CreateDropdownContainer("CardThemeDropdown", contentGO.transform, "оформление карт");
        RectTransform themeRect = cardThemeDropdown.transform.parent.GetComponent<RectTransform>();
        themeRect.anchoredPosition = new Vector2(-120f, -370f);

        cardPreview = CreatePreviewImage("CardThemePreview", contentGO.transform, new Vector2(180f, 220f));
        RectTransform previewRect = cardPreview.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 1f);
        previewRect.anchorMax = new Vector2(0.5f, 1f);
        previewRect.pivot = new Vector2(0.5f, 1f);
        previewRect.anchoredPosition = new Vector2(190f, -305f);

        applyButton = CreateButton("ApplySettingsButton", contentGO.transform, "применить");
        SetButtonLayout(applyButton, 240f, 56f);
        RectTransform applyRect = applyButton.GetComponent<RectTransform>();
        applyRect.anchorMin = new Vector2(0.5f, 0f);
        applyRect.anchorMax = new Vector2(0.5f, 0f);
        applyRect.pivot = new Vector2(0.5f, 0f);
        applyRect.anchoredPosition = new Vector2(0f, 120f);

        closeButton = CreateButton("CloseSettingsButton", contentGO.transform, "закрыть");
        SetButtonLayout(closeButton, 240f, 56f);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 50f);

        panel.SetActive(false);
        return panel;
    }

    private static Image CreateBrightnessOverlay(Transform canvasTransform)
    {
        GameObject overlay = new GameObject("BrightnessOverlay", typeof(Image));
        Undo.RegisterCreatedObjectUndo(overlay, "Create BrightnessOverlay");
        overlay.transform.SetParent(canvasTransform, false);
        overlay.transform.SetAsLastSibling();

        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlay.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

        return image;
    }

    private static void SetLocalizationEntry(SerializedProperty entriesProp, int index, string key, string russian, string english)
    {
        SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(index);
        entryProp.FindPropertyRelative("key").stringValue = key;
        entryProp.FindPropertyRelative("russian").stringValue = russian;
        entryProp.FindPropertyRelative("english").stringValue = english;
    }
}
