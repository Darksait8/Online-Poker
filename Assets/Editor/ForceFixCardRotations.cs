using UnityEngine;
using UnityEditor;

public class ForceFixCardRotations : EditorWindow
{
    [MenuItem("Tools/Poker/Force Fix Card Rotations")]
    public static void ShowWindow()
    {
        GetWindow<ForceFixCardRotations>("Fix Card Rotations");
    }

    private void OnGUI()
    {
        GUILayout.Label("Принудительное исправление поворота карт", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Исправить поворот карт СЕЙЧАС"))
        {
            ForceFixRotations();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Показать углы всех мест"))
        {
            ShowAllAngles();
        }
    }

    private void ForceFixRotations()
    {
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout == null)
        {
            Debug.LogError("SeatsLayoutRadial не найден!");
            return;
        }

        Debug.Log("=== ПРИНУДИТЕЛЬНОЕ ИСПРАВЛЕНИЕ ПОВОРОТА КАРТ ===");

        // Находим все места
        var seats = seatsLayout.GetComponentsInChildren<NewBehaviourScript>();
        Debug.Log($"Найдено мест: {seats.Length}");

        for (int i = 0; i < seats.Length; i++)
        {
            var seat = seats[i];
            if (seat == null) continue;

            // Получаем компоненты карт напрямую
            var hole1 = seat.transform.Find("Hole1")?.GetComponent<RectTransform>();
            var hole2 = seat.transform.Find("Hole2")?.GetComponent<RectTransform>();

            if (hole1 == null || hole2 == null)
            {
                Debug.LogWarning($"Место {i}: не найдены Hole1 или Hole2");
                continue;
            }

            // Определяем поворот на основе позиции места
            Vector3 seatPos = seat.transform.localPosition;
            float angle = Mathf.Atan2(seatPos.y, seatPos.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            float rotation = 0f;
            string position = "";

            // Определяем поворот
            if (angle >= 315f || angle < 45f)      // Низ
            {
                rotation = 0f;
                position = "Низ";
            }
            else if (angle >= 45f && angle < 135f) // Право
            {
                rotation = 90f;
                position = "Право";
            }
            else if (angle >= 135f && angle < 225f) // Верх
            {
                rotation = 180f;
                position = "Верх";
            }
            else if (angle >= 225f && angle < 315f) // Лево
            {
                rotation = 270f;
                position = "Лево";
            }

            // Применяем поворот напрямую
            hole1.localRotation = Quaternion.Euler(0, 0, rotation);
            hole2.localRotation = Quaternion.Euler(0, 0, rotation);

            // Помечаем как измененные
            EditorUtility.SetDirty(hole1);
            EditorUtility.SetDirty(hole2);

            Debug.Log($"Место {i} ({seat.name}): угол={angle:F1}°, позиция={position}, поворот={rotation}°");
        }

        Debug.Log("=== ПОВОРОТ КАРТ ИСПРАВЛЕН! ===");
    }

    private void ShowAllAngles()
    {
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout == null) return;

        var seats = seatsLayout.GetComponentsInChildren<NewBehaviourScript>();
        Debug.Log("=== УГЛЫ ВСЕХ МЕСТ ===");

        for (int i = 0; i < seats.Length; i++)
        {
            var seat = seats[i];
            Vector3 pos = seat.transform.localPosition;
            float angle = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            Debug.Log($"Место {i}: позиция=({pos.x:F1}, {pos.y:F1}), угол={angle:F1}°");
        }
    }
}