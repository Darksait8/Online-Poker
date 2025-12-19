using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Контроллер панели калькулятора вероятностей покерных комбинаций
/// </summary>
public class ProbabilityCalculatorPanel : MonoBehaviour
{
    [Header("Основные элементы")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button toggleButton; // Кнопка для открытия/закрытия панели
    [SerializeField] private Button closeButton; // Кнопка закрытия панели
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText; // Текст статуса (показывает текущие карты)

    [Header("Контейнер для списка комбинаций")]
    [SerializeField] private Transform combinationsContainer;
    [SerializeField] private GameObject combinationItemPrefab; // Префаб элемента списка

    [Header("Настройки")]
    [SerializeField] private KeyCode toggleKey = KeyCode.P; // Горячая клавиша для открытия/закрытия
    [SerializeField] private bool startVisible = false;
    [SerializeField] private bool useRealTimeOdds = true; // Использовать реальные вероятности на основе карт
    [SerializeField] private float updateInterval = 0.5f; // Интервал обновления вероятностей (секунды)

    [Header("Ссылки на игровые объекты")]
    [SerializeField] private GameManager gameManager; // Для доступа к картам игрока и стола

    private bool isVisible = false;
    private List<GameObject> combinationItems = new List<GameObject>();
    private Dictionary<string, double> currentOdds = new Dictionary<string, double>();
    private float lastUpdateTime = 0f;
    
    // Публичные свойства для доступа из редактора и других скриптов
    public GameObject Panel { get => panel; set => panel = value; }
    public Button ToggleButton { get => toggleButton; set => toggleButton = value; }
    public Button CloseButton { get => closeButton; set => closeButton = value; }
    public TextMeshProUGUI TitleText { get => titleText; set => titleText = value; }
    public Transform CombinationsContainer { get => combinationsContainer; set => combinationsContainer = value; }

    private void Awake()
    {
        // Ищем toggleButton как дочерний объект
        if (toggleButton == null)
        {
            Transform btnTransform = transform.Find("ToggleProbabilityButton");
            if (btnTransform != null)
            {
                toggleButton = btnTransform.GetComponent<Button>();
                Debug.Log("ProbabilityCalculatorPanel: toggleButton найден как дочерний объект");
            }
        }
        
        // Если panel не назначен, ищем объект с Image (панель с контентом)
        if (panel == null)
        {
            // Ищем первый дочерний объект с Image который НЕ является кнопкой
            foreach (Transform child in transform)
            {
                if (child.name != "ToggleProbabilityButton" && child.GetComponent<Image>() != null)
                {
                    panel = child.gameObject;
                    Debug.Log($"ProbabilityCalculatorPanel: panel найден как '{child.name}'");
                    break;
                }
            }
        }

        if (panel != null)
        {
            panel.SetActive(startVisible);
            isVisible = startVisible;
            Debug.Log($"ProbabilityCalculatorPanel: Панель инициализирована, startVisible = {startVisible}");
        }
        else
        {
            Debug.LogWarning("ProbabilityCalculatorPanel: panel не найден! Назначьте панель в инспекторе.");
        }
        
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
            Debug.Log("ProbabilityCalculatorPanel: toggleButton привязан");
        }
        else
        {
            Debug.LogWarning("ProbabilityCalculatorPanel: toggleButton не назначен!");
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
            Debug.Log("ProbabilityCalculatorPanel: closeButton привязан");
        }
        else
        {
            Debug.LogWarning("ProbabilityCalculatorPanel: closeButton не назначен!");
        }

        if (titleText != null)
            titleText.text = "Калькулятор вероятностей";
        
        if (statusText != null)
            statusText.text = "Ожидание карт...";
    }

    private void Start()
    {
        // Автоматически находим GameManager, если не назначен
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        // Заполняем список комбинаций с небольшой задержкой, чтобы убедиться, что UI готов
        if (combinationsContainer != null)
        {
            PopulateCombinations();
        }
        else
        {
            Debug.LogWarning("ProbabilityCalculatorPanel: combinationsContainer не назначен! Панель не будет отображать комбинации.");
        }
        
        // Инициализируем стандартные вероятности
        var allCombinations = PokerProbabilityCalculator.GetAllCombinations();
        foreach (var combo in allCombinations)
        {
            currentOdds[combo.Name] = combo.Probability;
        }
    }

    private void Update()
    {
        // Проверяем нажатие клавиши только если игра не на паузе
        if (Time.timeScale > 0 && Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"ProbabilityCalculatorPanel: Нажата клавиша {toggleKey}");
            TogglePanel();
        }
        
