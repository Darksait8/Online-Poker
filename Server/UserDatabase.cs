using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PokerServer
{
    /// <summary>
    /// База данных пользователей на сервере
    /// Хранит все аккаунты глобально
    /// </summary>
    public class UserDatabase
    {
        private Dictionary<string, UserData> users = new Dictionary<string, UserData>();
        private readonly object usersLock = new object();
        private string dataFilePath;
        
        public UserDatabase(string dataFilePath = "users.json")
        {
            this.dataFilePath = dataFilePath;
            LoadUsers();
        }
        
        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        public RegisterResult Register(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                return new RegisterResult { Success = false, Message = "Имя пользователя не может быть пустым" };
            
            if (string.IsNullOrWhiteSpace(password))
                return new RegisterResult { Success = false, Message = "Пароль не может быть пустым" };
            
            if (password.Length < 6)
                return new RegisterResult { Success = false, Message = "Пароль должен содержать минимум 6 символов" };
            
            lock (usersLock)
            {
                if (users.ContainsKey(username.ToLower()))
                {
                    return new RegisterResult { Success = false, Message = "Пользователь с таким именем уже существует" };
                }
                
                var user = new UserData
                {
                    Username = username,
                    Email = email ?? "",
                    PasswordHash = HashPassword(password),
                    RegistrationDate = DateTime.Now,
                    LastLoginDate = DateTime.Now,
                    Chips = 1000, // Начальный баланс
                    XP = 0,
                    Level = 1
                };
                
                users[username.ToLower()] = user;
                SaveUsers();
                
                Console.WriteLine($"✅ Зарегистрирован новый пользователь: {username}");
                return new RegisterResult { Success = true, Message = "Регистрация успешна", User = user };
            }
        }
        
        /// <summary>
        /// Вход пользователя
        /// </summary>
        public LoginResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new LoginResult { Success = false, Message = "Имя пользователя и пароль не могут быть пустыми" };
            
            lock (usersLock)
            {
                string key = username.ToLower();
                if (!users.ContainsKey(key))
                {
                    return new LoginResult { Success = false, Message = "Пользователь не найден" };
                }
                
                var user = users[key];
                
                if (!VerifyPassword(password, user.PasswordHash))
                {
                    return new LoginResult { Success = false, Message = "Неверный пароль" };
                }
                
                user.LastLoginDate = DateTime.Now;
                SaveUsers();
                
                Console.WriteLine($"✅ Пользователь вошел в систему: {username}");
                return new LoginResult { Success = true, Message = "Вход выполнен успешно", User = user };
            }
        }
        
        /// <summary>
        /// Получение профиля пользователя
        /// </summary>
        public UserData GetUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;
            
            lock (usersLock)
            {
                string key = username.ToLower();
                return users.ContainsKey(key) ? users[key] : null;
            }
        }
        
        /// <summary>
        /// Обновление профиля пользователя
        /// </summary>
        public bool UpdateUser(UserData user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Username))
                return false;
            
            lock (usersLock)
            {
                string key = user.Username.ToLower();
                if (users.ContainsKey(key))
                {
                    users[key] = user;
                    SaveUsers();
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Проверка существования пользователя
        /// </summary>
        public bool UserExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;
            
            lock (usersLock)
            {
                return users.ContainsKey(username.ToLower());
            }
        }
        
        /// <summary>
        /// Получение списка всех пользователей
        /// </summary>
        public List<UserData> GetAllUsers()
        {
            lock (usersLock)
            {
                return users.Values.ToList();
            }
        }
        
        /// <summary>
        /// Отправка заявки в друзья
        /// </summary>
        public bool SendFriendRequest(string fromUsername, string toUsername, out string error)
        {
            error = string.Empty;
            lock (usersLock)
            {
                if (!users.ContainsKey(fromUsername.ToLower()) || !users.ContainsKey(toUsername.ToLower()))
                {
                    error = "Пользователь не найден";
                    return false;
                }
                
                var fromUser = users[fromUsername.ToLower()];
                var toUser = users[toUsername.ToLower()];
                
                // Инициализируем списки, если они null
                if (fromUser.Friends == null) fromUser.Friends = new List<string>();
                if (fromUser.OutgoingFriendRequests == null) fromUser.OutgoingFriendRequests = new List<FriendRequest>();
                if (toUser.IncomingFriendRequests == null) toUser.IncomingFriendRequests = new List<FriendRequest>();
                if (toUser.Friends == null) toUser.Friends = new List<string>();
                
                // Проверяем, не друзья ли уже
                if (fromUser.Friends.Contains(toUsername, StringComparer.OrdinalIgnoreCase))
                {
                    error = "Вы уже друзья";
                    return false;
                }
                
                // Проверяем, не отправлена ли уже заявка
                if (fromUser.OutgoingFriendRequests.Any(r => 
                    string.Equals(r.To, toUsername, StringComparison.OrdinalIgnoreCase)))
                {
                    error = "Заявка уже отправлена";
                    return false;
                }
                
                // Создаем заявку
                var request = new FriendRequest
                {
                    From = fromUsername,
                    To = toUsername,
                    CreatedAt = DateTime.Now
                };
                
                fromUser.OutgoingFriendRequests.Add(request);
                toUser.IncomingFriendRequests.Add(request);
                
                SaveUsers();
                return true;
            }
        }
        
        /// <summary>
        /// Принятие заявки в друзья
        /// </summary>
        public bool AcceptFriendRequest(string username, string requesterUsername, out string error)
        {
            error = string.Empty;
            lock (usersLock)
            {
                if (!users.ContainsKey(username.ToLower()) || !users.ContainsKey(requesterUsername.ToLower()))
                {
                    error = "Пользователь не найден";
                    return false;
                }
                
                var user = users[username.ToLower()];
                var requester = users[requesterUsername.ToLower()];
                
                // Инициализируем списки, если они null
                if (user.IncomingFriendRequests == null) user.IncomingFriendRequests = new List<FriendRequest>();
                if (user.Friends == null) user.Friends = new List<string>();
                if (requester.OutgoingFriendRequests == null) requester.OutgoingFriendRequests = new List<FriendRequest>();
                if (requester.Friends == null) requester.Friends = new List<string>();
                
                // Удаляем заявку из входящих
                var request = user.IncomingFriendRequests.FirstOrDefault(r => 
                    string.Equals(r.From, requesterUsername, StringComparison.OrdinalIgnoreCase));
                
                if (request == null)
                {
                    error = "Заявка не найдена";
                    return false;
                }
                
                user.IncomingFriendRequests.Remove(request);
                requester.OutgoingFriendRequests.RemoveAll(r => 
                    string.Equals(r.To, username, StringComparison.OrdinalIgnoreCase));
                
                // Добавляем в друзья
                if (!user.Friends.Contains(requesterUsername, StringComparer.OrdinalIgnoreCase))
                    user.Friends.Add(requesterUsername);
                if (!requester.Friends.Contains(username, StringComparer.OrdinalIgnoreCase))
                    requester.Friends.Add(username);
                
                SaveUsers();
                return true;
            }
        }
        
        /// <summary>
        /// Отклонение заявки в друзья
        /// </summary>
        public bool DeclineFriendRequest(string username, string requesterUsername, out string error)
        {
            error = string.Empty;
            lock (usersLock)
            {
                if (!users.ContainsKey(username.ToLower()) || !users.ContainsKey(requesterUsername.ToLower()))
                {
                    error = "Пользователь не найден";
                    return false;
                }
                
                var user = users[username.ToLower()];
                var requester = users[requesterUsername.ToLower()];
                
                // Инициализируем списки, если они null
                if (user.IncomingFriendRequests == null) user.IncomingFriendRequests = new List<FriendRequest>();
                if (requester.OutgoingFriendRequests == null) requester.OutgoingFriendRequests = new List<FriendRequest>();
                
                // Удаляем заявку
                var request = user.IncomingFriendRequests.FirstOrDefault(r => 
                    string.Equals(r.From, requesterUsername, StringComparison.OrdinalIgnoreCase));
                
                if (request == null)
                {
                    error = "Заявка не найдена";
                    return false;
                }
                
                user.IncomingFriendRequests.Remove(request);
                requester.OutgoingFriendRequests.RemoveAll(r => 
                    string.Equals(r.To, username, StringComparison.OrdinalIgnoreCase));
                
                SaveUsers();
                return true;
            }
        }
        
        /// <summary>
        /// Отмена отправленной заявки
        /// </summary>
        public bool CancelFriendRequest(string fromUsername, string toUsername, out string error)
        {
            error = string.Empty;
            lock (usersLock)
            {
                if (!users.ContainsKey(fromUsername.ToLower()) || !users.ContainsKey(toUsername.ToLower()))
                {
                    error = "Пользователь не найден";
                    return false;
                }
                
                var fromUser = users[fromUsername.ToLower()];
                var toUser = users[toUsername.ToLower()];
                
                // Инициализируем списки, если они null
                if (fromUser.OutgoingFriendRequests == null) fromUser.OutgoingFriendRequests = new List<FriendRequest>();
                if (toUser.IncomingFriendRequests == null) toUser.IncomingFriendRequests = new List<FriendRequest>();
                
                // Удаляем заявку
                var request = fromUser.OutgoingFriendRequests.FirstOrDefault(r => 
                    string.Equals(r.To, toUsername, StringComparison.OrdinalIgnoreCase));
                
                if (request == null)
                {
                    error = "Заявка не найдена";
                    return false;
                }
                
                fromUser.OutgoingFriendRequests.Remove(request);
                toUser.IncomingFriendRequests.RemoveAll(r => 
                    string.Equals(r.From, fromUsername, StringComparison.OrdinalIgnoreCase));
                
                SaveUsers();
                return true;
            }
        }
        
        /// <summary>
        /// Удаление из друзей
        /// </summary>
        public bool RemoveFriend(string username, string friendUsername, out string error)
        {
            error = string.Empty;
            lock (usersLock)
            {
                if (!users.ContainsKey(username.ToLower()) || !users.ContainsKey(friendUsername.ToLower()))
                {
                    error = "Пользователь не найден";
                    return false;
                }
                
                var user = users[username.ToLower()];
                var friend = users[friendUsername.ToLower()];
                
                // Инициализируем списки, если они null
                if (user.Friends == null) user.Friends = new List<string>();
                if (friend.Friends == null) friend.Friends = new List<string>();
                
                user.Friends.RemoveAll(f => string.Equals(f, friendUsername, StringComparison.OrdinalIgnoreCase));
                friend.Friends.RemoveAll(f => string.Equals(f, username, StringComparison.OrdinalIgnoreCase));
                
                SaveUsers();
                return true;
            }
        }
        
        /// <summary>
        /// Хеширование пароля
        /// </summary>
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
        
        /// <summary>
        /// Проверка пароля
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;
            
            string hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }
        
        /// <summary>
        /// Загрузка пользователей из файла
        /// </summary>
        private void LoadUsers()
        {
            try
            {
                if (System.IO.File.Exists(dataFilePath))
                {
                    string json = System.IO.File.ReadAllText(dataFilePath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, UserData>>(json);
                    if (data != null)
                    {
                        users = data;
                        Console.WriteLine($"📂 Загружено {users.Count} пользователей из базы данных");
                    }
                }
                else
                {
                    Console.WriteLine("📂 База данных пользователей не найдена, создана новая");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка загрузки базы данных: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Сохранение пользователей в файл
        /// </summary>
        private void SaveUsers()
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                string json = System.Text.Json.JsonSerializer.Serialize(users, options);
                System.IO.File.WriteAllText(dataFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка сохранения базы данных: {ex.Message}");
            }
        }
        
        public class UserData
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public string PasswordHash { get; set; }
            public DateTime RegistrationDate { get; set; }
            public DateTime LastLoginDate { get; set; }
            public int Chips { get; set; }
            public int XP { get; set; }
            public int Level { get; set; }
            
            // Социальные функции
            public List<string> Friends { get; set; } = new List<string>();
            public List<FriendRequest> IncomingFriendRequests { get; set; } = new List<FriendRequest>();
            public List<FriendRequest> OutgoingFriendRequests { get; set; } = new List<FriendRequest>();
        }
        
        public class FriendRequest
        {
            public string From { get; set; }
            public string To { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        
        public class RegisterResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public UserData User { get; set; }
        }
        
        public class LoginResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public UserData User { get; set; }
        }
    }
}

