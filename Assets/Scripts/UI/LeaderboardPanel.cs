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

        if (titleText != null)
            titleText.text = "Таблица лидеров";

        PopulateSection(balanceContainer, "По балансу", byBalance);
        PopulateSection(levelContainer, "По уровню", byLevel);

        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
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
        Hide();
        OnCloseRequested?.Invoke();
    }

    private void PopulateSection(Transform container, string header, IReadOnlyList<LeaderboardEntry> entries)
    {
        if (container == null)
            return;

        ClearContainer(container);

        CreateText(container, header, 30, FontStyle.Bold);

        if (entries == null || entries.Count == 0)
        {
            CreateText(container, "Нет данных", 22, FontStyle.Italic);
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
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = content;
        text.supportRichText = supportRich;

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

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(root.transform, false);
        contentRect.anchorMin = new Vector2(0.05f, 0.05f);
        contentRect.anchorMax = new Vector2(0.95f, 0.95f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 18f;

        Text CreateTitle(string name, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var titleRect = go.GetComponent<RectTransform>();
            titleRect.SetParent(content.transform, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        var title = CreateTitle("Title", 38);
        title.text = "Таблица лидеров";

        Transform CreateSectionContainer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            var sectionRect = go.GetComponent<RectTransform>();
            sectionRect.SetParent(content.transform, false);
            var sectionLayout = go.GetComponent<VerticalLayoutGroup>();
            sectionLayout.childControlHeight = false;
            sectionLayout.childAlignment = TextAnchor.UpperLeft;
            sectionLayout.spacing = 8f;
            return sectionRect;
        }

        var balanceSection = CreateSectionContainer("BalanceSection");
        var levelSection = CreateSectionContainer("LevelSection");

        Button CreateCloseButton()
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonRect = go.GetComponent<RectTransform>();
            buttonRect.SetParent(content.transform, false);
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
        panel.TryInitialize();
        panel.Hide();

        return panel;
    }
}