        // Обновляем вероятности в реальном времени
        if (useRealTimeOdds && isVisible && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateProbabilities();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Обновляет вероятности на основе текущих карт
    /// </summary>
    private void UpdateProbabilities()
    {
        if (gameManager == null)
            return;
        
        // Получаем карты игрока
        Card[] holeCards = null;
        var players = gameManager.Players;
        if (players != null && players.Count > 0)
        {
            // Ищем игрока-человека (обычно первый или по индексу)
            foreach (var player in players)
            {
                if (player != null && player.HoleCards != null && player.HoleCards.Length == 2 && !player.IsFolded)
                {
                    holeCards = player.HoleCards;
                    break;
                }
            }
        }
        
        // Получаем карты на столе
        Card[] boardCards = null;
        var communityCards = gameManager.CommunityCards;
        if (communityCards != null && communityCards.Count > 0)
        {
            boardCards = communityCards.ToArray();
        }
        
        // Обновляем статус
        UpdateStatusText(holeCards, boardCards);
        
        // Рассчитываем вероятности
        if (holeCards != null)
        {
            try
            {
                currentOdds = PokerOddsCalculator.CalculateOdds(holeCards, boardCards);
                UpdateProbabilitiesDisplay();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ProbabilityCalculatorPanel: Ошибка при расчете вероятностей: {e.Message}");
            }
        }
        else
        {
            // Используем стандартные вероятности
            var allCombinations = PokerProbabilityCalculator.GetAllCombinations();
            foreach (var combo in allCombinations)
            {
                currentOdds[combo.Name] = combo.Probability;
            }
            UpdateProbabilitiesDisplay();
        }
    }
    
    /// <summary>
    /// Обновляет текст статуса с информацией о текущих картах
    /// </summary>
    private void UpdateStatusText(Card[] holeCards, Card[] boardCards)
    {
        if (statusText == null)
            return;
        
        if (holeCards == null || holeCards.Length != 2)
        {
            statusText.text = "Ожидание карт...";
            return;
        }
        
        string status = $"Ваши карты: {FormatCard(holeCards[0])} {FormatCard(holeCards[1])}";
        
        if (boardCards != null && boardCards.Length > 0)
        {
            status += " | Стол: ";
            for (int i = 0; i < boardCards.Length; i++)
            {
                if (i > 0) status += " ";
                status += FormatCard(boardCards[i]);
            }
        }
        else
        {
            status += " | Префлоп";
        }
        
        statusText.text = status;
    }
    
    /// <summary>
    /// Форматирует карту для отображения
    /// </summary>
    private string FormatCard(Card card)
    {
        string rank = "";
        switch (card.Rank)
        {
            case Rank.Ace: rank = "A"; break;
            case Rank.King: rank = "K"; break;
            case Rank.Queen: rank = "Q"; break;
            case Rank.Jack: rank = "J"; break;
            case Rank.Ten: rank = "10"; break;
            default: rank = ((int)card.Rank).ToString(); break;
        }
        
        string suit = "";
        switch (card.Suit)
        {
            case Suit.Spades: suit = "♠"; break;
            case Suit.Hearts: suit = "♥"; break;
            case Suit.Diamonds: suit = "♦"; break;
            case Suit.Clubs: suit = "♣"; break;
        }
        
        return rank + suit;
    }
    
    /// <summary>
    /// Обновляет отображение вероятностей
    /// </summary>
    private void UpdateProbabilitiesDisplay()
    {
        foreach (var item in combinationItems)
        {
            if (item == null) continue;
            
            // Ищем текстовые элементы с вероятностями
            TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                // Второй элемент - вероятность
                string comboName = GetCombinationNameFromItem(item);
                if (!string.IsNullOrEmpty(comboName) && currentOdds.ContainsKey(comboName))
                {
                    texts[1].text = PokerProbabilityCalculator.FormatProbability(currentOdds[comboName]);
                }
            }
        }
    }
    
