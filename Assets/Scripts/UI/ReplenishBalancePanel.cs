using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ReplenishBalancePanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private TMP_Text titleTextTMP;
    [SerializeField] private Text currentBalanceText;
    [SerializeField] private TMP_Text currentBalanceTextTMP;
    [SerializeField] private Transform amountsContainer;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button cancelButton;

    [Header("Amount Options")]
    [SerializeField] private int[] replenishAmounts = { 500, 1000, 2500, 5000, 10000 };
    [SerializeField] private GameObject amountButtonPrefab;
    [SerializeField] private Color selectedButtonColor = new Color(0.2f, 0.6f, 0.3f, 1f);
    [SerializeField] private Color normalButtonColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    public event Action<int> OnAmountSelected;
    public event Action OnCloseRequested;

    private int selectedAmount = 0;
    private readonly List<Button> amountButtons = new List<Button>();

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        SetOptionalText(titleText, titleTextTMP, "Пополнение баланса");
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleClose);
            closeButton.onClick.AddListener(HandleClose);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(HandleClose);
            cancelButton.onClick.AddListener(HandleClose);
        }

        Hide();
    }

    public void Show()
    {
        Debug.Log("ReplenishBalancePanel: Show() вызван");
        Initialize();
        
        if (gameObject == null)
        {
            Debug.LogError("ReplenishBalancePanel: gameObject is null!");
            return;
        }
        
        gameObject.SetActive(true);
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        RefreshCurrentBalance();
        CreateAmountButtons();
        selectedAmount = 0;
        
        // Принудительно перемещаем панель на верхний слой
        // Но не устанавливаем слишком высокий sortingOrder, чтобы не перекрывать другие панели
        transform.SetAsLastSibling();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // Устанавливаем умеренный порядок сортировки (выше обычных элементов, но не максимальный)
            // Другие панели могут иметь свои порядки
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 50);
        }
        
        // Принудительно обновляем Canvas
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"ReplenishBalancePanel: Панель показана. GameObject активен: {gameObject.activeSelf}, CanvasGroup alpha: {canvasGroup.alpha}, Transform sibling index: {transform.GetSiblingIndex()}, Canvas sortingOrder: {(canvas != null ? canvas.sortingOrder.ToString() : "N/A")}");
    }

    public void Hide()
    {
        if (canvasGroup == null)
            return;

        gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void RefreshCurrentBalance()
    {
        UserProfile profile = AuthManager.CurrentUser;
        int balance = profile?.chips ?? 0;
        int remainingWeeklyLimit = profile?.GetRemainingWeeklyDeposit() ?? 0;
        
        string balanceText = $"Текущий баланс: {FormatBalance(balance)}\n";
        balanceText += $"Недельный лимит: {FormatBalance(remainingWeeklyLimit)} из {FormatBalance(profile?.weeklyDepositLimit ?? 10000)}";
        
        SetOptionalText(currentBalanceText, currentBalanceTextTMP, balanceText);
    }

    private void CreateAmountButtons()
    {
        // Очищаем существующие кнопки
        ClearAmountButtons();

        if (amountsContainer == null)
        {
            Debug.LogWarning("ReplenishBalancePanel: amountsContainer не привязан!");
            return;
        }

        foreach (int amount in replenishAmounts)
        {
            GameObject buttonObj = CreateAmountButton(amount);
            buttonObj.transform.SetParent(amountsContainer, false);
        }
    }

    private GameObject CreateAmountButton(int amount)
    {
        GameObject buttonObj;
        
        if (amountButtonPrefab != null)
        {
            buttonObj = Instantiate(amountButtonPrefab);
        }
        else
        {
            // Создаем кнопку программно
            buttonObj = new GameObject($"AmountButton_{amount}", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 70f); // Увеличили размер кнопок

            Image image = buttonObj.GetComponent<Image>();
            image.color = normalButtonColor;

            Button button = buttonObj.GetComponent<Button>();
            button.targetGraphic = image;

            // Создаем текст на кнопке
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.SetParent(buttonObj.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObj.GetComponent<Text>();
            text.text = FormatBalance(amount);
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }

        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null)
        {
            int amountCopy = amount; // Локальная копия для замыкания
            btn.onClick.AddListener(() => OnAmountButtonClicked(amountCopy, btn));
            amountButtons.Add(btn);
        }

        return buttonObj;
    }

    private void OnAmountButtonClicked(int amount, Button clickedButton)
    {
        selectedAmount = amount;
        
        // Обновляем цвета кнопок
        foreach (var button in amountButtons)
        {
            if (button != null && button.targetGraphic is Image img)
            {
                img.color = (button == clickedButton) ? selectedButtonColor : normalButtonColor;
            }
        }

        Debug.Log($"ReplenishBalancePanel: Выбрана сумма пополнения: {amount}");
    }

    private void ClearAmountButtons()
    {
        foreach (var button in amountButtons)
        {
            if (button != null && button.gameObject != null)
                Destroy(button.gameObject);
        }
        amountButtons.Clear();

        if (amountsContainer != null)
        {
            for (int i = amountsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = amountsContainer.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    private void HandleClose()
    {
        Hide();
        OnCloseRequested?.Invoke();
    }

    public void ConfirmReplenishment()
    {
        if (selectedAmount <= 0)
        {
            Debug.LogWarning("ReplenishBalancePanel: Выберите сумму для пополнения!");
            return;
        }
        
        UserProfile profile = AuthManager.CurrentUser;
        if (profile == null)
        {
            Debug.LogWarning("ReplenishBalancePanel: Пользователь не авторизован!");
            return;
        }
        
        // Проверяем недельный лимит
        if (!profile.CanDeposit(selectedAmount))
        {
            int remaining = profile.GetRemainingWeeklyDeposit();
            string errorMessage = remaining > 0 
                ? $"Превышен недельный лимит!\nМаксимум на эту неделю: {FormatBalance(remaining)}"
                : "Недельный лимит исчерпан!\nПопробуйте на следующей неделе";
                
            Debug.LogWarning($"ReplenishBalancePanel: {errorMessage}");
            // Здесь можно показать popup с ошибкой
            return;
        }
        
        OnAmountSelected?.Invoke(selectedAmount);
        HandleClose();
    }

    private string FormatBalance(int chips)
    {
        return chips.ToString("N0").Replace(",", " ");
    }

    private void SetOptionalText(Text legacy, TMP_Text tmp, string value)
    {
        if (tmp != null)
            tmp.text = value;
        if (legacy != null)
            legacy.text = value;
    }

    public static ReplenishBalancePanel CreateDefault(Transform parent)
    {
        Debug.Log("ReplenishBalancePanel: CreateDefault вызван");
        
        GameObject root = new GameObject("ReplenishBalancePanel", typeof(RectTransform), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(700f, 600f); // Увеличили размер панели
        
        // Устанавливаем высокий порядок отрисовки, чтобы панель была поверх других элементов
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            rootRect.SetAsLastSibling(); // Перемещаем в конец списка дочерних элементов
            Debug.Log($"ReplenishBalancePanel: Установлен порядок отрисовки. Canvas: {canvas.name}");
        }

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(root.transform, false);
        contentRect.anchorMin = new Vector2(0.05f, 0.05f);
        contentRect.anchorMax = new Vector2(0.95f, 0.95f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 20f;
        contentLayout.childControlHeight = false;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.padding = new RectOffset(20, 20, 20, 20);

        // Title
        Text CreateTitle(string name)
        {
            GameObject titleObj = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.SetParent(content.transform, false);
            var title = titleObj.GetComponent<Text>();
            title.text = "Пополнение баланса";
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = 28;
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleCenter;
            return title;
        }

        Text title = CreateTitle("Title");

        // Current Balance
        Text CreateCurrentBalance(string name)
        {
            GameObject balanceObj = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform balanceRect = balanceObj.GetComponent<RectTransform>();
            balanceRect.SetParent(content.transform, false);
            var balance = balanceObj.GetComponent<Text>();
            balance.text = "Текущий баланс: 0";
            balance.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            balance.fontSize = 22;
            balance.color = Color.yellow;
            balance.alignment = TextAnchor.MiddleCenter;
            return balance;
        }

        Text currentBalance = CreateCurrentBalance("CurrentBalance");

        // Amounts Container
        GameObject amountsContainer = new GameObject("AmountsContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        RectTransform amountsRect = amountsContainer.GetComponent<RectTransform>();
        amountsRect.SetParent(content.transform, false);
        amountsRect.sizeDelta = new Vector2(0f, 250f);

        GridLayoutGroup grid = amountsContainer.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(180f, 60f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;

        // Buttons Container
        GameObject buttonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform buttonsRect = buttonsContainer.GetComponent<RectTransform>();
        buttonsRect.SetParent(content.transform, false);
        buttonsRect.sizeDelta = new Vector2(0f, 50f);

        HorizontalLayoutGroup buttonsLayout = buttonsContainer.GetComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 10f;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childControlHeight = false;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;

        Button CreateButton(string name, string label, Color color, Action onClick)
        {
            GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.SetParent(buttonsContainer.transform, false);
            buttonRect.sizeDelta = new Vector2(150f, 50f);

            Image buttonImage = buttonObj.GetComponent<Image>();
            buttonImage.color = color;

            Button button = buttonObj.GetComponent<Button>();
            button.targetGraphic = buttonImage;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.SetParent(buttonObj.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObj.GetComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 22; // Увеличили размер шрифта на кнопках действий
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            return button;
        }

        Button confirmButton = CreateButton("ConfirmButton", "Пополнить", new Color(0.2f, 0.7f, 0.3f, 1f), null);
        Button cancelButton = CreateButton("CancelButton", "Отмена", new Color(0.5f, 0.5f, 0.5f, 1f), null);

        var panel = root.AddComponent<ReplenishBalancePanel>();
        panel.canvasGroup = canvasGroup;
        panel.titleText = title;
        panel.currentBalanceText = currentBalance;
        panel.amountsContainer = amountsRect;
        panel.closeButton = cancelButton;
        panel.cancelButton = cancelButton;
        panel.replenishAmounts = new int[] { 500, 1000, 2500, 5000, 10000 };

        confirmButton.onClick.AddListener(() => panel.ConfirmReplenishment());

        panel.Initialize();
        panel.Hide();

        return panel;
    }
}

