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
    
    [Header("Ссылки")]
    [SerializeField] private PokerClient pokerClient;
    
    private void Start()
    {
        // Загружаем сохраненные настройки
        LoadSettings();
        
        // Настраиваем кнопки
        if (connectButton != null)
            connectButton.onClick.AddListener(ConnectToServer);
            
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(DisconnectFromServer);
        
        // Обновляем состояние кнопок
        UpdateButtonStates();
    }
    
    private void LoadSettings()
    {
        // Загружаем настройки из PlayerPrefs
        string savedHost = PlayerPrefs.GetString("ServerHost", "localhost");
        int savedPort = PlayerPrefs.GetInt("ServerPort", 8888);
        string savedName = PlayerPrefs.GetString("PlayerName", "Unity Player");
        int savedStack = PlayerPrefs.GetInt("StartingStack", 1000);
        
        // Устанавливаем значения в поля ввода
        if (serverHostInput != null)
            serverHostInput.text = savedHost;
            
        if (serverPortInput != null)
            serverPortInput.text = savedPort.ToString();
            
        if (playerNameInput != null)
            playerNameInput.text = savedName;
            
        if (startingStackInput != null)
            startingStackInput.text = savedStack.ToString();
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
        
        // Подключаемся к серверу
        pokerClient.ConnectToServer();
        
        // Обновляем состояние кнопок
        UpdateButtonStates();
    }
    
    public void DisconnectFromServer()
    {
        if (pokerClient != null)
        {
            pokerClient.DisconnectFromServer();
        }
        
        // Обновляем состояние кнопок
        UpdateButtonStates();
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
        
        if (playerNameInput != null)
        {
            var nameField = clientType.GetField("playerName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nameField?.SetValue(pokerClient, playerNameInput.text);
        }
        
        if (startingStackInput != null && int.TryParse(startingStackInput.text, out int stack))
        {
            var stackField = clientType.GetField("startingStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stackField?.SetValue(pokerClient, stack);
        }
    }
    
    private void UpdateButtonStates()
    {
        bool isConnected = pokerClient != null && pokerClient.IsConnected();
        
        if (connectButton != null)
            connectButton.interactable = !isConnected;
            
        if (disconnectButton != null)
            disconnectButton.interactable = isConnected;
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
}
