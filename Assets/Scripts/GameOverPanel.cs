using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameOverPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private Text summaryText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    public event Action OnRestartRequested;
    public event Action OnMainMenuRequested;

    private bool initialized;

    private void Awake()
    {
        TryInitialize();
    }

    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(HandleRestart);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(HandleMainMenu);
    }

    private void TryInitialize()
    {
        if (initialized)
            return;

        canvasGroup ??= GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestart);
            restartButton.onClick.AddListener(HandleRestart);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(HandleMainMenu);
            mainMenuButton.onClick.AddListener(HandleMainMenu);
        }

        HideImmediate();
        initialized = true;
    }

    public void Configure(CanvasGroup group, Text title, Text summary, Button restart, Button mainMenu)
    {
        canvasGroup = group ?? canvasGroup;
        titleText = title ?? titleText;
        summaryText = summary ?? summaryText;
        restartButton = restart ?? restartButton;
        mainMenuButton = mainMenu ?? mainMenuButton;
        initialized = false;
        TryInitialize();
    }

    public void Show(string winnerName, int winnerStack, int totalHandsPlayed = 0, bool showRestartButton = true)
    {
        TryInitialize();

        if (titleText != null)
            titleText.text = "Партия завершена";

        if (summaryText != null)
        {
            summaryText.text = $"Победитель: {winnerName}\n" +
                               $"Стек: {winnerStack}\n" +
                               (totalHandsPlayed > 0 ? $"Сыграно раздач: {totalHandsPlayed}" : string.Empty);
        }

        // Скрываем или показываем кнопку перезапуска
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(showRestartButton);
        }

        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void HideImmediate()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    private void HandleRestart()
    {
        OnRestartRequested?.Invoke();
    }

    private void HandleMainMenu()
    {
        OnMainMenuRequested?.Invoke();
    }

    public static GameOverPanel CreateDefault(Transform parent)
    {
        var root = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        var rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 360f);

        var bg = root.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        var verticalLayout = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        var contentRect = verticalLayout.GetComponent<RectTransform>();
        contentRect.SetParent(root.transform, false);
        contentRect.anchorMin = new Vector2(0.1f, 0.1f);
        contentRect.anchorMax = new Vector2(0.9f, 0.9f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var layoutGroup = verticalLayout.GetComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 16f;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;

        Text CreateText(string name, int fontSize, FontStyle fontStyle)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var textRect = go.GetComponent<RectTransform>();
            textRect.SetParent(verticalLayout.transform, false);
            var text = go.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = Color.white;
            text.supportRichText = true;
            return text;
        }

        var title = CreateText("Title", 36, FontStyle.Bold);
        title.text = "Партия завершена";

        var summary = CreateText("Summary", 26, FontStyle.Normal);
        summary.text = string.Empty;

        Button CreateButton(string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonRect = go.GetComponent<RectTransform>();
            buttonRect.SetParent(verticalLayout.transform, false);
            buttonRect.sizeDelta = new Vector2(280f, 48f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.13f, 0.5f, 0.24f, 1f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = image;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.SetParent(go.transform, false);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelGO.GetComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

#if UNITY_EDITOR
            EditorUtility.SetDirty(go);
#endif
            return btn;
        }

        var restartBtn = CreateButton("RestartButton", "Перезапустить игру");
        var mainMenuBtn = CreateButton("MainMenuButton", "Главное меню");

        var panel = root.AddComponent<GameOverPanel>();
        var cg = root.AddComponent<CanvasGroup>();
        panel.Configure(cg, title, summary, restartBtn, mainMenuBtn);

        return panel;
    }
}

