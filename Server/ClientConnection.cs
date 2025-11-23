using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PokerServer
{
    /// <summary>
    /// Представляет подключение клиента к серверу
    /// </summary>
    public class ClientConnection
    {
        private TcpClient tcpClient;
        private NetworkStream stream;
        private PokerServer server;
        private bool isConnected = true;
        
        public string ClientId { get; set; }
        public string PlayerName { get; set; }
        public int Stack { get; set; }
        public bool IsConnected => isConnected && tcpClient?.Connected == true;
        
        public ClientConnection(TcpClient tcpClient, PokerServer server)
        {
            this.tcpClient = tcpClient;
            this.stream = tcpClient.GetStream();
            this.server = server;
        }
        
        public void HandleClient()
        {
            byte[] buffer = new byte[4096];
            
            while (isConnected && tcpClient.Connected)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break; // Клиент отключился
                    }
                    
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ProcessMessage(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка чтения от клиента {PlayerName}: {ex.Message}");
                    break;
                }
            }
            
            Close();
        }
        
        private void ProcessMessage(string message)
        {
            try
            {
                var data = ParseJSON(message);
                string messageType = data.ContainsKey("type") ? data["type"].ToString() : "";
                
                Console.WriteLine($"📨 Получено сообщение от {PlayerName}: {messageType}");
                
                switch (messageType)
                {
                    case "auth_register":
                        server.HandleAuthRegister(this, data);
                        break;
                        
                    case "auth_login":
                        server.HandleAuthLogin(this, data);
                        break;
                        
                    case "auth_get_profile":
                        server.HandleAuthGetProfile(this, data);
                        break;
                        
                    case "auth_update_profile":
                        server.HandleAuthUpdateProfile(this, data);
                        break;
                        
                    case "auth_get_all_users":
                        server.HandleAuthGetAllUsers(this, data);
                        break;
                    case "friend_send_request":
                        server.HandleFriendSendRequest(this, data);
                        break;
                    case "friend_accept_request":
                        server.HandleFriendAcceptRequest(this, data);
                        break;
                    case "friend_decline_request":
                        server.HandleFriendDeclineRequest(this, data);
                        break;
                    case "friend_cancel_request":
                        server.HandleFriendCancelRequest(this, data);
                        break;
                    case "friend_remove":
                        server.HandleFriendRemove(this, data);
                        break;
                    case "friend_get_data":
                        server.HandleFriendGetData(this, data);
                        break;
                    case "register_for_notifications":
                        server.HandleRegisterForNotifications(this, data);
                        break;
                        
                    case "join":
                        server.HandleJoinRequest(this, data);
                        break;
                        
                    case "action":
                        server.HandlePlayerAction(this, data);
                        break;
                        
                    case "get_state":
                        // Отправляем текущее состояние игры
                        var state = new Dictionary<string, object>
                        {
                            {"type", "game_state"},
                            {"hand_active", false}
                        };
                        SendMessage(state);
                        break;
                        
                    default:
                        Console.WriteLine($"⚠️ Неизвестный тип сообщения: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки сообщения: {ex.Message}");
            }
        }
        
        public void SendMessage(Dictionary<string, object> message)
        {
            string json = SerializeJSON(message);
            byte[] data = Encoding.UTF8.GetBytes(json);
            Send(data);
        }
        
        public void Send(byte[] data)
        {
            try
            {
                if (isConnected && stream != null && stream.CanWrite)
                {
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки данных клиенту {PlayerName}: {ex.Message}");
                isConnected = false;
            }
        }
        
        public void Close()
        {
            isConnected = false;
            try
            {
                stream?.Close();
                tcpClient?.Close();
            }
            catch { }
            
            server.RemoveClient(this);
        }
        
        private Dictionary<string, object> ParseJSON(string json)
        {
            var result = new Dictionary<string, object>();
            json = json.Trim();
            
            if (!json.StartsWith("{") || !json.EndsWith("}"))
                throw new ArgumentException("Invalid JSON format");
            
            json = json.Substring(1, json.Length - 2); // Remove { }
            
            var parts = SplitJSON(json);
            foreach (var part in parts)
            {
                var keyValue = part.Split(new char[] { ':' }, 2);
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim().Trim('"');
                    string value = keyValue[1].Trim();
                    result[key] = ParseValue(value);
                }
            }
            
            return result;
        }
        
        private List<string> SplitJSON(string json)
        {
            var result = new List<string>();
            int depth = 0;
            int start = 0;
            
            for (int i = 0; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') depth--;
                else if (json[i] == ',' && depth == 0)
                {
                    result.Add(json.Substring(start, i - start));
                    start = i + 1;
                }
            }
            
            if (start < json.Length)
                result.Add(json.Substring(start));
            
            return result;
        }
        
        private object ParseValue(string value)
        {
            value = value.Trim();
            
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value.Substring(1, value.Length - 2);
            
            if (value == "true") return true;
            if (value == "false") return false;
            if (value == "null") return null;
            
            if (int.TryParse(value, out int intVal))
                return intVal;
            
            if (double.TryParse(value, out double doubleVal))
                return doubleVal;
            
            return value;
        }
        
        private string SerializeJSON(Dictionary<string, object> message)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var kvp in message)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{kvp.Key}\":");
                
                if (kvp.Value is string)
                    sb.Append($"\"{EscapeString(kvp.Value.ToString())}\"");
                else if (kvp.Value is bool)
                    sb.Append(kvp.Value.ToString().ToLower());
                else
                    sb.Append(kvp.Value);
                
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
        
        private string EscapeString(string str)
        {
            return str.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }
    }
}

