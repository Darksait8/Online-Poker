using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Вспомогательный скрипт для автоматического создания панели калькулятора вероятностей, если её нет
/// </summary>
public class ProbabilityCalculatorHelper : MonoBehaviour
{
    [Header("Автоматическое создание")]
    [SerializeField] private bool autoCreateIfMissing = true;
    [SerializeField] private bool createOnStart = true;

    private void Start()
    {
        if (createOnStart && autoCreateIfMissing)
        {
            CheckAndCreatePanel();
        }
    }

    /// <summary>
    /// Проверяет наличие панели и создает её, если нужно
    /// </summary>
    [ContextMenu("Проверить и создать панель")]
    public void CheckAndCreatePanel()
    {
        ProbabilityCalculatorPanel existingPanel = FindObjectOfType<ProbabilityCalculatorPanel>();
        
        if (existingPanel != null)
        {
            Debug.Log("ProbabilityCalculatorHelper: Панель уже существует в сцене");
            
            // Проверяем, что все ссылки установлены
            if (existingPanel.Panel == null)
            {
                existingPanel.Panel = existingPanel.gameObject;
                Debug.Log("ProbabilityCalculatorHelper: Установлена ссылка на panel");
            }
            
            return;
        }

        Debug.Log("ProbabilityCalculatorHelper: Панель не найдена, создаю...");
        CreatePanel();
    }

    private void CreatePanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("ProbabilityCalculatorHelper: Canvas не найден в сцене!");
            return;
        }

        // Создаем корневую панель
        GameObject panelRoot = new GameObject("ProbabilityCalculatorPanel", typeof(RectTransform), typeof(Image), typeof(ProbabilityCalculatorPanel));
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
        controller.Panel = panelRoot;

        // Заголовок
        GameObject titleObj = CreateTextElement(panelRoot.transform, "Калькулятор вероятностей", 
            new Vector2(0, 250), new Vector2(700, 40), 24, TextAlignmentOptions.Center, FontStyles.Bold);
        titleObj.name = "Title";
        controller.TitleText = titleObj.GetComponent<TextMeshProUGUI>();

        // Кнопка закрытия
        GameObject closeButton = CreateButton(panelRoot.transform, "✕", 
            new Vector2(350, 250), new Vector2(40, 40));
        closeButton.name = "CloseButton";
        controller.CloseButton = closeButton.GetComponent<Button>();

        // Контейнер для списка комбинаций (ScrollView)
        GameObject scrollView = CreateScrollView(panelRoot.transform, 
            new Vector2(0, -50), new Vector2(750, 450));
        scrollView.name = "CombinationsScrollView";

        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        RectTransform contentRect = scrollRect.content;
        controller.CombinationsContainer = contentRect;

        // Кнопка для открытия панели (вне панели, на Canvas)
        GameObject toggleButton = CreateButton(canvas.transform, "Калькулятор вероятностей", 
            new Vector2(-400, 400), new Vector2(200, 50));
        toggleButton.name = "ToggleProbabilityButton";
        controller.ToggleButton = toggleButton.GetComponent<Button>();

        // Скрываем панель по умолчанию
        panelRoot.SetActive(false);

        Debug.Log("ProbabilityCalculatorHelper: Панель успешно создана!");
    }

    private GameObject CreateTextElement(Transform parent, string text, Vector2 position, Vector2 size, 
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

    private GameObject CreateButton(Transform parent, string text, Vector2 position, Vector2 size)
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

        // Текст кнопки
        CreateTextElement(buttonObj.transform, text, Vector2.zero, size, 16, TextAlignmentOptions.Center);

        return buttonObj;
    }

    private GameObject CreateScrollView(Transform parent, Vector2 position, Vector2 size)
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
        scrollRectComponent.verticalScrollbar = null;
        scrollRectComponent.viewport = scrollRect;

        return scrollView;
    }
}

