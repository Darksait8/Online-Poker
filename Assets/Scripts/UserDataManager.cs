using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public static class UserDataManager
{
    private const string DATA_FOLDER = "UserData";
    private const string PROFILES_FOLDER = "Profiles";
    private const string SETTINGS_FILE = "GlobalSettings.json";
    private const string BACKUP_FOLDER = "Backups";
    
    private static string DataPath => Path.Combine(Application.persistentDataPath, DATA_FOLDER);
    private static string ProfilesPath => Path.Combine(DataPath, PROFILES_FOLDER);
    private static string SettingsPath => Path.Combine(DataPath, SETTINGS_FILE);
    private static string BackupPath => Path.Combine(DataPath, BACKUP_FOLDER);
    
    public static event Action<UserProfile> OnProfileLoaded;
    public static event Action<UserProfile> OnProfileSaved;
    public static event Action<string> OnDataError;
    
    /// <summary>
    /// Инициализация системы данных
    /// </summary>
    public static void Initialize()
    {
        try
        {
            // Создаем необходимые папки
            CreateDirectories();
            
            Debug.Log($"UserDataManager initialized. Data path: {DataPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize UserDataManager: {e.Message}");
            OnDataError?.Invoke($"Ошибка инициализации: {e.Message}");
        }
    }
    
    /// <summary>
    /// Создает необходимые директории
    /// </summary>
    private static void CreateDirectories()
    {
        if (!Directory.Exists(DataPath))
            Directory.CreateDirectory(DataPath);
            
        if (!Directory.Exists(ProfilesPath))
            Directory.CreateDirectory(ProfilesPath);
            
        if (!Directory.Exists(BackupPath))
            Directory.CreateDirectory(BackupPath);
    }
    
    /// <summary>
    /// Сохраняет профиль пользователя
    /// </summary>
    public static bool SaveUserProfile(UserProfile profile)
    {
        try
        {
            if (profile == null || string.IsNullOrEmpty(profile.username))
            {
                Debug.LogError("Cannot save profile: profile is null or username is empty");
                return false;
            }
            
            // Убеждаемся, что папки существуют
            CreateDirectories();
            
            // Создаем резервную копию
            CreateBackup(profile.username);
            
            // Сохраняем профиль
            string profilePath = GetProfilePath(profile.username);
            string json = JsonUtility.ToJson(profile, true);
            
            File.WriteAllText(profilePath, json);
            
            Debug.Log($"Profile saved: {profile.username}");
            OnProfileSaved?.Invoke(profile);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save profile {profile.username}: {e.Message}");
            OnDataError?.Invoke($"Ошибка сохранения профиля: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Загружает профиль пользователя
    /// </summary>
    public static UserProfile LoadUserProfile(string username)
    {
        try
        {
            if (string.IsNullOrEmpty(username))
            {
                Debug.LogError("Cannot load profile: username is empty");
                return null;
            }
            
            // Убеждаемся, что папки существуют
            CreateDirectories();
            
            string profilePath = GetProfilePath(username);
            
            if (!File.Exists(profilePath))
            {
                Debug.LogWarning($"Profile not found: {username}");
                return null;
            }
            
            string json = File.ReadAllText(profilePath);
            UserProfile profile = JsonUtility.FromJson<UserProfile>(json);
            
            if (profile != null)
            {
                Debug.Log($"Profile loaded: {username}");
                OnProfileLoaded?.Invoke(profile);
            }
            
            return profile;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load profile {username}: {e.Message}");
            OnDataError?.Invoke($"Ошибка загрузки профиля: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Проверяет существование профиля
    /// </summary>
    public static bool ProfileExists(string username)
    {
        if (string.IsNullOrEmpty(username))
            return false;
            
        string profilePath = GetProfilePath(username);
        return File.Exists(profilePath);
    }
    
    /// <summary>
    /// Удаляет профиль пользователя
    /// </summary>
    public static bool DeleteUserProfile(string username)
    {
        try
        {
            if (string.IsNullOrEmpty(username))
                return false;
                
            string profilePath = GetProfilePath(username);
            
            if (File.Exists(profilePath))
            {
                File.Delete(profilePath);
                Debug.Log($"Profile deleted: {username}");
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete profile {username}: {e.Message}");
            OnDataError?.Invoke($"Ошибка удаления профиля: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Получает список всех профилей
    /// </summary>
    public static List<string> GetAllUsernames()
    {
        List<string> usernames = new List<string>();
        
        try
        {
            if (Directory.Exists(ProfilesPath))
            {
                string[] files = Directory.GetFiles(ProfilesPath, "*.json");
                
                foreach (string file in files)
                {
                    string username = Path.GetFileNameWithoutExtension(file);
                    usernames.Add(username);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get usernames: {e.Message}");
            OnDataError?.Invoke($"Ошибка получения списка пользователей: {e.Message}");
        }
        
        return usernames;
    }
    
    /// <summary>
    /// Хеширует пароль
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "";
            
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
    
    /// <summary>
    /// Проверяет пароль
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;
            
        string hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }
    
    /// <summary>
    /// Создает резервную копию профиля
    /// </summary>
    private static void CreateBackup(string username)
    {
        try
        {
            string profilePath = GetProfilePath(username);
            
            if (File.Exists(profilePath))
            {
                string backupFileName = $"{username}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string backupPath = Path.Combine(BackupPath, backupFileName);
                
                File.Copy(profilePath, backupPath);
                
                // Удаляем старые резервные копии (старше 30 дней)
                CleanOldBackups(username);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to create backup for {username}: {e.Message}");
        }
    }
    
    /// <summary>
    /// Удаляет старые резервные копии
    /// </summary>
    private static void CleanOldBackups(string username)
    {
        try
        {
            if (!Directory.Exists(BackupPath))
                return;
                
            string[] backupFiles = Directory.GetFiles(BackupPath, $"{username}_*.json");
            DateTime cutoffDate = DateTime.Now.AddDays(-30);
            
            foreach (string file in backupFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to clean old backups: {e.Message}");
        }
    }
    
    /// <summary>
    /// Получает путь к файлу профиля
    /// </summary>
    private static string GetProfilePath(string username)
    {
        string safeUsername = SanitizeFileName(username);
        return Path.Combine(ProfilesPath, $"{safeUsername}.json");
    }
    
    /// <summary>
    /// Очищает имя файла от недопустимых символов
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
    
    /// <summary>
    /// Экспортирует профиль в JSON строку
    /// </summary>
    public static string ExportProfile(UserProfile profile)
    {
        try
        {
            return JsonUtility.ToJson(profile, true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export profile: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Импортирует профиль из JSON строки
    /// </summary>
    public static UserProfile ImportProfile(string jsonData)
    {
        try
        {
            return JsonUtility.FromJson<UserProfile>(jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to import profile: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Получает размер данных в байтах
    /// </summary>
    public static long GetDataSize()
    {
        try
        {
            if (!Directory.Exists(DataPath))
                return 0;
                
            long totalSize = 0;
            DirectoryInfo dirInfo = new DirectoryInfo(DataPath);
            
            foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                totalSize += file.Length;
            }
            
            return totalSize;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get data size: {e.Message}");
            return 0;
        }
    }
    
    /// <summary>
    /// Очищает все данные (для тестирования)
    /// </summary>
    public static void ClearAllData()
    {
        try
        {
            if (Directory.Exists(DataPath))
            {
                Directory.Delete(DataPath, true);
                Debug.Log("All user data cleared");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to clear data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Получает информацию о системе данных
    /// </summary>
    public static string GetDataInfo()
    {
        try
        {
            var usernames = GetAllUsernames();
            long dataSize = GetDataSize();
            
            return $"Profiles: {usernames.Count}, Data size: {dataSize / 1024} KB, Path: {DataPath}";
        }
        catch (Exception e)
        {
            return $"Error getting data info: {e.Message}";
        }
    }
}
