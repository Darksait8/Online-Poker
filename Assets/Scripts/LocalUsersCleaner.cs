using UnityEngine;
using System.IO;

/// <summary>
/// Утилита для очистки локальных пользователей
/// Используется при переходе на серверную авторизацию
/// </summary>
public class LocalUsersCleaner : MonoBehaviour
{
    [ContextMenu("Удалить всех локальных пользователей")]
    public void DeleteAllLocalUsers()
    {
        try
        {
            string profilesPath = Path.Combine(Application.persistentDataPath, "UserData", "Profiles");
            
            if (Directory.Exists(profilesPath))
            {
                string[] files = Directory.GetFiles(profilesPath, "*.json");
                int deletedCount = 0;
                
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                        Debug.Log($"✅ Удален локальный профиль: {Path.GetFileName(file)}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"⚠️ Не удалось удалить файл {file}: {ex.Message}");
                    }
                }
                
                // Удаляем папки с файлами пользователей (аватары и т.д.)
                string[] directories = Directory.GetDirectories(profilesPath);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        Debug.Log($"✅ Удалена папка пользователя: {Path.GetFileName(dir)}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"⚠️ Не удалось удалить папку {dir}: {ex.Message}");
                    }
                }
                
                Debug.Log($"✅ Удалено {deletedCount} локальных профилей пользователей");
                Debug.Log("ℹ️ Локальные пользователи удалены. Теперь используются только серверные аккаунты.");
            }
            else
            {
                Debug.Log("ℹ️ Папка с локальными пользователями не найдена. Возможно, уже очищена.");
            }
            
            // Также очищаем текущего пользователя в AuthManager
            AuthManager.Logout();
            
            Debug.Log("✅ Очистка завершена. Перезапустите игру для применения изменений.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Ошибка при удалении локальных пользователей: {ex.Message}");
        }
    }
    
    [ContextMenu("Показать информацию о локальных пользователях")]
    public void ShowLocalUsersInfo()
    {
        try
        {
            string profilesPath = Path.Combine(Application.persistentDataPath, "UserData", "Profiles");
            
            if (Directory.Exists(profilesPath))
            {
                string[] files = Directory.GetFiles(profilesPath, "*.json");
                Debug.Log($"📊 Найдено локальных профилей: {files.Length}");
                
                foreach (string file in files)
                {
                    string username = Path.GetFileNameWithoutExtension(file);
                    FileInfo fileInfo = new FileInfo(file);
                    Debug.Log($"  - {username} ({fileInfo.Length} байт, изменен: {fileInfo.LastWriteTime})");
                }
            }
            else
            {
                Debug.Log("ℹ️ Папка с локальными пользователями не найдена.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Ошибка при получении информации: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Статический метод для удаления всех локальных пользователей
    /// Можно вызвать из любого места в коде
    /// </summary>
    public static void ClearAllLocalUsers()
    {
        try
        {
            string profilesPath = Path.Combine(Application.persistentDataPath, "UserData", "Profiles");
            
            if (Directory.Exists(profilesPath))
            {
                string[] files = Directory.GetFiles(profilesPath, "*.json");
                int deletedCount = 0;
                
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch { }
                }
                
                // Удаляем папки с файлами пользователей
                string[] directories = Directory.GetDirectories(profilesPath);
                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch { }
                }
                
                Debug.Log($"✅ Удалено {deletedCount} локальных профилей пользователей");
            }
            
            // Очищаем текущего пользователя
            AuthManager.Logout();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Ошибка при удалении локальных пользователей: {ex.Message}");
        }
    }
}

