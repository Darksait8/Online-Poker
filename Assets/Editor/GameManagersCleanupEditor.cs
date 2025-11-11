#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameManagersCleanupEditor
{
    private static readonly string[] RemoveEntirelyTypes =
    {
        "TableConfigTester",
        "TableSettingsDebugger",
        "SceneTransitionChecker",
        "UserDataTester",
        "SceneTransitionChecker",
        "TableSettingsDebugger",
        "GameManager",           // старый менеджер (GameManager.cs)
        "PokerGameManager"       // устаревший PokerGameManager.cs
    };

    private static readonly string[] KeepSingleTypes =
    {
        "PokerGameManager",
        "GameStateMachine",
        "UnifiedPlayerManager",
        "AutoSeatFiller",
        "TableManager",
        "Deck",
        "GameManager"
    };

    [MenuItem("Tools/Poker/Cleanup/Remove Extra Managers")]
    public static void CleanupManagers()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogError("GameManagersCleanupEditor: активная сцена не загружена. Откройте сцену с игровым столом и попробуйте снова.");
            return;
        }

        int removedComponents = 0;
        int removedGameObjects = 0;

        removedComponents += RemoveAllSpecified(scene, out removedGameObjects);
        removedComponents += RemoveDuplicateManagers(scene, out var removedEmptyGos);
        removedGameObjects += removedEmptyGos;

        if (removedComponents > 0 || removedGameObjects > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"GameManagersCleanupEditor: Очистка завершена. Удалено компонентов: {removedComponents}, пустых объектов: {removedGameObjects}");
        }
        else
        {
            Debug.Log("GameManagersCleanupEditor: В сцене не обнаружено тестовых или дублирующих менеджеров.");
        }
    }

    private static int RemoveAllSpecified(Scene scene, out int removedGameObjects)
    {
        int removedComponents = 0;
        removedGameObjects = 0;

        foreach (var typeName in RemoveEntirelyTypes)
        {
            var type = FindType(typeName);
            if (type == null) continue;

            var components = Resources.FindObjectsOfTypeAll(type)
                .OfType<Component>()
                .Where(c => c != null && c.gameObject.scene == scene)
                .ToList();

            foreach (var component in components)
            {
                var go = component.gameObject;
                Debug.Log($"[Cleanup] Удаляю {typeName} на '{go.name}'");
                UnityEngine.Object.DestroyImmediate(component, true);
                removedComponents++;

                if (IsGameObjectEmpty(go))
                {
                    Debug.Log($"[Cleanup] Удален пустой объект '{go.name}'");
                    UnityEngine.Object.DestroyImmediate(go, true);
                    removedGameObjects++;
                }
            }
        }

        return removedComponents;
    }

    private static int RemoveDuplicateManagers(Scene scene, out int removedGameObjects)
    {
        int removedComponents = 0;
        removedGameObjects = 0;

        foreach (var typeName in KeepSingleTypes)
        {
            var type = FindType(typeName);
            if (type == null) continue;

            if (!typeof(Component).IsAssignableFrom(type))
            {
                Debug.LogWarning($"[Cleanup] Тип {typeName} не является компонентом Unity, пропускаю.");
                continue;
            }

            var components = Resources.FindObjectsOfTypeAll(type)
                .OfType<Component>()
                .Where(c => c != null && c.gameObject.scene == scene)
                .OrderBy(c => c.transform.GetSiblingIndex())
                .ToList();

            if (components.Count <= 1) continue;

            Debug.LogWarning($"[Cleanup] Найдено {components.Count} экземпляров {typeName}. Оставляю только первый.");

            for (int i = 1; i < components.Count; i++)
            {
                var component = components[i];
                if (component == null) continue;

                var go = component.gameObject;
                Debug.LogWarning($"[Cleanup] Удаляю дублирующий {typeName} на '{go.name}'");
                UnityEngine.Object.DestroyImmediate(component, true);
                removedComponents++;

                if (IsGameObjectEmpty(go))
                {
                    Debug.LogWarning($"[Cleanup] Удаляю пустой объект '{go.name}' после удаления {typeName}");
                    UnityEngine.Object.DestroyImmediate(go, true);
                    removedGameObjects++;
                }
            }
        }

        return removedComponents;
    }

    private static bool IsGameObjectEmpty(GameObject go)
    {
        if (go == null) return false;
        return go.GetComponents<Component>().Length <= 1; // только Transform
    }

    private static Type FindType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = null;
            try
            {
                type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
            }
            catch (ReflectionTypeLoadException ex)
            {
                type = ex.Types.FirstOrDefault(t => t != null && t.Name == typeName);
            }

            if (type != null)
                return type;
        }

        Debug.LogWarning($"GameManagersCleanupEditor: не удалось найти тип {typeName}");
        return null;
    }
}
#endif
