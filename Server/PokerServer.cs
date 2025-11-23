using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PokerServer
{
    /// <summary>
    /// Сервер для онлайн-покера
    /// Обрабатывает подключения клиентов и синхронизирует состояние игры
    /// </summary>
    public class PokerServer
    {
        private TcpListener listener;
        private List<ClientConnection> clients = new List<ClientConnection>();
        private GameSession currentSession;
        private bool isRunning = false;
        private readonly object clientsLock = new object();
        private UserDatabase userDatabase;
        
        private int port;
        
        public PokerServer(int port = 8888)
        {
            this.port = port;
            this.userDatabase = new UserDatabase("users.json");
        }
        
        public void Start()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                isRunning = true;
                
                Console.WriteLine($"🎮 Покерный сервер запущен на порту {port}");
                Console.WriteLine("Ожидание подключений...");
                Console.WriteLine();
                Console.WriteLine("📡 Доступные IP-адреса для подключения:");
                PrintLocalIPAddresses();
                Console.WriteLine();
                
                Task.Run(AcceptClients);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка запуска сервера: {ex.Message}");
            }
        }
        
        public void Stop()
        {
            isRunning = false;
            listener?.Stop();
            
            lock (clientsLock)
            {
                foreach (var client in clients)
                {
                    client.Close();
                }
                clients.Clear();
            }
            
            Console.WriteLine("🛑 Сервер остановлен");
        }
        
        private async Task AcceptClients()
        {
            while (isRunning)
            {
                try
                {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    var client = new ClientConnection(tcpClient, this);
                    
                    lock (clientsLock)
                    {
                        clients.Add(client);
                    }
                    
                    Console.WriteLine($"✅ Новый клиент подключен. Всего клиентов: {clients.Count}");
                    
                    Task.Run(() => client.HandleClient());
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Console.WriteLine($"❌ Ошибка принятия клиента: {ex.Message}");
                }
            }
        }
        
        public void RemoveClient(ClientConnection client)
        {
            lock (clientsLock)
            {
                clients.Remove(client);
                Console.WriteLine($"👋 Клиент отключен. Осталось клиентов: {clients.Count}");
            }
        }
        
        public void BroadcastMessage(Dictionary<string, object> message, ClientConnection excludeClient = null)
        {
            string json = SerializeMessage(message);
            byte[] data = Encoding.UTF8.GetBytes(json);
            
            lock (clientsLock)
            {
                foreach (var client in clients)
                {
                    if (client != excludeClient && client.IsConnected)
                    {
                        try
                        {
                            client.Send(data);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Ошибка отправки сообщения клиенту: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        public void HandleAuthRegister(ClientConnection client, Dictionary<string, object> data)
        {
            string username = data.ContainsKey("username") ? data["username"].ToString() : "";
            string email = data.ContainsKey("email") ? data["email"].ToString() : "";
            string password = data.ContainsKey("password") ? data["password"].ToString() : "";
            
            var result = userDatabase.Register(username, email, password);
            
            var response = new Dictionary<string, object>
            {
                {"type", "auth_register_response"},
                {"success", result.Success},
                {"message", result.Message}
            };
            
            if (result.Success && result.User != null)
            {
                response["username"] = result.User.Username;
                response["chips"] = result.User.Chips;
                response["xp"] = result.User.XP;
                response["level"] = result.User.Level;
            }
            
            client.SendMessage(response);
        }
        
        public void HandleAuthLogin(ClientConnection client, Dictionary<string, object> data)
        {
            string username = data.ContainsKey("username") ? data["username"].ToString() : "";
            string password = data.ContainsKey("password") ? data["password"].ToString() : "";
            
            var result = userDatabase.Login(username, password);
            
            var response = new Dictionary<string, object>
            {
                {"type", "auth_login_response"},
                {"success", result.Success},
                {"message", result.Message}
            };
            
            if (result.Success && result.User != null)
            {
                response["username"] = result.User.Username;
                response["email"] = result.User.Email;
                response["chips"] = result.User.Chips;
                response["xp"] = result.User.XP;
                response["level"] = result.User.Level;
                response["registration_date"] = result.User.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss");
            }
            
            client.SendMessage(response);
        }
        
        public void HandleAuthGetProfile(ClientConnection client, Dictionary<string, object> data)
        {
            string username = data.ContainsKey("username") ? data["username"].ToString() : "";
            
            var user = userDatabase.GetUser(username);
            
            var response = new Dictionary<string, object>
            {
                {"type", "auth_profile_response"},
                {"success", user != null}
            };
            
            if (user != null)
            {
                response["username"] = user.Username;
                response["email"] = user.Email;
                response["chips"] = user.Chips;
                response["xp"] = user.XP;
                response["level"] = user.Level;
                response["registration_date"] = user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                response["message"] = "Пользователь не найден";
            }
            
            client.SendMessage(response);
        }
        
        public void HandleAuthUpdateProfile(ClientConnection client, Dictionary<string, object> data)
        {
            string username = data.ContainsKey("username") ? data["username"].ToString() : "";
            
            var user = userDatabase.GetUser(username);
            if (user == null)
            {
                var errorResponse = new Dictionary<string, object>
                {
                    {"type", "auth_update_response"},
                    {"success", false},
                    {"message", "Пользователь не найден"}
                };
                client.SendMessage(errorResponse);
                return;
            }
            
            // Обновляем данные пользователя
            if (data.ContainsKey("chips"))
                user.Chips = Convert.ToInt32(data["chips"]);
            if (data.ContainsKey("xp"))
                user.XP = Convert.ToInt32(data["xp"]);
            if (data.ContainsKey("level"))
                user.Level = Convert.ToInt32(data["level"]);
            
            bool updated = userDatabase.UpdateUser(user);
            
            var response = new Dictionary<string, object>
            {
                {"type", "auth_update_response"},
                {"success", updated},
                {"message", updated ? "Профиль обновлен" : "Ошибка обновления"}
            };
            
            if (updated)
            {
                response["chips"] = user.Chips;
                response["xp"] = user.XP;
                response["level"] = user.Level;
            }
            
            client.SendMessage(response);
        }
        
        public void HandleJoinRequest(ClientConnection client, Dictionary<string, object> data)
        {
            string playerName = data.ContainsKey("name") ? data["name"].ToString() : "Игрок";
            int startingStack = data.ContainsKey("stack") ? Convert.ToInt32(data["stack"]) : 1000;
            
            client.PlayerName = playerName;
            client.Stack = startingStack;
            client.ClientId = Guid.NewGuid().ToString();
            
            // Создаем или присоединяем к сессии
            if (currentSession == null || currentSession.IsFull)
            {
                currentSession = new GameSession();
            }
            
            currentSession.AddPlayer(client);
            
            // Отправляем подтверждение
            var response = new Dictionary<string, object>
            {
                {"type", "join_success"},
                {"client_id", client.ClientId},
                {"player_name", playerName}
            };
            client.SendMessage(response);
            
            // Уведомляем всех о новом игроке
            BroadcastPlayersUpdate();
            
            // Если достаточно игроков, начинаем игру
            if (currentSession.Players.Count >= 2 && !currentSession.IsGameActive)
            {
                StartNewHand();
            }
        }
        
        public void HandlePlayerAction(ClientConnection client, Dictionary<string, object> data)
        {
            if (currentSession == null || !currentSession.IsGameActive)
            {
                var error = new Dictionary<string, object>
                {
                    {"type", "error"},
                    {"message", "Игра не активна"}
                };
                client.SendMessage(error);
                return;
            }
            
            string action = data.ContainsKey("action") ? data["action"].ToString() : "";
            int amount = data.ContainsKey("amount") ? Convert.ToInt32(data["amount"]) : 0;
            
            // Обрабатываем действие игрока
            currentSession.ProcessPlayerAction(client, action, amount);
            
            // Отправляем обновление всем клиентам
            BroadcastPlayerAction(client.PlayerName, action, amount);
            
            // Проверяем, нужно ли перейти к следующей фазе
            if (currentSession.CheckRoundComplete())
            {
                currentSession.AdvancePhase();
                BroadcastGameState();
            }
        }
        
        private void StartNewHand()
        {
            if (currentSession == null) return;
            
            currentSession.StartNewHand();
            
            var message = new Dictionary<string, object>
            {
                {"type", "hand_start"},
                {"dealer_index", currentSession.DealerIndex},
                {"small_blind", currentSession.SmallBlind},
                {"big_blind", currentSession.BigBlind}
            };
            
            BroadcastMessage(message);
            
            // Отправляем карты каждому игроку
            foreach (var player in currentSession.Players)
            {
                var cardsMessage = new Dictionary<string, object>
                {
                    {"type", "hole_cards"},
                    {"card1", currentSession.GetPlayerCard1(player).ToString()},
                    {"card2", currentSession.GetPlayerCard2(player).ToString()}
                };
                player.SendMessage(cardsMessage);
            }
            
            BroadcastGameState();
        }
        
        private void BroadcastPlayersUpdate()
        {
            if (currentSession == null) return;
            
            var playersList = new List<string>();
            foreach (var player in currentSession.Players)
            {
                playersList.Add(player.PlayerName);
            }
            
            var message = new Dictionary<string, object>
            {
                {"type", "players_update"},
                {"players", string.Join(",", playersList)},
                {"count", currentSession.Players.Count}
            };
            
            BroadcastMessage(message);
        }
        
        private void BroadcastPlayerAction(string playerName, string action, int amount)
        {
            var message = new Dictionary<string, object>
            {
                {"type", "player_action"},
                {"player_name", playerName},
                {"action", action},
                {"amount", amount}
            };
            
            BroadcastMessage(message);
        }
        
        private void BroadcastGameState()
        {
            if (currentSession == null) return;
            
            var message = new Dictionary<string, object>
            {
                {"type", "game_state"},
                {"hand_active", currentSession.IsGameActive},
                {"phase", currentSession.CurrentPhase},
                {"current_bet", currentSession.CurrentBet},
                {"pot", currentSession.Pot}
            };
            
            // Добавляем информацию о картах на столе
            if (currentSession.CommunityCards.Count > 0)
            {
                message["community_cards"] = string.Join(",", currentSession.CommunityCards);
            }
            
            BroadcastMessage(message);
        }
        
        private void PrintLocalIPAddresses()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        Console.WriteLine($"   - {ip} (для подключения используйте этот IP)");
                    }
                }
                Console.WriteLine($"   - localhost или 127.0.0.1 (только для локального подключения)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Не удалось определить IP-адреса: {ex.Message}");
            }
        }
        
        private string SerializeMessage(Dictionary<string, object> message)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var kvp in message)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{kvp.Key}\":");
                
                if (kvp.Value is string)
                    sb.Append($"\"{kvp.Value}\"");
                else if (kvp.Value is bool)
                    sb.Append(kvp.Value.ToString().ToLower());
                else
                    sb.Append(kvp.Value);
                
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}

