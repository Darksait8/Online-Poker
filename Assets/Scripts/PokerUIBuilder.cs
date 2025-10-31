using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Автоматическое создание UI для покерного клиента
/// </summary>
public class PokerUIBuilder : MonoBehaviour
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
    
    [ContextMenu("Create Poker UI")]
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
        CreateText("Title", "🎰 Покерная игра", mainPanel.transform, new Vector2(0, 200), new Vector2(400, 50), 24, Color.white);
        
        // Создаем панель подключения
        GameObject connectionPanel = CreatePanel("ConnectionPanel", mainPanel.transform);
        RectTransform connRect = connectionPanel.GetComponent<RectTransform>();
        connRect.anchorMin = new Vector2(0, 0.7f);
        connRect.anchorMax = new Vector2(1, 1);
        connRect.sizeDelta = Vector2.zero;
        connRect.anchoredPosition = Vector2.zero;
        
        // Создаем поля ввода
        CreateInputField("ServerHostInput", "IP сервера:", connectionPanel.transform, new Vector2(-100, 150), new Vector2(200, 30), "localhost");
        CreateInputField("ServerPortInput", "Порт:", connectionPanel.transform, new Vector2(150, 150), new Vector2(100, 30), "8888");
        CreateInputField("PlayerNameInput", "Имя игрока:", connectionPanel.transform, new Vector2(-100, 100), new Vector2(200, 30), playerName);
        CreateInputField("StartingStackInput", "Начальный стек:", connectionPanel.transform, new Vector2(150, 100), new Vector2(100, 30), "1000");
        
        // Создаем кнопки подключения
        CreateButton("ConnectButton", "Подключиться", connectionPanel.transform, new Vector2(-100, 50), new Vector2(120, 40), Color.green);
        CreateButton("DisconnectButton", "Отключиться", connectionPanel.transform, new Vector2(50, 50), new Vector2(120, 40), Color.red);
        
        // Создаем панель статуса
        GameObject statusPanel = CreatePanel("StatusPanel", mainPanel.transform);
        RectTransform statusRect = statusPanel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.4f);
        statusRect.anchorMax = new Vector2(1, 0.7f);
        statusRect.sizeDelta = Vector2.zero;
        statusRect.anchoredPosition = Vector2.zero;
        
        // Создаем тексты статуса
        CreateText("ConnectionStatusText", "Статус: Не подключен", statusPanel.transform, new Vector2(0, 50), new Vector2(300, 30), 16, Color.yellow);
        CreateText("PlayersListText", "Игроки: Нет подключенных игроков", statusPanel.transform, new Vector2(0, 10), new Vector2(300, 30), 14, Color.white);
        CreateText("GameStateText", "Состояние: Ожидание игроков", statusPanel.transform, new Vector2(0, -30), new Vector2(300, 30), 14, Color.yellow);
        
        // Создаем панель действий
        GameObject actionPanel = CreatePanel("ActionPanel", mainPanel.transform);
        RectTransform actionRect = actionPanel.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0, 0);
        actionRect.anchorMax = new Vector2(1, 0.4f);
        actionRect.sizeDelta = Vector2.zero;
        actionRect.anchoredPosition = Vector2.zero;
        
        // Создаем кнопки действий
        CreateButton("FoldButton", "Fold", actionPanel.transform, new Vector2(-150, 50), new Vector2(80, 40), Color.red);
        CreateButton("CallButton", "Call", actionPanel.transform, new Vector2(-50, 50), new Vector2(80, 40), Color.blue);
        CreateButton("RaiseButton", "Raise", actionPanel.transform, new Vector2(50, 50), new Vector2(80, 40), Color.green);
        CreateButton("CheckButton", "Check", actionPanel.transform, new Vector2(150, 50), new Vector2(80, 40), Color.yellow);
        
        // Создаем поле ввода для рейза
        CreateInputField("RaiseAmountInput", "Сумма рейза:", actionPanel.transform, new Vector2(0, 0), new Vector2(150, 30), "100");
        
        // Создаем лог действий
        CreateScrollView("ActionLogScrollView", "Лог действий:", actionPanel.transform, new Vector2(0, -80), new Vector2(400, 120));
        
        // Добавляем компоненты
        AddComponents(mainPanel);
        
        Debug.Log("✅ UI для покерной игры создан!");
    }
    
    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 200);
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
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
    
    private void CreateScrollView(string name, string title, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject scrollGO = new GameObject(name);
        scrollGO.transform.SetParent(parent, false);
        
        RectTransform rect = scrollGO.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        Image scrollImage = scrollGO.AddComponent<Image>();
        scrollImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
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
        
        // Создаем текст для лога
        GameObject logTextGO = new GameObject("ActionLogText");
        logTextGO.transform.SetParent(contentGO.transform, false);
        
        RectTransform logTextRect = logTextGO.AddComponent<RectTransform>();
        logTextRect.anchorMin = Vector2.zero;
        logTextRect.anchorMax = Vector2.one;
        logTextRect.sizeDelta = Vector2.zero;
        logTextRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI logText = logTextGO.AddComponent<TextMeshProUGUI>();
        logText.text = "Лог действий пуст...\n";
        logText.fontSize = 12;
        logText.color = Color.white;
        logText.alignment = TextAlignmentOptions.TopLeft;
        
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
    }
    
    private void AddComponents(GameObject mainPanel)
    {
        // Добавляем PokerClient
        PokerClient pokerClient = mainPanel.AddComponent<PokerClient>();
        
        // Добавляем PokerUIController
        PokerUIController uiController = mainPanel.AddComponent<PokerUIController>();
        
        // Добавляем ConnectionManager
        ConnectionManager connectionManager = mainPanel.AddComponent<ConnectionManager>();
        
        // Связываем компоненты
        var uiControllerType = typeof(PokerUIController);
        var pokerClientField = uiControllerType.GetField("pokerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        pokerClientField?.SetValue(uiController, pokerClient);
        
        var connectionManagerType = typeof(ConnectionManager);
        var pokerClientField2 = connectionManagerType.GetField("pokerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        pokerClientField2?.SetValue(connectionManager, pokerClient);
        
        Debug.Log("✅ Компоненты добавлены и связаны!");
    }
}
