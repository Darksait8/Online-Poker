using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanelUIBuilder : EditorWindow
{
    [MenuItem("Tools/Poker/Rebuild Action Panel")] 
    public static void BuildActionPanel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Action Panel Builder", "Не найден Canvas в текущей сцене.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();

        foreach (var existingController in Object.FindObjectsOfType<ActionPanelController>())
        {
            Undo.DestroyObjectImmediate(existingController.gameObject);
        }

        GameObject panel = new GameObject("ActionPanel", typeof(RectTransform), typeof(Image), typeof(ActionPanelController));
        Undo.RegisterCreatedObjectUndo(panel, "Create ActionPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(960f, 220f);
        rect.anchoredPosition = new Vector2(0f, 40f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.15f, 0.05f, 0.92f);

        GameObject buttonsRow = new GameObject("ActionRow", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(buttonsRow, "Create ActionRow");
        buttonsRow.transform.SetParent(panel.transform, false);

        RectTransform rowRect = buttonsRow.GetComponent<RectTransform>() ?? buttonsRow.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 0.5f);
        rowRect.anchorMax = new Vector2(1f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(0f, 140f);
        rowRect.anchoredPosition = Vector2.zero;

        Button foldButton = CreateActionButton("FoldButton", buttonsRow.transform, "Fold", new Color(0.75f, 0.2f, 0.2f, 1f));
        Button checkCallButton = CreateActionButton("CheckCallButton", buttonsRow.transform, "Check / Call", new Color(0.25f, 0.45f, 0.8f, 1f));

        GameObject betGroup = new GameObject("BetControls", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(betGroup, "Create BetControls");
        betGroup.transform.SetParent(buttonsRow.transform, false);

        VerticalLayoutGroup betLayout = betGroup.AddComponent<VerticalLayoutGroup>();
        betLayout.spacing = 6f;
        betLayout.childAlignment = TextAnchor.MiddleCenter;
        betLayout.childForceExpandHeight = false;
        betLayout.childForceExpandWidth = false;
        betLayout.childControlHeight = false;
        betLayout.childControlWidth = false;

        Text betLabel = CreateTextElement("BetValueLabel", betGroup.transform, "Ставка", 22, Color.white);
        Slider betSlider = CreateBetSlider(betGroup.transform);
        Text betValue = CreateTextElement("BetValueText", betGroup.transform, "0", 24, Color.yellow);
        RectTransform betValueRect = betValue.GetComponent<RectTransform>();
        betValueRect.sizeDelta = new Vector2(160f, 36f);

        Button betRaiseButton = CreateActionButton("BetRaiseButton", buttonsRow.transform, "Bet / Raise", new Color(0.25f, 0.65f, 0.3f, 1f));

        // Расставляем элементы вручную, чтобы пользователь мог легко их подвинуть позже
        PositionActionElement(foldButton.GetComponent<RectTransform>(), new Vector2(-360f, 0f));
        PositionActionElement(checkCallButton.GetComponent<RectTransform>(), new Vector2(-120f, 0f));
        PositionActionElement(betGroup.GetComponent<RectTransform>(), new Vector2(120f, 0f));
        PositionActionElement(betRaiseButton.GetComponent<RectTransform>(), new Vector2(360f, 0f));

        // Подключаем контроллер
        ActionPanelController controller = panel.GetComponent<ActionPanelController>();
        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("foldButton").objectReferenceValue = foldButton;
        controllerSO.FindProperty("checkCallButton").objectReferenceValue = checkCallButton;
        controllerSO.FindProperty("betRaiseButton").objectReferenceValue = betRaiseButton;
        controllerSO.FindProperty("betSlider").objectReferenceValue = betSlider;
        controllerSO.FindProperty("betValueText").objectReferenceValue = betValue;
        controllerSO.ApplyModifiedProperties();

        controller.SetupSlider(100, 1000, 50);

        // Локализация
        AutoLocalizationByName localization = panel.GetComponent<AutoLocalizationByName>();
        if (localization == null)
        {
            localization = panel.AddComponent<AutoLocalizationByName>();
        }

        SerializedObject locSO = new SerializedObject(localization);
        SerializedProperty entriesProp = locSO.FindProperty("entries");
        entriesProp.arraySize = 4;
        SetLocalizationEntry(entriesProp, 0, "FoldButton", "пас", "Fold");
        SetLocalizationEntry(entriesProp, 1, "CheckCallButton", "чек / колл", "Check / Call");
        SetLocalizationEntry(entriesProp, 2, "BetRaiseButton", "бет / рейз", "Bet / Raise");
        SetLocalizationEntry(entriesProp, 3, "BetValueLabel", "ставка", "Bet");
        locSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(controller);
        EditorUtility.DisplayDialog("Action Panel Builder", "Панель действий перестроена.", "OK");
    }

    private static Button CreateActionButton(string name, Transform parent, string text, Color color)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(buttonGO, "Create " + name);
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(220f, 70f);

        Image img = buttonGO.GetComponent<Image>();
        img.color = color;

        Button button = buttonGO.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(Text));
        textGO.transform.SetParent(buttonGO.transform, false);
        Text txt = textGO.GetComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 22;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = txt.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static Text CreateTextElement(string name, Transform parent, string text, int fontSize, Color color)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
        Undo.RegisterCreatedObjectUndo(textGO, "Create " + name);
        textGO.transform.SetParent(parent, false);

        Text txt = textGO.GetComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140f, 40f);

        return txt;
    }

    private static Slider CreateBetSlider(Transform parent)
    {
        GameObject sliderGO = new GameObject("BetSlider", typeof(RectTransform), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(sliderGO, "Create BetSlider");
        sliderGO.transform.SetParent(parent, false);

        RectTransform rect = sliderGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360f, 36f);

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = true;

        GameObject backgroundGO = new GameObject("Background", typeof(Image));
        backgroundGO.transform.SetParent(sliderGO.transform, false);
        RectTransform backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.35f);
        backgroundRect.anchorMax = new Vector2(1f, 0.65f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = backgroundGO.GetComponent<Image>();
        backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        backgroundImage.color = new Color(0f, 0f, 0f, 0.4f);

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
        fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fillImage.color = new Color(0.25f, 0.7f, 0.3f, 1f);

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
        handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
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

    private static void PositionActionElement(RectTransform rect, Vector2 anchoredPosition)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
    }
}
