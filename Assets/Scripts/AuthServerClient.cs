using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Клиент для работы с сервером авторизации
/// Синхронизирует аккаунты через сервер
/// </summary>
public class AuthServerClient : MonoBehaviour
{
    [Header("Настройки подключения")]
    [SerializeField] private string serverHost = "localhost";
    [SerializeField] private int serverPort = 8888;
    
    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;
    
    // События
    public System.Action<bool, string, Dictionary<string, object>> OnRegisterResponse;
    public System.Action<bool, string, Dictionary<string, object>> OnLoginResponse;
    public System.Action<bool, Dictionary<string, object>> OnProfileResponse;
    public System.Action<bool, string> OnUpdateResponse;
    
    private void OnDestroy()
    {
        Disconnect();
    }
    
    /// <summary>
    /// Подключение к серверу
    /// </summary>
    public void Connect()
    {
        if (isConnected)
            return;
        
        try
        {
            tcpClient = new TcpClient();
            var connectResult = tcpClient.BeginConnect(serverHost, serverPort, null, null);
            if (!connectResult.AsyncWaitHandle.WaitOne(System.TimeSpan.FromSeconds(5)))
            {
                throw new System.TimeoutException("Превышено время ожидания");
            }
            tcpClient.EndConnect(connectResult);
            
            stream = tcpClient.GetStream();
            isConnected = true;
            
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            if (enableDebugLogs)
                Debug.Log($"✅ Подключен к серверу авторизации {serverHost}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Ошибка подключения к серверу авторизации: {e.Message}");
            isConnected = false;
        }
    }
    
    /// <summary>
    /// Отключение от сервера
    /// </summary>
    public void Disconnect()
    {
        isConnected = false;
        
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        
        try
        {
            stream?.Close();
            tcpClient?.Close();
        }
        catch { }
    }
    
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public void Register(string username, string email, string password)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "auth_register"},
            {"username", username},
            {"email", email},
            {"password", password}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Вход пользователя
    /// </summary>
    public void Login(string username, string password)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "auth_login"},
            {"username", username},
            {"password", password}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Получение профиля пользователя
    /// </summary>
    public void GetProfile(string username)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "auth_get_profile"},
            {"username", username}
        };
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Обновление профиля пользователя
    /// </summary>
    public void UpdateProfile(string username, int? chips = null, int? xp = null, int? level = null)
    {
        if (!EnsureConnected())
            return;
        
        var message = new Dictionary<string, object>
        {
            {"type", "auth_update_profile"},
            {"username", username}
        };
        
        if (chips.HasValue)
            message["chips"] = chips.Value;
        if (xp.HasValue)
            message["xp"] = xp.Value;
        if (level.HasValue)
            message["level"] = level.Value;
        
        SendMessage(message);
    }
    
    private bool EnsureConnected()
    {
        if (!isConnected || tcpClient == null || !tcpClient.Connected)
        {
            Connect();
            return isConnected;
        }
        return true;
    }
    
    private void SendMessage(Dictionary<string, object> message)
    {
        if (!isConnected || stream == null)
        {
            Debug.LogWarning("⚠️ Нет подключения к серверу авторизации");
            return;
        }
        
        try
        {
            string json = SimpleJSONParser.ToJSON(message);
            byte[] data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Ошибка отправки сообщения: {e.Message}");
        }
    }
    
    private void ReceiveMessages()
    {
        byte[] buffer = new byte[4096];
        
        while (isConnected && tcpClient != null && tcpClient.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;
                
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ProcessMessage(message);
            }
            catch (Exception ex)
            {
                if (isConnected)
                {
                    Debug.LogError($"❌ Ошибка получения сообщения: {ex.Message}");
                }
                break;
            }
        }
    }
    
    private void ProcessMessage(string message)
    {
        try
        {
            var data = SimpleJSONParser.FromJSON(message);
            string messageType = data.ContainsKey("type") ? data["type"].ToString() : "";
            
            if (enableDebugLogs)
                Debug.Log($"📨 Получено сообщение от сервера авторизации: {messageType}");
            
            switch (messageType)
            {
                case "auth_register_response":
                    bool regSuccess = data.ContainsKey("success") && Convert.ToBoolean(data["success"]);
                    string regMessage = data.ContainsKey("message") ? data["message"].ToString() : "";
                    OnRegisterResponse?.Invoke(regSuccess, regMessage, data);
                    break;
                    
                case "auth_login_response":
                    bool loginSuccess = data.ContainsKey("success") && Convert.ToBoolean(data["success"]);
                    string loginMessage = data.ContainsKey("message") ? data["message"].ToString() : "";
                    OnLoginResponse?.Invoke(loginSuccess, loginMessage, data);
                    break;
                    
                case "auth_profile_response":
                    bool profileSuccess = data.ContainsKey("success") && Convert.ToBoolean(data["success"]);
                    OnProfileResponse?.Invoke(profileSuccess, data);
                    break;
                    
                case "auth_update_response":
                    bool updateSuccess = data.ContainsKey("success") && Convert.ToBoolean(data["success"]);
                    string updateMessage = data.ContainsKey("message") ? data["message"].ToString() : "";
                    OnUpdateResponse?.Invoke(updateSuccess, updateMessage);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Ошибка обработки сообщения: {e.Message}");
        }
    }
    
    public void SetServerAddress(string host, int port)
    {
        serverHost = host;
        serverPort = port;
    }
    
    public bool IsConnected()
    {
        return isConnected && tcpClient != null && tcpClient.Connected;
    }
}

