using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Редактор для очистки локальных пользователей
/// Доступен через меню Unity: Tools > Clear Local Users
/// </summary>
public class ClearLocalUsersEditor
{
    [MenuItem("Tools/Очистить всех локальных пользователей")]
    public static void ClearLocalUsers()
    {
        if (!EditorUtility.DisplayDialog(
            "Удаление локальных пользователей",
            "Вы уверены, что хотите удалить всех локальных пользователей?\n\n" +
            "Это действие удалит все локальные профили пользователей.\n" +
            "Серверные аккаунты не будут затронуты.\n\n" +
            "ВНИМАНИЕ: Это действие нельзя отменить!",
            "Да, удалить",
            "Отмена"))
        {
            return;
        }
        
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
                
                EditorUtility.DisplayDialog(
                    "Успешно",
                    $"Удалено {deletedCount} локальных профилей пользователей.\n\n" +
                    "Локальные пользователи удалены. Теперь используются только серверные аккаунты.",
                    "OK");
                
                Debug.Log($"✅ Удалено {deletedCount} локальных профилей пользователей");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Информация",
                    "Папка с локальными пользователями не найдена.\nВозможно, уже очищена.",
                    "OK");
            }
            
            AssetDatabase.Refresh();
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog(
                "Ошибка",
                $"Ошибка при удалении локальных пользователей:\n{ex.Message}",
                "OK");
            Debug.LogError($"❌ Ошибка при удалении локальных пользователей: {ex.Message}");
        }
    }
    
    [MenuItem("Tools/Показать информацию о локальных пользователях")]
    public static void ShowLocalUsersInfo()
    {
        try
        {
            string profilesPath = Path.Combine(Application.persistentDataPath, "UserData", "Profiles");
            
            if (Directory.Exists(profilesPath))
            {
                string[] files = Directory.GetFiles(profilesPath, "*.json");
                string info = $"Найдено локальных профилей: {files.Length}\n\n";
                
                foreach (string file in files)
                {
                    string username = Path.GetFileNameWithoutExtension(file);
                    FileInfo fileInfo = new FileInfo(file);
                    info += $"• {username} ({fileInfo.Length} байт)\n";
                }
                
                EditorUtility.DisplayDialog("Локальные пользователи", info, "OK");
                Debug.Log($"📊 {info}");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Информация",
                    "Папка с локальными пользователями не найдена.",
                    "OK");
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog(
                "Ошибка",
                $"Ошибка при получении информации:\n{ex.Message}",
                "OK");
            Debug.LogError($"❌ Ошибка при получении информации: {ex.Message}");
        }
    }
}

