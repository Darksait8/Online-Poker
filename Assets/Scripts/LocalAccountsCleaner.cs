using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Утилита для удаления всех локальных аккаунтов
/// Используйте для очистки перед переходом на облачное хранение
/// </summary>
public class LocalAccountsCleaner : MonoBehaviour
{
    [ContextMenu("Удалить все локальные аккаунты")]
    public void DeleteAllLocalAccounts()
    {
        try
        {
            string profilesPath = Path.Combine(Application.persistentDataPath, "UserData", "Profiles");
            
            if (!Directory.Exists(profilesPath))
            {
                Debug.Log("Папка с профилями не найдена. Локальных аккаунтов нет.");
                return;
            }
            
            string[] profileFiles = Directory.GetFiles(profilesPath, "*.json");
            
            if (profileFiles.Length == 0)
            {
                Debug.Log("Локальных аккаунтов не найдено.");
                return;
            }
            
            int deletedCount = 0;
            foreach (string file in profileFiles)
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                    Debug.Log($"Удален профиль: {Path.GetFileName(file)}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Ошибка удаления {file}: {e.Message}");
                }
            }
            
            Debug.Log($"✅ Удалено локальных аккаунтов: {deletedCount} из {profileFiles.Length}");
            
            // Также удаляем резервные копии
            string backupPath = Path.Combine(Application.persistentDataPath, "UserData", "Backups");
            if (Directory.Exists(backupPath))
            {
                string[] backupFiles = Directory.GetFiles(backupPath, "*.json");
                foreach (string file in backupFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Не удалось удалить резервную копию {file}: {e.Message}");
                    }
                }
                Debug.Log($"✅ Удалено резервных копий: {backupFiles.Length}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при удалении локальных аккаунтов: {e.Message}");
        }
    }
    
    [ContextMenu("Показать путь к локальным аккаунтам")]
    public void ShowLocalAccountsPath()
    {
        string profilesPath = Path.Combine(Application.persistentDataPath, "UserData", "Profiles");
        Debug.Log($"Путь к локальным аккаунтам: {profilesPath}");
        
        if (Directory.Exists(profilesPath))
        {
            string[] files = Directory.GetFiles(profilesPath, "*.json");
            Debug.Log($"Найдено локальных аккаунтов: {files.Length}");
            foreach (string file in files)
            {
                Debug.Log($"  - {Path.GetFileName(file)}");
            }
        }
        else
        {
            Debug.Log("Папка не существует. Локальных аккаунтов нет.");
        }
    }
    
    [ContextMenu("Удалить все локальные данные (включая настройки)")]
    public void DeleteAllLocalData()
    {
        try
        {
            string dataPath = Path.Combine(Application.persistentDataPath, "UserData");
            
            if (Directory.Exists(dataPath))
            {
                Directory.Delete(dataPath, true);
                Debug.Log($"✅ Удалена вся папка UserData: {dataPath}");
            }
            else
            {
                Debug.Log("Папка UserData не найдена.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при удалении локальных данных: {e.Message}");
        }
    }
}

