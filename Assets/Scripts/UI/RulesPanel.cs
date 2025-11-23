using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RulesPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private TMP_Text titleTextTMP;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Text contentText;
    [SerializeField] private TMP_Text contentTextTMP;
    [SerializeField] private Button closeButton;

    public event Action OnCloseRequested;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        canvasGroup ??= GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        SetOptionalText(titleText, titleTextTMP, "Правила игры: Техасский Холдем");

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleClose);
            closeButton.onClick.AddListener(HandleClose);
        }

        UpdateRulesContent();
        Hide();
    }

    public void Show()
    {
        if (gameObject == null)
            return;

        gameObject.SetActive(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Сбрасываем прокрутку в начало
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        transform.SetAsLastSibling();
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

    private void HandleClose()
    {
        Hide();
        OnCloseRequested?.Invoke();
    }

    private void UpdateRulesContent()
    {
        string rulesText = GetRulesText();
        SetOptionalText(contentText, contentTextTMP, rulesText);
    }

    private string GetRulesText()
    {
        return @"ТЕХАССКИЙ ХОЛДЕМ - ПРАВИЛА ИГРЫ

📋 ОСНОВНАЯ ИНФОРМАЦИЯ

Техасский Холдем - это разновидность покера, в которой каждый игрок получает 2 закрытые карты (hole cards), а на стол выкладывается 5 общих карт (community cards). Цель игры - составить лучшую покерную комбинацию из 5 карт, используя свои 2 карты и любые 5 карт со стола.

🎯 ЦЕЛЬ ИГРЫ

Выиграть все фишки оппонентов, составляя лучшие покерные комбинации или заставляя их сбрасывать карты (фолд).

🃏 ФАЗЫ ИГРЫ (РАУНДЫ)

1. ПРЕФЛОП (Pre-Flop)
   • Каждому игроку раздаются 2 закрытые карты
   • Игроки делают ставки, начиная с игрока слева от большого блайнда
   • Можно: Чек, Колл, Рейз, Фолд

2. ФЛОП (Flop)
   • На стол выкладываются первые 3 общие карты
   • Раунд торговли начинается с первого активного игрока слева от дилера
   • Можно: Чек, Колл, Рейз, Фолд

3. ТЕРН (Turn)
   • Выкладывается 4-я общая карта
   • Раунд торговли продолжается
   • Можно: Чек, Колл, Рейз, Фолд

4. РИВЕР (River)
   • Выкладывается последняя 5-я общая карта
   • Финальный раунд торговли
   • Можно: Чек, Колл, Рейз, Фолд

5. ШОУДАУН (Showdown)
   • Все активные игроки вскрывают свои карты
   • Игрок с лучшей комбинацией выигрывает банк

💰 СТАВКИ И ДЕЙСТВИЯ

• БИГ-БЛАЙНД (Big Blind) - обязательная ставка большого блайнда
• СМОЛЛ-БЛАЙНД (Small Blind) - обязательная ставка малого блайнда (обычно в 2 раза меньше)
• ЧЕК (Check) - пропуск хода без ставки (если никто не повысил ставку)
• КОЛЛ (Call) - уравнять текущую ставку
• РЕЙЗ (Raise) - повысить ставку
• ФОЛД (Fold) - сбросить карты и выйти из раздачи
• ОЛЛ-ИН (All-In) - поставить все свои фишки

🃏 ПОКЕРНЫЕ КОМБИНАЦИИ (от лучшей к худшей)

1. РОЯЛ-ФЛЕШ (Royal Flush)
   Туз, Король, Дама, Валет, Десятка одной масти
   Пример: A♠ K♠ Q♠ J♠ 10♠

2. СТРИТ-ФЛЕШ (Straight Flush)
   Пять карт подряд одной масти
   Пример: 9♥ 8♥ 7♥ 6♥ 5♥

3. КАРЕ (Four of a Kind)
   Четыре карты одного достоинства
   Пример: K♠ K♥ K♦ K♣ 7♠

4. ФУЛЛ-ХАУС (Full House)
   Три карты одного достоинства + пара
   Пример: Q♠ Q♥ Q♦ 8♣ 8♠

5. ФЛЕШ (Flush)
   Пять карт одной масти
   Пример: A♠ 9♠ 7♠ 5♠ 3♠

6. СТРИТ (Straight)
   Пять карт подряд разных мастей
   Пример: 10♠ 9♥ 8♦ 7♣ 6♠

7. ТРОЙКА (Three of a Kind)
   Три карты одного достоинства
   Пример: 7♠ 7♥ 7♦ K♣ 5♠

8. ДВЕ ПАРЫ (Two Pair)
   Две пары карт
   Пример: J♠ J♥ 8♦ 8♣ A♠

9. ПАРА (One Pair)
   Две карты одного достоинства
   Пример: A♠ A♥ K♦ Q♣ 7♠

10. СТАРШАЯ КАРТА (High Card)
    Если нет комбинации, выигрывает старшая карта
    Пример: A♠ K♥ Q♦ 9♣ 7♠

📊 СРАВНЕНИЕ КОМБИНАЦИЙ

При одинаковых комбинациях сравниваются старшие карты. Если и они совпадают - делят банк поровну.

💡 СТРАТЕГИЧЕСКИЕ СОВЕТЫ

• Не играйте каждую руку - будьте избирательны
• Обращайте внимание на позицию за столом
• Анализируйте действия соперников
• Управляйте банкроллом - не ставьте все фишки без необходимости
• Изучайте оппонентов - их стиль игры поможет принимать решения

⚠️ ВАЖНЫЕ ПРАВИЛА

• Минимальный рейз должен быть равен последнему рейзу или больше
• Максимальное количество рейзов за раунд может быть ограничено
• Если у игрока закончились фишки (олл-ин), он играет только за свою часть банка
• Игрок с нулевым балансом автоматически выбывает из игры

🎮 НАСТРОЙКИ СТОЛА

В этой игре используются следующие настройки:
• Стартовый стек: определяется настройками стола
• Блайнды: фиксированные (Small Blind / Big Blind)
• Количество рейзов: ограничено правилами стола

Удачи за столом! 🍀";
    }

    private void SetOptionalText(Text legacy, TMP_Text tmp, string value)
    {
        if (tmp != null)
        {
            tmp.text = value;
            tmp.enableWordWrapping = true;
        }
        if (legacy != null)
        {
            legacy.text = value;
            legacy.horizontalOverflow = HorizontalWrapMode.Wrap;
            legacy.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    public static RulesPanel CreateDefault(Transform parent)
    {
        GameObject root = new GameObject("RulesPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(900f, 700f);

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.1f, 0.1f, 0.1f, 0.98f);

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.SetParent(root.transform, false);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(800f, 50f);

        Text titleText = titleObj.GetComponent<Text>();
        titleText.text = "Правила игры: Техасский Холдем";
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;

        // Scroll View
        GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.SetParent(root.transform, false);
        scrollRect.anchorMin = new Vector2(0.05f, 0.1f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.85f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        ScrollRect scroll = scrollView.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        Image scrollImage = scrollView.GetComponent<Image>();
        scrollImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollView.transform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;

        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        Mask viewportMask = viewport.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        scroll.viewport = viewportRect;

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(viewport.transform, false);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 2000f);

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(20, 20, 20, 20);
        contentLayout.spacing = 10f;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;

        scroll.content = contentRect;

        // Content Text
        GameObject contentTextObj = new GameObject("ContentText", typeof(RectTransform), typeof(Text));
        RectTransform contentTextRect = contentTextObj.GetComponent<RectTransform>();
        contentTextRect.SetParent(content.transform, false);
        contentTextRect.sizeDelta = new Vector2(800f, 1950f);

        Text contentText = contentTextObj.GetComponent<Text>();
        contentText.text = "";
        contentText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        contentText.fontSize = 18;
        contentText.color = Color.white;
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentText.verticalOverflow = VerticalWrapMode.Overflow;

        // Close Button
        GameObject closeButtonObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform closeButtonRect = closeButtonObj.GetComponent<RectTransform>();
        closeButtonRect.SetParent(root.transform, false);
        closeButtonRect.anchorMin = new Vector2(0.5f, 0f);
        closeButtonRect.anchorMax = new Vector2(0.5f, 0f);
        closeButtonRect.pivot = new Vector2(0.5f, 0f);
        closeButtonRect.anchoredPosition = new Vector2(0f, 20f);
        closeButtonRect.sizeDelta = new Vector2(200f, 50f);

        Image closeButtonImage = closeButtonObj.GetComponent<Image>();
        closeButtonImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        Button closeButton = closeButtonObj.GetComponent<Button>();

        GameObject closeButtonTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        RectTransform closeButtonTextRect = closeButtonTextObj.GetComponent<RectTransform>();
        closeButtonTextRect.SetParent(closeButtonObj.transform, false);
        closeButtonTextRect.anchorMin = Vector2.zero;
        closeButtonTextRect.anchorMax = Vector2.one;
        closeButtonTextRect.offsetMin = Vector2.zero;
        closeButtonTextRect.offsetMax = Vector2.zero;

        Text closeButtonText = closeButtonTextObj.GetComponent<Text>();
        closeButtonText.text = "Закрыть";
        closeButtonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        closeButtonText.fontSize = 24;
        closeButtonText.color = Color.white;
        closeButtonText.alignment = TextAnchor.MiddleCenter;

        var panel = root.AddComponent<RulesPanel>();
        panel.canvasGroup = canvasGroup;
        panel.titleText = titleText;
        panel.scrollRect = scroll;
        panel.contentText = contentText;
        panel.closeButton = closeButton;

        panel.Initialize();
        panel.Hide();

        return panel;
    }
}

