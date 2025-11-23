using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public struct LeaderboardEntry
{
    public string username;
    public int level;
    public int chips;
    public int xp;
    public bool isCurrentUser;
}

public class LeaderboardPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private Transform balanceContainer;
    [SerializeField] private Transform levelContainer;
    [SerializeField] private Button closeButton;

    public event Action OnCloseRequested;

    private readonly List<GameObject> spawnedRows = new();
    private bool initialized;

    private void Awake()
    {
        TryInitialize();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleClose);
    }

    private void TryInitialize()
    {
        if (initialized)
            return;

        canvasGroup ??= GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleClose);
            closeButton.onClick.AddListener(HandleClose);
        }

        Hide();
        initialized = true;
    }

    public void Show(IReadOnlyList<LeaderboardEntry> byBalance, IReadOnlyList<LeaderboardEntry> byLevel)
    {
        TryInitialize();

        if (gameObject == null)
        {
            Debug.LogError("LeaderboardPanel: gameObject is null!");
            return;
        }

        gameObject.SetActive(true);

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        if (titleText != null)
            titleText.text = "Таблица лидеров";

        Debug.Log($"LeaderboardPanel: Показываю таблицу лидеров. По балансу: {byBalance?.Count ?? 0}, По уровню: {byLevel?.Count ?? 0}");

        PopulateSection(balanceContainer, "По балансу", byBalance);
        PopulateSection(levelContainer, "По уровню", byLevel);

        // Убеждаемся, что кнопка закрытия подключена
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleClose);
            closeButton.onClick.AddListener(HandleClose);
            Debug.Log("LeaderboardPanel: Кнопка закрытия подключена");
        }
        else
        {
            Debug.LogWarning("LeaderboardPanel: closeButton is null!");
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Перемещаем панель на верхний слой
        transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
    }

    public void Hide()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    private void HandleClose()
    {
        Debug.Log("LeaderboardPanel: HandleClose вызван");
        Hide();
        OnCloseRequested?.Invoke();
    }

    private void PopulateSection(Transform container, string header, IReadOnlyList<LeaderboardEntry> entries)
    {
        if (container == null)
        {
            Debug.LogWarning($"LeaderboardPanel: Container is null for section '{header}'");
            return;
        }

        ClearContainer(container);

        Debug.Log($"LeaderboardPanel: Заполняю секцию '{header}'. Записей: {entries?.Count ?? 0}");

        CreateText(container, header, 30, FontStyle.Bold);

        if (entries == null || entries.Count == 0)
        {
            CreateText(container, "Нет данных", 22, FontStyle.Italic);
            Debug.Log($"LeaderboardPanel: Секция '{header}' пуста, показываю 'Нет данных'");
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            string label = $"{i + 1}. {entry.username} — уровень {entry.level}, фишки: {entry.chips}";
            if (entry.isCurrentUser)
                label = $"<color=#FFD700>{label}</color>";
            CreateText(container, label, 24, FontStyle.Normal, true);
        }

        Debug.Log($"LeaderboardPanel: Секция '{header}' заполнена, создано {entries.Count} записей");
    }

    private void ClearContainer(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private Text CreateText(Transform parent, string content, int fontSize, FontStyle style, bool supportRich = false)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        // Используем верхний якорь для вертикального layout
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        
        // Фиксированная высота на основе размера шрифта
        float height = fontSize + 10f;
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = Vector2.zero; // Позиция будет установлена VerticalLayoutGroup

        // LayoutElement для правильной работы с VerticalLayoutGroup
        var layoutElement = go.GetComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        layoutElement.flexibleHeight = 0f;

        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = content;
        text.supportRichText = supportRich;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        spawnedRows.Add(go);
        return text;
    }

    public static LeaderboardPanel CreateDefault(Transform parent)
    {
        var root = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(Image));
        var rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(720f, 520f);

        var image = root.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.85f);

        var canvasGroup = root.AddComponent<CanvasGroup>();

        // Scroll View для прокрутки контента
        var scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        var scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.SetParent(root.transform, false);
        scrollRect.anchorMin = new Vector2(0.05f, 0.15f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.85f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        var scroll = scrollView.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        var scrollImage = scrollView.GetComponent<Image>();
        scrollImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        // Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollView.transform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;

        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        var viewportMask = viewport.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        scroll.viewport = viewportRect;

        // Content для прокрутки
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(viewport.transform, false);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);
        contentRect.anchoredPosition = Vector2.zero;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 18f;
        layout.padding = new RectOffset(20, 20, 20, 20);

        var contentFitter = content.GetComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        Text CreateTitle(string name, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var titleRect = go.GetComponent<RectTransform>();
            // Заголовок будет размещен вне ScrollView
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(700f, fontSize + 15f);
            
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        // Заголовок вне ScrollView
        var title = CreateTitle("Title", 38);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.SetParent(root.transform, false);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(700f, 53f);
        title.text = "Таблица лидеров";

        Transform CreateSectionContainer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var sectionRect = go.GetComponent<RectTransform>();
            sectionRect.SetParent(content.transform, false);
            // Настройка размера контейнера
            sectionRect.anchorMin = new Vector2(0f, 1f);
            sectionRect.anchorMax = new Vector2(1f, 1f);
            sectionRect.pivot = new Vector2(0.5f, 1f);
            sectionRect.sizeDelta = new Vector2(0f, 0f);
            
            var sectionLayout = go.GetComponent<VerticalLayoutGroup>();
            sectionLayout.childControlHeight = false;
            sectionLayout.childControlWidth = true;
            sectionLayout.childForceExpandHeight = false;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childAlignment = TextAnchor.UpperLeft;
            sectionLayout.spacing = 8f;
            sectionLayout.padding = new RectOffset(10, 10, 10, 10);
            
            // ContentSizeFitter для автоматической высоты
            var sizeFitter = go.GetComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            return sectionRect;
        }

        var balanceSection = CreateSectionContainer("BalanceSection");
        var levelSection = CreateSectionContainer("LevelSection");

        // Кнопка закрытия вне ScrollView
        Button CreateCloseButton()
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonRect = go.GetComponent<RectTransform>();
            buttonRect.SetParent(root.transform, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 20f);
            
            buttonRect.sizeDelta = new Vector2(260f, 50f);

            var buttonImage = go.GetComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.3f, 1f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = buttonImage;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.SetParent(go.transform, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelText = labelGO.GetComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 24;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = "Закрыть";

#if UNITY_EDITOR
            EditorUtility.SetDirty(go);
#endif
            return button;
        }

        var closeBtn = CreateCloseButton();

        var panel = root.AddComponent<LeaderboardPanel>();
        panel.canvasGroup = canvasGroup;
        panel.titleText = title;
        panel.balanceContainer = balanceSection;
        panel.levelContainer = levelSection;
        panel.closeButton = closeBtn;
        
        // Убеждаемся, что кнопка закрытия подключена
        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(() => {
                Debug.Log("LeaderboardPanel: CloseButton clicked");
                panel.HandleClose();
            });
        }
        
        panel.TryInitialize();
        panel.Hide();

        Debug.Log("LeaderboardPanel: CreateDefault завершен успешно");
        return panel;
    }
}



