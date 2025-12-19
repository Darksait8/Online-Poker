using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Редакторский инструмент для создания панели калькулятора вероятностей
/// </summary>
public class ProbabilityCalculatorPanelBuilder : EditorWindow
{
    [MenuItem("Tools/Poker/Create Probability Calculator Panel")]
    public static void ShowWindow()
    {
        GetWindow<ProbabilityCalculatorPanelBuilder>("Probability Calculator Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Создание панели калькулятора вероятностей", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Создать панель в текущей сцене", GUILayout.Height(30)))
        {
            CreateProbabilityPanel();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Создаст панель калькулятора вероятностей покерных комбинаций в текущей сцене.\n" +
            "Панель можно открыть/закрыть клавишей P или кнопкой.",
            MessageType.Info);
    }

    private static void CreateProbabilityPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Не найден Canvas в текущей сцене.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();

        // Удаляем старую панель, если есть
        ProbabilityCalculatorPanel existingPanel = FindObjectOfType<ProbabilityCalculatorPanel>();
        if (existingPanel != null)
        {
            Undo.DestroyObjectImmediate(existingPanel.gameObject);
        }

        // Создаем корневую панель
        GameObject panelRoot = new GameObject("ProbabilityCalculatorPanel", typeof(RectTransform), typeof(Image), typeof(ProbabilityCalculatorPanel));
        Undo.RegisterCreatedObjectUndo(panelRoot, "Create ProbabilityCalculatorPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(800, 600);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelBg = panelRoot.GetComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        ProbabilityCalculatorPanel controller = panelRoot.GetComponent<ProbabilityCalculatorPanel>();

        // Заголовок
        GameObject titleObj = CreateTextElement(panelRoot.transform, "Калькулятор вероятностей", 
            new Vector2(0, 250), new Vector2(700, 40), 24, TextAlignmentOptions.Center, FontStyles.Bold);
        titleObj.name = "Title";

        // Кнопка закрытия
        GameObject closeButton = CreateButton(panelRoot.transform, "✕", 
            new Vector2(350, 250), new Vector2(40, 40), () => { });
        closeButton.name = "CloseButton";

        // Контейнер для списка комбинаций (ScrollView)
        GameObject scrollView = CreateScrollView(panelRoot.transform, 
            new Vector2(0, -50), new Vector2(750, 450));
        scrollView.name = "CombinationsScrollView";

        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        RectTransform contentRect = scrollRect.content;

        // Настраиваем контроллер через SerializedObject
        SerializedObject controllerSO = new SerializedObject(controller);
        
        // Устанавливаем ссылку на панель
        var panelProp = controllerSO.FindProperty("panel");
        if (panelProp != null)
        {
            panelProp.objectReferenceValue = panelRoot;
        }
        
        // Устанавливаем кнопку закрытия
        var closeButtonProp = controllerSO.FindProperty("closeButton");
        if (closeButtonProp != null)
        {
            closeButtonProp.objectReferenceValue = closeButton.GetComponent<Button>();
        }
        
        // Устанавливаем заголовок
        var titleTextProp = controllerSO.FindProperty("titleText");
        if (titleTextProp != null)
        {
            titleTextProp.objectReferenceValue = titleObj.GetComponent<TextMeshProUGUI>();
        }
        
        // Устанавливаем контейнер для комбинаций
        var containerProp = controllerSO.FindProperty("combinationsContainer");
        if (containerProp != null)
        {
            containerProp.objectReferenceValue = contentRect;
        }
        
        // Устанавливаем startVisible
        var startVisibleProp = controllerSO.FindProperty("startVisible");
        if (startVisibleProp != null)
        {
            startVisibleProp.boolValue = false;
        }
        
        controllerSO.ApplyModifiedProperties();

        // Кнопка для открытия панели (вне панели, на Canvas)
        GameObject toggleButton = CreateButton(canvas.transform, "Калькулятор вероятностей", 
            new Vector2(-400, 400), new Vector2(200, 50), () => { });
        toggleButton.name = "ToggleProbabilityButton";

        // Устанавливаем кнопку переключения
        SerializedObject controllerSO2 = new SerializedObject(controller);
        var toggleButtonProp = controllerSO2.FindProperty("toggleButton");
        if (toggleButtonProp != null)
        {
            toggleButtonProp.objectReferenceValue = toggleButton.GetComponent<Button>();
        }
        controllerSO2.ApplyModifiedProperties();
        
        // Принудительно обновляем ссылки через публичные свойства (на случай проблем с SerializedObject)
        if (Application.isPlaying)
        {
            controller.Panel = panelRoot;
            controller.CloseButton = closeButton.GetComponent<Button>();
            controller.TitleText = titleObj.GetComponent<TextMeshProUGUI>();
            controller.CombinationsContainer = contentRect;
            controller.ToggleButton = toggleButton.GetComponent<Button>();
        }

        // Скрываем панель по умолчанию
        panelRoot.SetActive(false);

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        EditorUtility.DisplayDialog("Успех", "Панель калькулятора вероятностей создана!", "OK");
    }

    private static GameObject CreateTextElement(Transform parent, string text, Vector2 position, Vector2 size, 
        float fontSize, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.color = Color.white;

        return textObj;
    }

    private static GameObject CreateButton(Transform parent, string text, Vector2 position, Vector2 size, System.Action onClick)
    {
        GameObject buttonObj = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image buttonImage = buttonObj.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.4f, 0.6f, 1f);

        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(() => onClick());

        // Текст кнопки
        GameObject textObj = CreateTextElement(buttonObj.transform, text, Vector2.zero, size, 16, TextAlignmentOptions.Center);
        textObj.name = "Text";

        return buttonObj;
    }

    private static GameObject CreateScrollView(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
        scrollView.transform.SetParent(parent, false);

        RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.sizeDelta = size;
        scrollRect.anchoredPosition = position;

        Image scrollBg = scrollView.GetComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.05f, 0.1f, 0.8f);

        Mask mask = scrollView.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(scrollView.transform, false);

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        contentRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 2;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect настройка
        ScrollRect scrollRectComponent = scrollView.GetComponent<ScrollRect>();
        scrollRectComponent.content = contentRect;
        scrollRectComponent.horizontal = false;
        scrollRectComponent.vertical = true;
        scrollRectComponent.verticalScrollbar = null; // Можно добавить скроллбар позже
        scrollRectComponent.viewport = scrollRect; // Viewport - это сам RectTransform ScrollView

        return scrollView;
    }
}