    /// <summary>
    /// Получает название комбинации из элемента списка
    /// </summary>
    private string GetCombinationNameFromItem(GameObject item)
    {
        TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length > 0)
        {
            string russianName = texts[0].text;
            // Конвертируем русское название в английское
            var allCombinations = PokerProbabilityCalculator.GetAllCombinations();
            foreach (var combo in allCombinations)
            {
                if (combo.RussianName == russianName)
                {
                    return combo.Name;
                }
            }
        }
        return null;
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(TogglePanel);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }

    /// <summary>
    /// Заполняет список комбинаций
    /// </summary>
    private void PopulateCombinations()
    {
        if (combinationsContainer == null)
        {
            Debug.LogWarning("ProbabilityCalculatorPanel: combinationsContainer не назначен!");
            return;
        }

        // Очищаем существующие элементы
        foreach (var item in combinationItems)
        {
            if (item != null)
                Destroy(item);
        }
        combinationItems.Clear();

        // Получаем все комбинации
        var combinations = PokerProbabilityCalculator.GetAllCombinations();

        // Если префаб не назначен, создаем элементы программно
        if (combinationItemPrefab == null)
        {
            CreateCombinationItemsProgrammatically(combinations);
        }
        else
        {
            CreateCombinationItemsFromPrefab(combinations);
        }
    }

    /// <summary>
    /// Создает элементы списка из префаба
    /// </summary>
    private void CreateCombinationItemsFromPrefab(List<PokerProbabilityCalculator.CombinationInfo> combinations)
    {
        foreach (var combo in combinations)
        {
            GameObject item = Instantiate(combinationItemPrefab, combinationsContainer);
            SetupCombinationItem(item, combo);
            combinationItems.Add(item);
        }
    }

    /// <summary>
    /// Создает элементы списка программно
    /// </summary>
    private void CreateCombinationItemsProgrammatically(List<PokerProbabilityCalculator.CombinationInfo> combinations)
    {
        foreach (var combo in combinations)
        {
            // Создаем контейнер для элемента
            GameObject item = new GameObject($"CombinationItem_{combo.Rank}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            item.transform.SetParent(combinationsContainer, false);

            RectTransform rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 80); // Увеличиваем высоту для карт

            // Настройка Layout Group
            HorizontalLayoutGroup layout = item.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Фон элемента (чередование цветов для читаемости)
            Image bgImage = item.GetComponent<Image>();
            bgImage.color = combo.Rank % 2 == 0 ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : new Color(0.15f, 0.15f, 0.15f, 0.5f);

            // Название комбинации
            CreateTextElement(item.transform, combo.RussianName, 180, TextAlignmentOptions.Left, FontStyles.Bold);
            
            // Контейнер для карт (пример комбинации)
            GameObject cardsContainer = CreateCardsContainer(item.transform, combo.ExampleCards);
            
            // Вероятность
            CreateTextElement(item.transform, PokerProbabilityCalculator.FormatProbability(combo.Probability), 120, TextAlignmentOptions.Center);
            
            // Шансы
            CreateTextElement(item.transform, combo.Odds, 150, TextAlignmentOptions.Right);

            combinationItems.Add(item);
        }
    }
    
    /// <summary>
    /// Создает контейнер с картами для визуализации комбинации
    /// </summary>
    private GameObject CreateCardsContainer(Transform parent, Card[] cards)
    {
        if (cards == null || cards.Length == 0)
            return null;
            
        GameObject container = new GameObject("CardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        container.transform.SetParent(parent, false);

        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(200, 70);
        containerRect.anchorMin = new Vector2(0, 0.5f);
        containerRect.anchorMax = new Vector2(0, 0.5f);
        containerRect.pivot = new Vector2(0, 0.5f);

        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = -30; // Перекрытие карт для красивого вида
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Создаем изображения карт
        foreach (var card in cards)
        {
            GameObject cardImageObj = new GameObject($"Card_{card.Suit}_{card.Rank}", typeof(RectTransform), typeof(Image));
            cardImageObj.transform.SetParent(container.transform, false);

            RectTransform cardRect = cardImageObj.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(50, 70);
            cardRect.anchorMin = new Vector2(0, 0.5f);
            cardRect.anchorMax = new Vector2(0, 0.5f);
            cardRect.pivot = new Vector2(0, 0.5f);

            Image cardImage = cardImageObj.GetComponent<Image>();
            
            // Получаем спрайт карты через CardSpriteProvider
            Sprite cardSprite = CardSpriteProvider.GetSprite(card);
            if (cardSprite != null)
            {
                cardImage.sprite = cardSprite;
            }
            else
            {
                // Если спрайт не найден, используем рубашку или создаем placeholder
                Sprite cardBack = CardSpriteProvider.GetCardBack();
                if (cardBack != null)
                {
                    cardImage.sprite = cardBack;
                }
                else
                {
                    // Создаем простой цветной прямоугольник как fallback
                    cardImage.color = new Color(0.3f, 0.3f, 0.5f, 1f);
                }
            }
            
            cardImage.preserveAspect = true;
        }

        return container;
    }

    /// <summary>
    /// Создает текстовый элемент
    /// </summary>
    private void CreateTextElement(Transform parent, string text, float width, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(width, 0);
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(0, 1);
        textRect.pivot = new Vector2(0, 0.5f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.color = Color.white;
    }

    /// <summary>
    /// Настраивает элемент комбинации (если используется префаб)
    /// </summary>
    private void SetupCombinationItem(GameObject item, PokerProbabilityCalculator.CombinationInfo combo)
    {
        // Ищем текстовые элементы в префабе
        TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
        
        if (texts.Length >= 1)
            texts[0].text = combo.RussianName;
        if (texts.Length >= 2)
            texts[1].text = PokerProbabilityCalculator.FormatProbability(combo.Probability);
        if (texts.Length >= 3)
            texts[2].text = combo.Odds;
        
        // Ищем контейнер для карт или создаем его
        Transform cardsContainer = item.transform.Find("CardsContainer");
        if (cardsContainer == null)
        {
            // Создаем контейнер для карт, если его нет
            CreateCardsContainer(item.transform, combo.ExampleCards);
        }
        else
        {
            // Обновляем существующие карты
            UpdateCardsInContainer(cardsContainer, combo.ExampleCards);
        }
    }
    
    /// <summary>
    /// Обновляет карты в существующем контейнере
    /// </summary>
    private void UpdateCardsInContainer(Transform container, Card[] cards)
    {
        if (cards == null || cards.Length == 0)
            return;
            
        // Удаляем старые карты
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        
        // Создаем новые карты
        foreach (var card in cards)
        {
            GameObject cardImageObj = new GameObject($"Card_{card.Suit}_{card.Rank}", typeof(RectTransform), typeof(Image));
            cardImageObj.transform.SetParent(container, false);

            RectTransform cardRect = cardImageObj.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(50, 70);
            cardRect.anchorMin = new Vector2(0, 0.5f);
            cardRect.anchorMax = new Vector2(0, 0.5f);
            cardRect.pivot = new Vector2(0, 0.5f);

            Image cardImage = cardImageObj.GetComponent<Image>();
            
            Sprite cardSprite = CardSpriteProvider.GetSprite(card);
            if (cardSprite != null)
            {
                cardImage.sprite = cardSprite;
            }
            else
            {
                Sprite cardBack = CardSpriteProvider.GetCardBack();
                if (cardBack != null)
                {
                    cardImage.sprite = cardBack;
                }
                else
                {
                    cardImage.color = new Color(0.3f, 0.3f, 0.5f, 1f);
                }
            }
            
            cardImage.preserveAspect = true;
        }
    }

    /// <summary>
    /// Открывает панель
    /// </summary>
    public void OpenPanel()
    {
        if (panel != null)
        {
            // Принудительно активируем панель и всех родителей
            panel.SetActive(true);
            
            // Убеждаемся что все родители тоже активны
            Transform parent = panel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                {
                    Debug.LogWarning($"ProbabilityCalculatorPanel: Родитель '{parent.name}' был неактивен, активируем");
                    parent.gameObject.SetActive(true);
                }
                parent = parent.parent;
            }
            
            isVisible = true;
            Debug.Log($"ProbabilityCalculatorPanel: Панель открыта, activeInHierarchy = {panel.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("ProbabilityCalculatorPanel: Не удалось открыть панель - panel == null");
        }
    }

    /// <summary>
    /// Закрывает панель
    /// </summary>
    public void ClosePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
            isVisible = false;
            Debug.Log("ProbabilityCalculatorPanel: Панель закрыта");
        }
        else
        {
            Debug.LogError("ProbabilityCalculatorPanel: Не удалось закрыть панель - panel == null");
        }
    }

    /// <summary>
    /// Переключает видимость панели
    /// </summary>
    public void TogglePanel()
    {
        // Синхронизируем isVisible с реальным состоянием панели
        if (panel != null)
        {
            isVisible = panel.activeInHierarchy;
        }
        
        Debug.Log($"ProbabilityCalculatorPanel: TogglePanel вызван, isVisible = {isVisible}, panel активен = {(panel != null ? panel.activeInHierarchy.ToString() : "null")}");
        
        if (isVisible)
            ClosePanel();
        else
            OpenPanel();
    }

    /// <summary>
    /// Обновляет список комбинаций (можно вызвать вручную, если нужно)
    /// </summary>
    public void RefreshCombinations()
    {
        PopulateCombinations();
    }
    
    /// <summary>
    /// Рекурсивный поиск кнопки по имени
    /// </summary>
    private Button FindButtonRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null) return btn;
            }
            var result = FindButtonRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}

