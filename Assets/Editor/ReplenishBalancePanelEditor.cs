using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

public static class ReplenishBalancePanelEditor
{
    [MenuItem("Tools/Poker/Create Replenish Balance Panel", false, 51)]
    public static void CreateReplenishBalancePanel()
    {
        // Находим Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("ReplenishBalancePanelEditor: Canvas не найден! Создаю Canvas...");
            
            // Создаем Canvas
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Создаем EventSystem если его нет
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // Создаем панель через статический метод
        ReplenishBalancePanel panel = ReplenishBalancePanel.CreateDefault(canvas.transform);
        
        if (panel != null)
        {
            // Устанавливаем имя
            panel.gameObject.name = "ReplenishBalancePanel";
            
            // Регистрируем создание для Undo
            Undo.RegisterCreatedObjectUndo(panel.gameObject, "Create Replenish Balance Panel");
            
            // Выбираем созданный объект
            Selection.activeGameObject = panel.gameObject;
            
            Debug.Log("ReplenishBalancePanelEditor: Панель пополнения баланса создана успешно!");
        }
        else
        {
            Debug.LogError("ReplenishBalancePanelEditor: Не удалось создать панель!");
        }
    }

}

