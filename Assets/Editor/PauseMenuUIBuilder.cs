using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUIBuilder : EditorWindow
{
    [MenuItem("Tools/Poker/Rebuild Pause Menu")] 
    public static void BuildPauseMenu()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Pause Menu Builder", "Не найден Canvas в текущей сцене.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();

        // Чистим старые объекты
        DestroyIfExists(canvas.transform, "PauseUI");
        DestroyIfExists(canvas.transform, "PausePanel");
        DestroyIfExists(canvas.transform, "PauseButton");
        DestroyIfExists(canvas.transform, "BrightnessOverlay");

        GameObject root = new GameObject("PauseUI");
        Undo.RegisterCreatedObjectUndo(root, "Create PauseUI root");
        root.transform.SetParent(canvas.transform, false);

        PauseMenuController controller = root.GetComponent<PauseMenuController>();
        if (controller == null)
        {
            controller = root.AddComponent<PauseMenuController>();
        }

        // Кнопка паузы
        Button pauseButton = CreatePauseButton(root.transform);

        // Панель паузы и настроек
        GameObject pausePanel = CreatePausePanel(root.transform,
            out GameObject pauseContent,
            out Button resumeButton,
            out Button settingsButton,
            out Button mainMenuButton,
            out Button exitButton,
            out GameObject settingsPanel,
            out Text settingsTitle,
            out Slider volumeSlider,
            out Text volumeLabel,
            out Slider brightnessSlider,
            out Text brightnessLabel,
            out Button backButton);

        // Яркостный оверлей
        Image brightnessOverlay = CreateBrightnessOverlay(canvas.transform, root.transform);
        root.transform.SetSiblingIndex(brightnessOverlay.transform.GetSiblingIndex() + 1);

        // Настройки локализации
        AutoLocalizationByName localization = root.GetComponent<AutoLocalizationByName>();
        if (localization == null)
        {
            localization = root.AddComponent<AutoLocalizationByName>();
        }

        SerializedObject locSO = new SerializedObject(localization);
        SerializedProperty entriesProp = locSO.FindProperty("entries");
        entriesProp.arraySize = 8;
        SetLocalizationEntry(entriesProp, 0, "ResumeButton", "продолжить", "Resume");
        SetLocalizationEntry(entriesProp, 1, "SettingsButton", "настройки", "Settings");
        SetLocalizationEntry(entriesProp, 2, "MainMenuButton", "главное меню", "Main menu");
        SetLocalizationEntry(entriesProp, 3, "ExitButton", "выход", "Exit");
        SetLocalizationEntry(entriesProp, 4, settingsTitle.gameObject.name, "Настройки", "Settings");
        SetLocalizationEntry(entriesProp, 5, volumeLabel.gameObject.name, "громкость", "Volume");
        SetLocalizationEntry(entriesProp, 6, brightnessLabel.gameObject.name, "яркость", "Brightness");
        SetLocalizationEntry(entriesProp, 7, backButton.gameObject.name, "назад", "Back");
        locSO.ApplyModifiedProperties();

        // Присваиваем ссылки контроллеру
        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        controllerSO.FindProperty("pauseContent").objectReferenceValue = pauseContent;
        controllerSO.FindProperty("resumeButton").objectReferenceValue = resumeButton;
        controllerSO.FindProperty("settingsButton").objectReferenceValue = settingsButton;
        controllerSO.FindProperty("mainMenuButton").objectReferenceValue = mainMenuButton;
        controllerSO.FindProperty("exitButton").objectReferenceValue = exitButton;
        controllerSO.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        controllerSO.FindProperty("masterVolumeSlider").objectReferenceValue = volumeSlider;
        controllerSO.FindProperty("backFromSettingsButton").objectReferenceValue = backButton;
        controllerSO.FindProperty("brightnessOverlay").objectReferenceValue = brightnessOverlay;
        controllerSO.FindProperty("brightnessSlider").objectReferenceValue = brightnessSlider;
        controllerSO.ApplyModifiedProperties();

        // Настройки кнопок и состояния
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);

        // Навешиваем вызов паузы на кнопку
        pauseButton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(pauseButton.onClick, controller.Pause);

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(controller);
        EditorUtility.DisplayDialog("Pause Menu Builder", "Пауза-меню успешно перестроено.", "OK");
    }

    private static void DestroyIfExists(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static Button CreatePauseButton(Transform parent)
    {
        GameObject go = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create PauseButton");
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-40f, -40f);
        rect.sizeDelta = new Vector2(70f, 70f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.18f, 0.45f, 0.18f, 0.95f);

        Button button = go.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        Text txt = textGO.GetComponent<Text>();
        txt.text = "II";
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 32;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = txt.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static GameObject CreatePausePanel(Transform parent,
        out GameObject content,
        out Button resumeButton,
        out Button settingsButton,
        out Button mainMenuButton,
        out Button exitButton,
        out GameObject settingsPanel,
        out Text settingsTitle,
        out Slider volumeSlider,
        out Text volumeLabel,
        out Slider brightnessSlider,
        out Text brightnessLabel,
        out Button backButton)
    {
        GameObject panel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create PausePanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.7f);

        content = new GameObject("PauseContent", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(content, "Create PauseContent");
        content.transform.SetParent(panel.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(520f, 460f);
        contentRect.anchoredPosition = Vector2.zero;

        Image contentImage = content.GetComponent<Image>();
        contentImage.color = new Color(1f, 1f, 1f, 0.95f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15f;
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.childAlignment = TextAnchor.MiddleCenter;

        ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        resumeButton = CreateMenuButton("ResumeButton", content.transform, "Resume");
        settingsButton = CreateMenuButton("SettingsButton", content.transform, "Settings");
        mainMenuButton = CreateMenuButton("MainMenuButton", content.transform, "Main menu");
        exitButton = CreateMenuButton("ExitButton", content.transform, "Exit");

        settingsPanel = new GameObject("PauseSettingsPanel", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(settingsPanel, "Create PauseSettingsPanel");
        settingsPanel.transform.SetParent(panel.transform, false);
        RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>();
        settingsRect.anchorMin = new Vector2(0.5f, 0.5f);
        settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
        settingsRect.pivot = new Vector2(0.5f, 0.5f);
        settingsRect.sizeDelta = new Vector2(520f, 420f);
        settingsRect.anchoredPosition = Vector2.zero;

        Image settingsImage = settingsPanel.GetComponent<Image>();
        settingsImage.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);

        GameObject settingsContent = new GameObject("SettingsContent", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(settingsContent, "Create PauseSettingsContent");
        settingsContent.transform.SetParent(settingsPanel.transform, false);
        RectTransform settingsContentRect = settingsContent.GetComponent<RectTransform>();
        settingsContentRect.anchorMin = Vector2.zero;
        settingsContentRect.anchorMax = Vector2.one;
        settingsContentRect.offsetMin = new Vector2(30f, 30f);
        settingsContentRect.offsetMax = new Vector2(-30f, -30f);

        VerticalLayoutGroup settingsLayout = settingsContent.AddComponent<VerticalLayoutGroup>();
        settingsLayout.spacing = 24f;
        settingsLayout.childAlignment = TextAnchor.UpperCenter;
        settingsLayout.childControlHeight = false;
        settingsLayout.childControlWidth = true;
        settingsLayout.childForceExpandHeight = false;
        settingsLayout.childForceExpandWidth = true;

        ContentSizeFitter settingsFitter = settingsContent.AddComponent<ContentSizeFitter>();
        settingsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        settingsTitle = CreateTextElement("SettingsTitle", settingsContent.transform, "Settings", 30, Color.white, TextAnchor.MiddleCenter);
        volumeLabel = CreateTextElement("PauseVolumeLabel", settingsContent.transform, "Volume", 22, Color.white, TextAnchor.MiddleLeft);
        volumeSlider = CreateSlider(settingsContent.transform, "PauseVolumeSlider", Color.green);
        brightnessLabel = CreateTextElement("PauseBrightnessLabel", settingsContent.transform, "Brightness", 22, Color.white, TextAnchor.MiddleLeft);
        brightnessSlider = CreateSlider(settingsContent.transform, "PauseBrightnessSlider", new Color(0.2f, 0.6f, 0.2f, 1f));
        brightnessSlider.value = 1f;

        backButton = CreateMenuButton("BackFromSettingsButton", settingsContent.transform, "Back");

        settingsPanel.SetActive(false);

        return panel;
    }

    private static Image CreateBrightnessOverlay(Transform canvasTransform, Transform pauseRoot)
    {
        GameObject overlayGO = new GameObject("BrightnessOverlay", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(overlayGO, "Create BrightnessOverlay");
        overlayGO.transform.SetParent(canvasTransform, false);

        int targetIndex = pauseRoot.GetSiblingIndex();
        overlayGO.transform.SetSiblingIndex(Mathf.Max(0, targetIndex));

        RectTransform rect = overlayGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlayGO.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

        return image;
    }

    private static Button CreateMenuButton(string name, Transform parent, string text)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(buttonGO, "Create " + name);
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 60f);

        Image img = buttonGO.GetComponent<Image>();
        img.color = new Color(0.18f, 0.55f, 0.18f, 1f);

        Button button = buttonGO.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.55f, 0.18f, 1f);
        colors.highlightedColor = new Color(0.24f, 0.65f, 0.24f, 1f);
        colors.pressedColor = new Color(0.14f, 0.45f, 0.14f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        GameObject textGO = new GameObject("Text", typeof(Text));
        textGO.transform.SetParent(buttonGO.transform, false);
        Text txt = textGO.GetComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 26;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform textRect = txt.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static Text CreateTextElement(string name, Transform parent, string text, int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        GameObject textGO = new GameObject(name, typeof(Text));
        Undo.RegisterCreatedObjectUndo(textGO, "Create " + name);
        textGO.transform.SetParent(parent, false);

        Text txt = textGO.GetComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = anchor;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform rect = txt.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 42f);

        LayoutElement layout = textGO.AddComponent<LayoutElement>();
        layout.preferredHeight = 42f;
        layout.minHeight = 42f;

        return txt;
    }

    private static Slider CreateSlider(Transform parent, string name, Color fillColor)
    {
        GameObject sliderGO = new GameObject(name, typeof(RectTransform), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(sliderGO, "Create Slider");
        sliderGO.transform.SetParent(parent, false);

        RectTransform rect = sliderGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400f, 40f);

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        LayoutElement sliderLayout = sliderGO.AddComponent<LayoutElement>();
        sliderLayout.preferredWidth = 240f;
        sliderLayout.preferredHeight = 36f;
        sliderLayout.minHeight = 36f;

        GameObject backgroundGO = new GameObject("Background", typeof(Image));
        backgroundGO.transform.SetParent(sliderGO.transform, false);
        RectTransform backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.35f);
        backgroundRect.anchorMax = new Vector2(1f, 0.65f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = backgroundGO.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.3f);

        GameObject fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.35f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.65f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject fillGO = new GameObject("Fill", typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillGO.GetComponent<Image>();
        fillImage.color = fillColor;

        GameObject handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handleGO = new GameObject("Handle", typeof(Image));
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(24f, 24f);
        Image handleImage = handleGO.GetComponent<Image>();
        handleImage.color = Color.white;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private static void SetLocalizationEntry(SerializedProperty entriesProp, int index, string key, string russian, string english)
    {
        SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(index);
        entryProp.FindPropertyRelative("key").stringValue = key;
        entryProp.FindPropertyRelative("russian").stringValue = russian;
        entryProp.FindPropertyRelative("english").stringValue = english;
    }
}
