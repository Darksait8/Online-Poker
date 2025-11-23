using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Менеджер настроек подключения к серверу
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private TMP_InputField serverHostInput;
    [SerializeField] private TMP_InputField serverPortInput;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField startingStackInput;
    
    [Header("Кнопки")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button autoFillButton; // Кнопка для ручного автозаполнения
    
    [Header("Ссылки")]
    [SerializeField] private PokerClient pokerClient;
    
    [Header("Панель подключения")]
    [SerializeField] private GameObject connectionPanel; // Панель, которую нужно скрывать/показывать
    [SerializeField] private bool hidePanelOnConnect = true; // Скрывать панель при подключении
    [SerializeField] private bool showPanelOnDisconnect = true; // Показывать панель при отключении
    
    [Header("Автозаполнение")]
    [SerializeField] private bool autoFillOnStart = true; // Автоматически заполнять поля при старте
    [SerializeField] private bool useLocalIP = false; // Использовать локальный IP вместо localhost
    
    private void Start()
    {
        // Загружаем сохраненные настройки
        LoadSettings();
        
        // Синхронизируем адрес сервера с AuthServerSync
        SyncServerAddressWithAuth();
        
        // Настраиваем кнопки
        if (connectButton != null)
            connectButton.onClick.AddListener(ConnectToServer);
            
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(DisconnectFromServer);
        
        if (autoFillButton != null)
            autoFillButton.onClick.AddListener(AutoFillFields);
        
        // Подписываемся на события подключения
        if (pokerClient != null)
        {
            pokerClient.OnConnectionStatusChanged += HandleConnectionStatusChanged;
        }
        
        // Обновляем состояние кнопок
        UpdateButtonStates();
        
        // Находим панель автоматически, если не назначена
        if (connectionPanel == null)
        {
            FindConnectionPanel();
        }
        
        // Автозаполнение полей при старте
        if (autoFillOnStart)
        {
            AutoFillFields();
        }
    }
    
    /// <summary>
    /// Автоматически заполняет поля подключения умными значениями
    /// </summary>
    public void AutoFillFields()
    {
        bool changed = false;
        
        // Автозаполнение IP-адреса (всегда заполняем, если пусто)
        if (serverHostInput != null)
        {
            if (string.IsNullOrEmpty(serverHostInput.text.Trim()))
            {
                if (useLocalIP)
                {
                    string localIP = ServerDiscoveryHelper.GetLocalIPAddress();
                    if (localIP != "localhost" && !string.IsNullOrEmpty(localIP))
                    {
                        serverHostInput.text = localIP;
                        changed = true;
                    }
                    else
                    {
                        serverHostInput.text = "localhost";
                        changed = true;
                    }
                }
                else
                {
                    serverHostInput.text = "localhost";
                    changed = true;
                }
            }
        }
        
        // Автозаполнение порта (всегда заполняем, если пусто)
        if (serverPortInput != null)
        {
            if (string.IsNullOrEmpty(serverPortInput.text.Trim()))
            {
                serverPortInput.text = "8888";
                changed = true;
            }
        }
        
        // Автозаполнение имени игрока (всегда заполняем, если пусто)
        if (playerNameInput != null)
        {
            if (string.IsNullOrEmpty(playerNameInput.text.Trim()))
            {
                var currentUser = AuthManager.CurrentUser;
                if (currentUser != null && !string.IsNullOrEmpty(currentUser.username))
                {
                    playerNameInput.text = currentUser.username;
                }
                else
                {
                    playerNameInput.text = "Игрок";
                }
                changed = true;
            }
        }
        
        // Автозаполнение начального стека (всегда заполняем, если пусто)
        if (startingStackInput != null)
        {
            if (string.IsNullOrEmpty(startingStackInput.text.Trim()))
            {
                var currentUser = AuthManager.CurrentUser;
                if (currentUser != null && currentUser.chips > 0)
                {
                    startingStackInput.text = currentUser.chips.ToString();
                }
                else
                {
                    startingStackInput.text = "1000";
                }
                changed = true;
            }
        }
        
        // Сохраняем изменения
        if (changed)
        {
            SaveSettings();
            UpdateClientSettings();
            Debug.Log("✅ Поля подключения автоматически заполнены");
        }
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (pokerClient != null)
        {
            pokerClient.OnConnectionStatusChanged -= HandleConnectionStatusChanged;
        }
    }
    
    private void FindConnectionPanel()
    {
        // Пытаемся найти панель по имени
        GameObject foundPanel = GameObject.Find("ConnectionPanel");
        if (foundPanel == null)
        {
            // Ищем родительский объект с полями ввода
            if (serverHostInput != null)
            {
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
        }
        else
        {
            connectionPanel = foundPanel;
        }
    }
    
    private void HandleConnectionStatusChanged(string status)
    {
        // Обновляем состояние кнопок
        UpdateButtonStates();
        
        // Управляем видимостью панели
        if (connectionPanel != null)
        {
            if (status.Contains("Подключен") && hidePanelOnConnect)
            {
                // Скрываем панель при успешном подключении
                connectionPanel.SetActive(false);
                Debug.Log("✅ Панель подключения скрыта после успешного подключения");
            }
            else if ((status.Contains("Отключен") || status.Contains("Ошибка")) && showPanelOnDisconnect)
            {
                // Показываем панель при отключении или ошибке
                connectionPanel.SetActive(true);
            }
        }
    }
    
    private void LoadSettings()
    {
        // Загружаем настройки из PlayerPrefs или используем умные значения по умолчанию
        string savedHost = PlayerPrefs.GetString("ServerHost", "");
        int savedPort = PlayerPrefs.GetInt("ServerPort", 8888);
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        int savedStack = PlayerPrefs.GetInt("StartingStack", 1000);
        
        // Если пользователь авторизован, используем его имя и баланс
        if (AuthManager.IsLoggedIn && AuthManager.CurrentUser != null)
        {
            savedName = AuthManager.CurrentUser.username;
            savedStack = AuthManager.CurrentUser.chips;
            Debug.Log($"✅ Загружены настройки из профиля пользователя: {savedName}, баланс: {savedStack}");
        }
        
        // Автозаполнение IP-адреса
        if (serverHostInput != null)
        {
            if (string.IsNullOrEmpty(savedHost))
            {
                // Используем умное значение по умолчанию
                if (useLocalIP)
                {
                    // Пытаемся определить локальный IP
                    string localIP = ServerDiscoveryHelper.GetLocalIPAddress();
                    if (localIP != "localhost" && !string.IsNullOrEmpty(localIP))
                    {
                        serverHostInput.text = localIP;
                    }
                    else
                    {
                        serverHostInput.text = "localhost";
                    }
                }
                else
                {
                    serverHostInput.text = "localhost";
                }
            }
            else
            {
                serverHostInput.text = savedHost;
            }
        }
        
        // Автозаполнение порта
        if (serverPortInput != null)
        {
            serverPortInput.text = savedPort.ToString();
        }
        
        // Автозаполнение имени игрока
        if (playerNameInput != null)
        {
            if (string.IsNullOrEmpty(savedName))
            {
                // Пытаемся взять имя из профиля
                var currentUser = AuthManager.CurrentUser;
                if (currentUser != null && !string.IsNullOrEmpty(currentUser.username))
                {
                    playerNameInput.text = currentUser.username;
                }
                else
                {
                    playerNameInput.text = "Игрок";
                }
            }
            else
            {
                playerNameInput.text = savedName;
            }
        }
        
        // Автозаполнение начального стека
        if (startingStackInput != null)
        {
            if (savedStack <= 0)
            {
                // Пытаемся взять баланс из профиля
                var currentUser = AuthManager.CurrentUser;
                if (currentUser != null && currentUser.chips > 0)
                {
                    startingStackInput.text = currentUser.chips.ToString();
                }
                else
                {
                    startingStackInput.text = "1000";
                }
            }
            else
            {
                startingStackInput.text = savedStack.ToString();
            }
        }
        
        // Сохраняем автозаполненные значения
        if (autoFillOnStart)
        {
            SaveSettings();
        }
    }
    
    private void SaveSettings()
    {
        // Сохраняем настройки в PlayerPrefs
        if (serverHostInput != null)
            PlayerPrefs.SetString("ServerHost", serverHostInput.text);
            
        if (serverPortInput != null && int.TryParse(serverPortInput.text, out int port))
            PlayerPrefs.SetInt("ServerPort", port);
            
        if (playerNameInput != null)
            PlayerPrefs.SetString("PlayerName", playerNameInput.text);
            
        if (startingStackInput != null && int.TryParse(startingStackInput.text, out int stack))
            PlayerPrefs.SetInt("StartingStack", stack);
        
        PlayerPrefs.Save();
    }
    
    public void ConnectToServer()
    {
        if (pokerClient == null)
        {
            Debug.LogError("❌ PokerClient не назначен!");
            return;
        }
        
        // Сохраняем настройки
        SaveSettings();
        
        // Обновляем настройки клиента
        UpdateClientSettings();
        
        // Проверяем, что поля заполнены
        string host = GetServerHost();
        int port = GetServerPort();
        
        if (string.IsNullOrEmpty(host))
        {
            Debug.LogError("❌ IP-адрес сервера не указан!");
            return;
        }
        
        Debug.Log($"🔌 Попытка подключения к серверу {host}:{port}...");
        
        // Подключаемся к серверу
        pokerClient.ConnectToServer();
        
        // Обновляем состояние кнопок через небольшую задержку
        Invoke(nameof(UpdateButtonStates), 0.5f);
    }
    
    public void DisconnectFromServer()
    {
        if (pokerClient != null)
        {
            pokerClient.DisconnectFromServer();
        }
        
        // Показываем панель при отключении
        if (connectionPanel != null && showPanelOnDisconnect)
        {
            connectionPanel.SetActive(true);
        }
        
        // Обновляем состояние кнопок
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Показывает панель подключения
    /// </summary>
    public void ShowConnectionPanel()
    {
        if (connectionPanel != null)
        {
            connectionPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Скрывает панель подключения
    /// </summary>
    public void HideConnectionPanel()
    {
        if (connectionPanel != null)
        {
            connectionPanel.SetActive(false);
        }
    }
    
    private void UpdateClientSettings()
    {
        if (pokerClient == null) return;
        
        // Обновляем настройки через рефлексию
        var clientType = typeof(PokerClient);
        
        if (serverHostInput != null)
        {
            var hostField = clientType.GetField("serverHost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            hostField?.SetValue(pokerClient, serverHostInput.text);
        }
        
        if (serverPortInput != null && int.TryParse(serverPortInput.text, out int port))
        {
            var portField = clientType.GetField("serverPort", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            portField?.SetValue(pokerClient, port);
        }
        
        // Используем имя и баланс из авторизованного пользователя, если доступно
        string nameToUse = playerNameInput != null ? playerNameInput.text : "Игрок";
        int stackToUse = 1000;
        
        if (AuthManager.IsLoggedIn && AuthManager.CurrentUser != null)
        {
            nameToUse = AuthManager.CurrentUser.username;
            stackToUse = AuthManager.CurrentUser.chips;
            Debug.Log($"✅ Используются данные авторизованного пользователя для подключения: {nameToUse}, баланс: {stackToUse}");
        }
        else if (startingStackInput != null && int.TryParse(startingStackInput.text, out int parsedStack))
        {
            stackToUse = parsedStack;
        }
        
        if (playerNameInput != null)
        {
            var nameField = clientType.GetField("playerName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nameField?.SetValue(pokerClient, nameToUse);
        }
        
        var stackField = clientType.GetField("startingStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        stackField?.SetValue(pokerClient, stackToUse);
    }
    
    private void UpdateButtonStates()
    {
        bool isConnected = pokerClient != null && pokerClient.IsConnected();
        
        if (connectButton != null)
            connectButton.interactable = !isConnected;
            
        if (disconnectButton != null)
            disconnectButton.interactable = isConnected;
        
        // Управляем видимостью панели на основе состояния подключения
        if (connectionPanel != null && hidePanelOnConnect)
        {
            // Скрываем панель если подключены, показываем если не подключены
            if (isConnected)
            {
                connectionPanel.SetActive(false);
            }
            else if (showPanelOnDisconnect)
            {
                connectionPanel.SetActive(true);
            }
        }
    }
    
    private void Update()
    {
        // Обновляем состояние кнопок каждые несколько секунд
        if (Time.frameCount % 60 == 0) // Каждую секунду при 60 FPS
        {
            UpdateButtonStates();
        }
    }
    
    [ContextMenu("Reset Settings")]
    public void ResetSettings()
    {
        PlayerPrefs.DeleteAll();
        LoadSettings();
        Debug.Log("🔄 Настройки сброшены к значениям по умолчанию");
    }
    
    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        UpdateClientSettings();
        Debug.Log($"🔧 Настройки обновлены: {serverHostInput?.text}:{serverPortInput?.text}");
    }
    
    /// <summary>
    /// Устанавливает IP-адрес сервера программно
    /// </summary>
    public void SetServerAddress(string host, int port = 8888)
    {
        if (serverHostInput != null)
            serverHostInput.text = host;
            
        if (serverPortInput != null)
            serverPortInput.text = port.ToString();
        
        SaveSettings();
        UpdateClientSettings();
    }
    
    /// <summary>
    /// Получает текущий IP-адрес сервера
    /// </summary>
    public string GetServerHost()
    {
        return serverHostInput != null ? serverHostInput.text : "localhost";
    }
    
    /// <summary>
    /// Получает текущий порт сервера
    /// </summary>
    public int GetServerPort()
    {
        if (serverPortInput != null && int.TryParse(serverPortInput.text, out int port))
            return port;
        return 8888;
    }
    
    /// <summary>
    /// Синхронизирует адрес сервера с AuthServerSync
    /// </summary>
    private void SyncServerAddressWithAuth()
    {
        AuthServerSync authSync = FindObjectOfType<AuthServerSync>();
        if (authSync != null)
        {
            string host = GetServerHost();
            int port = GetServerPort();
            authSync.SetServerAddress(host, port);
            Debug.Log($"✅ Адрес сервера синхронизирован с AuthServerSync: {host}:{port}");
        }
    }
}
