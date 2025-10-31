using UnityEngine;

public class FixCardRotations : MonoBehaviour
{
    [ContextMenu("Fix All Card Rotations")]
    public void FixAllCardRotations()
    {
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout == null)
        {
            Debug.LogError("SeatsLayoutRadial не найден!");
            return;
        }

        Debug.Log("=== Исправление поворота карт ===");

        // Получаем все места
        var seats = seatsLayout.GetComponentsInChildren<NewBehaviourScript>();
        
        // Получаем настройки через рефлексию
        var type = typeof(SeatsLayoutRadial);
        var startAngleField = type.GetField("startAngleDeg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var clockwiseField = type.GetField("clockwise", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var holeDistanceField = type.GetField("holeDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var holeSpacingField = type.GetField("holeSpacing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        float startAngleDeg = (float)startAngleField.GetValue(seatsLayout);
        bool clockwise = (bool)clockwiseField.GetValue(seatsLayout);
        float holeDistance = (float)holeDistanceField.GetValue(seatsLayout);
        float holeSpacing = (float)holeSpacingField.GetValue(seatsLayout);

        float dir = clockwise ? -1f : 1f;
        int maxSeats = seats.Length;

        for (int i = 0; i < seats.Length; i++)
        {
            var seat = seats[i];
            if (seat == null) continue;

            // Вычисляем угол для этого места
            float t = i / (float)maxSeats;
            float angleDeg = startAngleDeg + dir * t * 360f;
            float norm = Mathf.Repeat(angleDeg, 360f);
            float rad = Mathf.Deg2Rad * angleDeg;

            // Направление к центру стола
            Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));

            // Определяем правильный поворот
            float rot = 0f;
            string position = "";

            if (norm >= 315f || norm < 45f)        // Низ
            {
                rot = 0f;
                position = "Низ";
            }
            else if (norm >= 45f && norm < 135f)   // Право
            {
                rot = 90f;
                position = "Право";
            }
            else if (norm >= 135f && norm < 225f)  // Верх
            {
                rot = 180f;
                position = "Верх";
            }
            else if (norm >= 225f && norm < 315f)  // Лево
            {
                rot = 270f;
                position = "Лево";
            }

            // Применяем новый поворот
            seat.ConfigureHoleLayout(inward, rot, holeDistance, holeSpacing);
            
            Debug.Log($"Место {i + 1} ({seat.name}): угол={norm:F1}°, позиция={position}, поворот={rot}°");
        }

        Debug.Log("Поворот карт исправлен для всех мест!");
    }
}