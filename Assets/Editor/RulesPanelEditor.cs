using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using Object = UnityEngine.Object;

public static class RulesPanelEditor
{
    [MenuItem("Tools/Poker/Create Rules Panel", false, 52)]
    public static void CreateRulesPanel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Создаем EventSystem, если его нет
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            Debug.Log("RulesPanelEditor: Canvas и EventSystem созданы автоматически");
        }

        RulesPanel panel = RulesPanel.CreateDefault(canvas.transform);

        if (panel != null)
        {
            panel.gameObject.name = "RulesPanel";
            Undo.RegisterCreatedObjectUndo(panel.gameObject, "Create Rules Panel");
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("RulesPanelEditor: Панель правил создана успешно!");
        }
        else
        {
            Debug.LogError("RulesPanelEditor: Не удалось создать панель правил!");
        }
    }
}

