using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Автоматическое создание полноценного UI для покерной игры
/// </summary>
public class PokerUIBuilderFull : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool createUIOnStart = true;
    [SerializeField] private string playerName = "Player";
    
    private void Start()
    {
        if (createUIOnStart)
        {
            CreatePokerUI();
        }
    }
    
    [ContextMenu("Create Full Poker UI")]
    public void CreatePokerUI()
    {
        // Создаем Canvas если его нет
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Создаем основной контейнер
        GameObject mainPanel = CreatePanel("MainPanel", canvas.transform);
        RectTransform mainRect = mainPanel.GetComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.sizeDelta = Vector2.zero;
        mainRect.anchoredPosition = Vector2.zero;
        
        // Создаем заголовок
        CreateText("Title", "🎰 ПОКЕРНАЯ ИГРА", mainPanel.transform, new Vector2(0, 300), new Vector2(600, 60), 28, Color.white);
        
        // Создаем панель подключения
        CreateConnectionPanel(mainPanel.transform);
        
        // Создаем панель игровой информации
        CreateGameInfoPanel(mainPanel.transform);
        
        // Создаем панель действий
        CreateActionPanel(mainPanel.transform);
        
        // Создаем панель логов
        CreateLogPanel(mainPanel.transform);
        
        // Добавляем компоненты
        AddComponents(mainPanel);
        
        Debug.Log("✅ Полноценный UI для покерной игры создан!");
    }
    
    private void CreateConnectionPanel(Transform parent)
    {
        GameObject connectionPanel = CreatePanel("ConnectionPanel", parent);
        RectTransform connRect = connectionPanel.GetComponent<RectTransform>();
        connRect.anchorMin = new Vector2(0, 0.8f);
        connRect.anchorMax = new Vector2(1, 1);
        connRect.sizeDelta = Vector2.zero;
        connRect.anchoredPosition = Vector2.zero;
        
        // Заголовок панели
        CreateText("ConnectionTitle", "🔌 ПОДКЛЮЧЕНИЕ", connectionPanel.transform, new Vector2(0, 180), new Vector2(400, 30), 18, Color.cyan);
        
        // Поля ввода
        CreateInputField("ServerHostInput", "IP сервера:", connectionPanel.transform, new Vector2(-200, 140), new Vector2(180, 30), "localhost");
        CreateInputField("ServerPortInput", "Порт:", connectionPanel.transform, new Vector2(0, 140), new Vector2(100, 30), "8888");
        CreateInputField("PlayerNameInput", "Имя игрока:", connectionPanel.transform, new Vector2(-200, 100), new Vector2(180, 30), playerName);
        CreateInputField("StartingStackInput", "Начальный стек:", connectionPanel.transform, new Vector2(0, 100), new Vector2(100, 30), "1000");
        
        // Кнопки подключения
        CreateButton("ConnectButton", "Подключиться", connectionPanel.transform, new Vector2(-150, 60), new Vector2(120, 40), Color.green);
        CreateButton("DisconnectButton", "Отключиться", connectionPanel.transform, new Vector2(0, 60), new Vector2(120, 40), Color.red);
        
        // Статус подключения
        CreateText("ConnectionStatusText", "Статус: Не подключен", connectionPanel.transform, new Vector2(0, 20), new Vector2(300, 30), 16, Color.yellow);
    }
    
    private void CreateGameInfoPanel(Transform parent)
    {
        GameObject gameInfoPanel = CreatePanel("GameInfoPanel", parent);
        RectTransform gameRect = gameInfoPanel.GetComponent<RectTransform>();
        gameRect.anchorMin = new Vector2(0, 0.5f);
        gameRect.anchorMax = new Vector2(1, 0.8f);
        gameRect.sizeDelta = Vector2.zero;
        gameRect.anchoredPosition = Vector2.zero;
        
        // Заголовок панели
        CreateText("GameInfoTitle", "🎮 ИГРОВАЯ ИНФОРМАЦИЯ", gameInfoPanel.transform, new Vector2(0, 120), new Vector2(400, 30), 18, Color.cyan);
        
        // Информация о фазе игры
        CreateText("GamePhaseText", "Фаза: Ожидание игроков", gameInfoPanel.transform, new Vector2(0, 80), new Vector2(300, 25), 16, Color.yellow);
        
        // Информация о банке и ставках
        CreateText("PotText", "Банк: 0", gameInfoPanel.transform, new Vector2(-150, 50), new Vector2(150, 25), 16, Color.white);
        CreateText("CurrentBetText", "Текущая ставка: 0", gameInfoPanel.transform, new Vector2(150, 50), new Vector2(150, 25), 16, Color.white);
        
        // Информация о блайндах
        CreateText("SmallBlindText", "Small Blind: 10", gameInfoPanel.transform, new Vector2(-150, 20), new Vector2(150, 25), 14, Color.gray);
        CreateText("BigBlindText", "Big Blind: 20", gameInfoPanel.transform, new Vector2(150, 20), new Vector2(150, 25), 14, Color.gray);
        
        // Список игроков
        CreateScrollView("PlayersListScrollView", "👥 ИГРОКИ", gameInfoPanel.transform, new Vector2(0, -40), new Vector2(500, 100));
    }
    
    private void CreateActionPanel(Transform parent)
    {
        GameObject actionPanel = CreatePanel("ActionPanel", parent);
        RectTransform actionRect = actionPanel.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0, 0.2f);
        actionRect.anchorMax = new Vector2(1, 0.5f);
        actionRect.sizeDelta = Vector2.zero;
        actionRect.anchoredPosition = Vector2.zero;
        
        // Заголовок панели
        CreateText("ActionTitle", "🎯 ДЕЙСТВИЯ", actionPanel.transform, new Vector2(0, 100), new Vector2(400, 30), 18, Color.cyan);
        
        // Кнопки действий
        CreateButton("FoldButton", "Fold", actionPanel.transform, new Vector2(-200, 50), new Vector2(80, 40), Color.red);
        CreateButton("CallButton", "Call", actionPanel.transform, new Vector2(-100, 50), new Vector2(80, 40), Color.blue);
        CreateButton("RaiseButton", "Raise", actionPanel.transform, new Vector2(0, 50), new Vector2(80, 40), Color.green);
        CreateButton("CheckButton", "Check", actionPanel.transform, new Vector2(100, 50), new Vector2(80, 40), Color.yellow);
        CreateButton("AllInButton", "All-In", actionPanel.transform, new Vector2(200, 50), new Vector2(80, 40), Color.magenta);
        
        // Поле ввода для рейза
        CreateInputField("RaiseAmountInput", "Сумма рейза:", actionPanel.transform, new Vector2(-100, 0), new Vector2(150, 30), "100");
        
        // Слайдер для рейза
        CreateSlider("RaiseSlider", actionPanel.transform, new Vector2(100, 0), new Vector2(200, 30));
    }
    
    private void CreateLogPanel(Transform parent)
    {
        GameObject logPanel = CreatePanel("LogPanel", parent);
        RectTransform logRect = logPanel.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 0);
        logRect.anchorMax = new Vector2(1, 0.2f);
        logRect.sizeDelta = Vector2.zero;
        logRect.anchoredPosition = Vector2.zero;
        
        // Заголовок панели
        CreateText("LogTitle", "📝 ЛОГ ДЕЙСТВИЙ", logPanel.transform, new Vector2(0, 80), new Vector2(400, 30), 18, Color.cyan);
        
        // Лог действий
        CreateScrollView("ActionLogScrollView", "", logPanel.transform, new Vector2(0, 0), new Vector2(600, 120));
    }
    
    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 200);
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        return panel;
    }
    
    private void CreateText(string name, string text, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        
        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Center;
    }
    
    private void CreateInputField(string name, string placeholder, Transform parent, Vector2 position, Vector2 size, string defaultValue)
    {
        GameObject inputGO = new GameObject(name);
        inputGO.transform.SetParent(parent, false);
        
        RectTransform rect = inputGO.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Image image = inputGO.AddComponent<Image>();
        image.color = Color.white;
        
        TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();
        inputField.text = defaultValue;
        
        // Создаем текст для placeholder
        GameObject placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(inputGO.transform, false);
        
        RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        placeholderRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 14;
        placeholderText.color = Color.gray;
        
        inputField.placeholder = placeholderText;
    }
    
    private void CreateButton(string name, string text, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Image image = buttonGO.AddComponent<Image>();
        image.color = color;
        
        Button button = buttonGO.AddComponent<Button>();
        
        // Создаем текст кнопки
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
    }
    
    private void CreateSlider(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject sliderGO = new GameObject(name);
        sliderGO.transform.SetParent(parent, false);
        
        RectTransform rect = sliderGO.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 20;
        slider.maxValue = 1000;
        slider.value = 100;
        
        // Создаем фон слайдера
        GameObject backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(sliderGO.transform, false);
        
        RectTransform bgRect = backgroundGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = backgroundGO.AddComponent<Image>();
        bgImage.color = Color.gray;
        
        // Создаем заполнение
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(backgroundGO.transform, false);
        
        RectTransform fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = Color.green;
        
        // Создаем ручку
        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(sliderGO.transform, false);
        
        RectTransform handleRect = handleGO.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        
        Image handleImage = handleGO.AddComponent<Image>();
        handleImage.color = Color.white;
        
        slider.targetGraphic = handleImage;
    }
    
    private void CreateScrollView(string name, string title, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject scrollGO = new GameObject(name);
        scrollGO.transform.SetParent(parent, false);
        
        RectTransform rect = scrollGO.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        Image scrollImage = scrollGO.AddComponent<Image>();
        scrollImage.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        
        // Создаем Viewport
        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        
        RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        
        Image viewportImage = viewportGO.AddComponent<Image>();
        viewportImage.color = Color.clear;
        Mask mask = viewportGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Создаем Content
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.sizeDelta = new Vector2(0, size.y);
        contentRect.anchoredPosition = Vector2.zero;
        
        // Создаем текст для лога/списка
        GameObject textGO = new GameObject(name.Replace("ScrollView", "Text"));
        textGO.transform.SetParent(contentGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = name.Contains("Players") ? "Список игроков пуст..." : "Лог действий пуст...\n";
        textComponent.fontSize = 12;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.TopLeft;
        
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
    }
    
    private void AddComponents(GameObject mainPanel)
    {
        // Добавляем PokerClientFull
        PokerClientFull pokerClient = mainPanel.AddComponent<PokerClientFull>();
        
        // Добавляем PokerUIControllerFull
        PokerUIControllerFull uiController = mainPanel.AddComponent<PokerUIControllerFull>();
        
        // Добавляем ConnectionManager
        ConnectionManager connectionManager = mainPanel.AddComponent<ConnectionManager>();
        
        // Связываем компоненты
        var uiControllerType = typeof(PokerUIControllerFull);
        var pokerClientField = uiControllerType.GetField("pokerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        pokerClientField?.SetValue(uiController, pokerClient);
        
        var connectionManagerType = typeof(ConnectionManager);
        var pokerClientField2 = connectionManagerType.GetField("pokerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        pokerClientField2?.SetValue(connectionManager, pokerClient);
        
        Debug.Log("✅ Компоненты добавлены и связаны!");
    }
}
