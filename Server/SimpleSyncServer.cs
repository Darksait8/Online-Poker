using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PokerServer
{
    /// <summary>
    /// Простой HTTP сервер для синхронизации данных пользователей
    /// Можно запустить на любом хостинге или использовать как отдельный сервис
    /// </summary>
    public class SimpleSyncServer
    {
        private HttpListener listener;
        private Dictionary<string, UserData> users;
        private string dataFilePath;
        private string apiKey;
        private bool isRunning;
        
        public class UserData
        {
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public string PasswordHash { get; set; } = "";
            public DateTime RegistrationDate { get; set; }
            public DateTime LastLoginDate { get; set; }
            public int Chips { get; set; }
            public int XP { get; set; }
            public int Level { get; set; }
        }
        
        public SimpleSyncServer(int port = 8889, string dataFilePath = "sync_users.json", string? apiKey = null)
        {
            this.dataFilePath = dataFilePath ?? "sync_users.json";
            this.apiKey = apiKey ?? "default-key-change-me";
            this.users = new Dictionary<string, UserData>();
            
            listener = new HttpListener();
            // Используем localhost вместо * для избежания проблем с правами доступа
            // Для доступа из сети используйте http://+:8889/ (требует прав администратора или резервации URL)
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            
            LoadUsers();
        }
        
        public void Start()
        {
            try
            {
                listener.Start();
                isRunning = true;
                
                string portInfo = listener.Prefixes.FirstOrDefault() ?? $"http://localhost:{port}/";
                string portNumber = portInfo.Replace("http://localhost:", "").Replace("http://127.0.0.1:", "").Replace("http://*:", "").Replace("http://+:", "").Replace("/", "");
                Console.WriteLine($"🌐 Сервер синхронизации запущен на порту {portNumber}");
                Console.WriteLine($"📁 Файл данных: {dataFilePath}");
                Console.WriteLine($"🔑 API Key: {apiKey}");
                Console.WriteLine($"📍 Доступен по адресу: http://localhost:{portNumber}/");
                Console.WriteLine("Ожидание запросов...");
                
                _ = Task.Run(Listen);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка запуска сервера синхронизации: {ex.Message}");
            }
        }
        
        public void Stop()
        {
            isRunning = false;
            listener?.Stop();
            SaveUsers();
            Console.WriteLine("🛑 Сервер синхронизации остановлен");
        }
        
        private async Task Listen()
        {
            while (isRunning)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Console.WriteLine($"❌ Ошибка обработки запроса: {ex.Message}");
                }
            }
        }
        
        private void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            // Проверка API ключа
            string? providedKey = request.Headers["X-API-Key"];
            if (providedKey != apiKey)
            {
                response.StatusCode = 401;
                response.Close();
                return;
            }
            
            try
            {
                if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/users")
                {
                    // Получить всех пользователей
                    string json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    
                    response.ContentType = "application/json";
                    response.ContentLength64 = buffer.Length;
                    response.StatusCode = 200;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                }
                else if (request.HttpMethod == "PUT" && request.Url.AbsolutePath == "/users")
                {
                    // Сохранить пользователей
                    using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        var newUsers = JsonSerializer.Deserialize<Dictionary<string, UserData>>(json);
                        
                        if (newUsers != null)
                        {
                            users = newUsers;
                            SaveUsers();
                            
                            response.StatusCode = 200;
                            response.Close();
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.Close();
                        }
                    }
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки запроса: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }
        
        private void LoadUsers()
        {
            try
            {
                if (File.Exists(dataFilePath))
                {
                    string json = File.ReadAllText(dataFilePath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, UserData>>(json);
                    if (loaded != null)
                    {
                        users = loaded;
                        Console.WriteLine($"📂 Загружено {users.Count} пользователей из файла синхронизации");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка загрузки данных синхронизации: {ex.Message}");
            }
        }
        
        private void SaveUsers()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(users, options);
                File.WriteAllText(dataFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка сохранения данных синхронизации: {ex.Message}");
            }
        }
    }
}

