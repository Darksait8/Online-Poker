using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Утилита для поиска и исправления отсутствующих скриптов в сцене
/// </summary>
public class MissingScriptsFinder : EditorWindow
{
    [MenuItem("Tools/Poker/Find Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<MissingScriptsFinder>("Missing Scripts Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Поиск отсутствующих скриптов", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Найти отсутствующие скрипты"))
        {
            FindMissingScripts();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Удалить отсутствующие скрипты"))
        {
            RemoveMissingScripts();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Внимание: Удаление отсутствующих скриптов необратимо!", EditorStyles.helpBox);
    }

    private void FindMissingScripts()
    {
        Debug.Log("=== ПОИСК ОТСУТСТВУЮЩИХ СКРИПТОВ ===");
        
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int missingCount = 0;
        
        foreach (GameObject go in allObjects)
        {
            // Пропускаем объекты не из текущей сцены
            if (go.scene.name == null || !go.scene.isLoaded)
                continue;
                
            Component[] components = go.GetComponents<Component>();
            
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                    Debug.LogWarning($"Отсутствующий скрипт найден на объекте: {GetGameObjectPath(go)}", go);
                }
            }
        }
        
        if (missingCount == 0)
        {
            Debug.Log("✅ Отсутствующие скрипты не найдены!");
        }
        else
        {
            Debug.LogWarning($"⚠️ Найдено {missingCount} отсутствующих скриптов");
        }
    }

    private void RemoveMissingScripts()
    {
        Debug.Log("=== УДАЛЕНИЕ ОТСУТСТВУЮЩИХ СКРИПТОВ ===");
        
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int removedCount = 0;
        
        foreach (GameObject go in allObjects)
        {
            // Пропускаем объекты не из текущей сцены
            if (go.scene.name == null || !go.scene.isLoaded)
                continue;
                
            Component[] components = go.GetComponents<Component>();
            
            for (int i = components.Length - 1; i >= 0; i--)
            {
                if (components[i] == null)
                {
                    removedCount++;
                    Debug.Log($"Удален отсутствующий скрипт с объекта: {GetGameObjectPath(go)}", go);
                    
                    // Удаляем отсутствующий компонент
                    SerializedObject serializedObject = new SerializedObject(go);
                    SerializedProperty prop = serializedObject.FindProperty("m_Component");
                    
                    prop.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        if (removedCount == 0)
        {
            Debug.Log("✅ Отсутствующие скрипты не найдены!");
        }
        else
        {
            Debug.Log($"✅ Удалено {removedCount} отсутствующих скриптов");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}
