using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace PokerServer
{
    /// <summary>
    /// Служба синхронизации с облачным хранилищем
    /// Позволяет автоматически синхронизировать данные пользователей между серверами
    /// </summary>
    public class CloudSyncService
    {
        private string syncUrl = "";
        private string apiKey = "";
        private HttpClient? httpClient;
        private bool enabled;
        
        public CloudSyncService(string? syncUrl = null, string? apiKey = null)
        {
            this.syncUrl = syncUrl ?? Environment.GetEnvironmentVariable("POKER_SYNC_URL") ?? "";
            this.apiKey = apiKey ?? Environment.GetEnvironmentVariable("POKER_SYNC_API_KEY") ?? "";
            this.enabled = !string.IsNullOrEmpty(this.syncUrl);
            
            if (enabled)
            {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("X-API-Key", this.apiKey);
                Console.WriteLine($"☁️ Облачная синхронизация включена: {this.syncUrl}");
            }
            else
            {
                Console.WriteLine("ℹ️ Облачная синхронизация отключена (установите POKER_SYNC_URL для включения)");
            }
        }
        
        /// <summary>
        /// Загружает данные пользователей с облака
        /// </summary>
        public async Task<Dictionary<string, UserDatabase.UserData>> LoadFromCloud()
        {
            if (!enabled) return null;
            
            try
            {
                if (httpClient == null) return null;
                var response = await httpClient.GetAsync($"{syncUrl}/users");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<Dictionary<string, UserDatabase.UserData>>(json);
                    Console.WriteLine($"☁️ Загружено {users?.Count ?? 0} пользователей с облака");
                    return users;
                }
                else
                {
                    Console.WriteLine($"⚠️ Не удалось загрузить данные с облака: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка загрузки с облака: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Сохраняет данные пользователей в облако
        /// </summary>
        public async Task<bool> SaveToCloud(Dictionary<string, UserDatabase.UserData> users)
        {
            if (!enabled || users == null) return false;
            
            try
            {
                if (httpClient == null) return false;
                string json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PutAsync($"{syncUrl}/users", content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"☁️ Сохранено {users.Count} пользователей в облако");
                    return true;
                }
                else
                {
                    Console.WriteLine($"⚠️ Не удалось сохранить в облако: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка сохранения в облако: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Синхронизирует данные: загружает с облака и объединяет с локальными
        /// </summary>
        public async Task<Dictionary<string, UserDatabase.UserData>> SyncUsers(
            Dictionary<string, UserDatabase.UserData> localUsers)
        {
            if (!enabled) return localUsers;
            
            try
            {
                var cloudUsers = await LoadFromCloud();
                if (cloudUsers == null || cloudUsers.Count == 0)
                {
                    // Если в облаке нет данных, сохраняем локальные
                    await SaveToCloud(localUsers);
                    return localUsers;
                }
                
                // Объединяем: приоритет у более новых данных
                var merged = new Dictionary<string, UserDatabase.UserData>(cloudUsers);
                
                foreach (var localUser in localUsers)
                {
                    if (!merged.ContainsKey(localUser.Key))
                    {
                        // Новый пользователь из локальной БД
                        merged[localUser.Key] = localUser.Value;
                    }
                    else
                    {
                        // Выбираем более новую версию
                        var cloudUser = merged[localUser.Key];
                        if (localUser.Value.LastLoginDate > cloudUser.LastLoginDate)
                        {
                            merged[localUser.Key] = localUser.Value;
                        }
                    }
                }
                
                // Сохраняем объединенные данные обратно в облако
                await SaveToCloud(merged);
                
                Console.WriteLine($"☁️ Синхронизировано: {merged.Count} пользователей");
                return merged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка синхронизации: {ex.Message}");
                return localUsers;
            }
        }
    }
}

