using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Автоматическая настройка ConnectionManager в сцене
/// Находит UI элементы и подключает их к ConnectionManager
/// </summary>
public class ConnectionManagerSetup : MonoBehaviour
{
    [Header("Автоматическая настройка")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool createUIIfMissing = true;
    
    [Header("Ручная настройка (если нужно)")]
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private PokerClient pokerClient;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupConnectionManager();
        }
    }
    
    [ContextMenu("Setup Connection Manager")]
    public void SetupConnectionManager()
    {
        // Находим или создаем ConnectionManager
        if (connectionManager == null)
        {
            connectionManager = FindObjectOfType<ConnectionManager>();
            if (connectionManager == null)
            {
                GameObject managerGO = new GameObject("ConnectionManager");
                connectionManager = managerGO.AddComponent<ConnectionManager>();
                Debug.Log("✅ ConnectionManager создан");
            }
        }
        
        // Находим или создаем PokerClient
        if (pokerClient == null)
        {
            pokerClient = FindObjectOfType<PokerClient>();
            if (pokerClient == null)
            {
                GameObject clientGO = new GameObject("PokerClient");
                pokerClient = clientGO.AddComponent<PokerClient>();
                Debug.Log("✅ PokerClient создан");
            }
        }
        
        // Подключаем PokerClient к ConnectionManager через рефлексию
        var connectionManagerType = typeof(ConnectionManager);
        var pokerClientField = connectionManagerType.GetField("pokerClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        pokerClientField?.SetValue(connectionManager, pokerClient);
        
        // Находим UI элементы
        FindAndConnectUIElements();
        
        // Создаем UI если его нет
        if (createUIIfMissing && !HasUIElements())
        {
            CreateConnectionUI();
            FindAndConnectUIElements();
        }
        
        Debug.Log("✅ ConnectionManager настроен!");
    }
    
    private void FindAndConnectUIElements()
    {
        if (connectionManager == null) return;
        
        var managerType = typeof(ConnectionManager);
        
        // Ищем поля ввода
        TMP_InputField serverHostInput = GameObject.Find("ServerHostInput")?.GetComponent<TMP_InputField>();
        TMP_InputField serverPortInput = GameObject.Find("ServerPortInput")?.GetComponent<TMP_InputField>();
        TMP_InputField playerNameInput = GameObject.Find("PlayerNameInput")?.GetComponent<TMP_InputField>();
        TMP_InputField startingStackInput = GameObject.Find("StartingStackInput")?.GetComponent<TMP_InputField>();
        
        // Ищем кнопки
        Button connectButton = GameObject.Find("ConnectButton")?.GetComponent<Button>();
        Button disconnectButton = GameObject.Find("DisconnectButton")?.GetComponent<Button>();
        
        // Подключаем через рефлексию
        if (serverHostInput != null)
        {
            var field = managerType.GetField("serverHostInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(connectionManager, serverHostInput);
        }
        
        if (serverPortInput != null)
        {
            var field = managerType.GetField("serverPortInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(connectionManager, serverPortInput);
        }
        
        if (playerNameInput != null)
        {
            var field = managerType.GetField("playerNameInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(connectionManager, playerNameInput);
        }
        
        if (startingStackInput != null)
        {
            var field = managerType.GetField("startingStackInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(connectionManager, startingStackInput);
        }
        
        if (connectButton != null)
        {
            var field = managerType.GetField("connectButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(connectionManager, connectButton);
        }
        
        if (disconnectButton != null)
        {
            var field = managerType.GetField("disconnectButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(connectionManager, disconnectButton);
        }
        
        // Находим и назначаем панель подключения
        GameObject connectionPanel = GameObject.Find("ConnectionPanel");
        if (connectionPanel == null && serverHostInput != null)
        {
            // Ищем родительский объект панели
            Transform parent = serverHostInput.transform.parent;
            while (parent != null)
            {
                if (parent.name.Contains("Panel") || parent.name.Contains("Connection"))
                {
                    connectionPanel = parent.gameObject;
                    break;
                }
                parent = parent.parent;
            }
        }
        
        if (connectionPanel != null)
        {
            var panelField = managerType.GetField("connectionPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            panelField?.SetValue(connectionManager, connectionPanel);
            Debug.Log("✅ Панель подключения назначена");
        }
        
        Debug.Log("✅ UI элементы подключены к ConnectionManager");
    }
    
    private bool HasUIElements()
    {
        return GameObject.Find("ServerHostInput") != null ||
               GameObject.Find("ConnectButton") != null;
    }
    
    private void CreateConnectionUI()
    {
        // Находим или создаем Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Создаем панель подключения
        GameObject connectionPanel = new GameObject("ConnectionPanel");
        connectionPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = connectionPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500, 300);
        panelRect.anchoredPosition = new Vector2(0, 100);
        
        Image panelImage = connectionPanel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Заголовок (без эмодзи для совместимости)
        CreateLabel("ConnectionTitle", "ПОДКЛЮЧЕНИЕ К СЕРВЕРУ", connectionPanel.transform, 
            new Vector2(0, 120), new Vector2(450, 40), 20, Color.cyan);
        
        // Поля ввода
        CreateInputFieldWithLabel("ServerHostInput", "IP сервера:", connectionPanel.transform, 
            new Vector2(-150, 60), new Vector2(200, 30), "localhost");
        CreateInputFieldWithLabel("ServerPortInput", "Порт:", connectionPanel.transform, 
            new Vector2(150, 60), new Vector2(100, 30), "8888");
        CreateInputFieldWithLabel("PlayerNameInput", "Имя игрока:", connectionPanel.transform, 
            new Vector2(-150, 10), new Vector2(200, 30), "Игрок");
        CreateInputFieldWithLabel("StartingStackInput", "Начальный стек:", connectionPanel.transform, 
            new Vector2(150, 10), new Vector2(100, 30), "1000");
        
        // Кнопки
        CreateButton("ConnectButton", "Подключиться", connectionPanel.transform, 
            new Vector2(-100, -50), new Vector2(150, 40), Color.green);
        CreateButton("DisconnectButton", "Отключиться", connectionPanel.transform, 
            new Vector2(100, -50), new Vector2(150, 40), Color.red);
        
        Debug.Log("✅ UI для подключения создан");
    }
    
    private void CreateLabel(string name, string text, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject labelGO = new GameObject(name);
        labelGO.transform.SetParent(parent, false);
        RectTransform rect = labelGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        
        TextMeshProUGUI textComp = labelGO.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = fontSize;
        textComp.color = color;
        textComp.alignment = TextAlignmentOptions.Center;
    }
    
    private void CreateInputFieldWithLabel(string fieldName, string labelText, Transform parent, Vector2 position, Vector2 size, string placeholder)
    {
        // Метка
        GameObject labelGO = new GameObject(fieldName + "Label");
        labelGO.transform.SetParent(parent, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(1f, 0.5f);
        labelRect.sizeDelta = new Vector2(120, 25);
        labelRect.anchoredPosition = new Vector2(position.x - size.x / 2 - 10, position.y);
        
        TextMeshProUGUI labelTextComp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTextComp.text = labelText;
        labelTextComp.fontSize = 14;
        labelTextComp.color = Color.white;
        labelTextComp.alignment = TextAlignmentOptions.Right;
        
        // Поле ввода
        GameObject inputGO = new GameObject(fieldName);
        inputGO.transform.SetParent(parent, false);
        RectTransform inputRect = inputGO.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = size;
        inputRect.anchoredPosition = position;
        
        Image inputImage = inputGO.AddComponent<Image>();
        inputImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();
        
        // Placeholder
        GameObject placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(inputGO.transform, false);
        RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 14;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.alignment = TextAlignmentOptions.Left;
        
        // Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(inputGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);
        
        TextMeshProUGUI textComp = textGO.AddComponent<TextMeshProUGUI>();
        textComp.fontSize = 14;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Left;
        
        inputField.textViewport = textRect;
        inputField.textComponent = textComp;
        inputField.placeholder = placeholderText;
    }
    
    private void CreateButton(string name, string text, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = color;
        
        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // Текст кнопки
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI textComp = textGO.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = 16;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;
    }
}

