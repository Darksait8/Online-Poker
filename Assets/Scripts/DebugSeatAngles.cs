using UnityEngine;

public class DebugSeatAngles : MonoBehaviour
{
    [ContextMenu("Debug Seat Angles")]
    public void DebugAngles()
    {
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout == null)
        {
            Debug.LogError("SeatsLayoutRadial не найден!");
            return;
        }

        // Получаем настройки через рефлексию
        var type = typeof(SeatsLayoutRadial);
        var maxSeatsField = type.GetField("maxSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var startAngleField = type.GetField("startAngleDeg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var clockwiseField = type.GetField("clockwise", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        int maxSeats = (int)maxSeatsField.GetValue(seatsLayout);
        float startAngleDeg = (float)startAngleField.GetValue(seatsLayout);
        bool clockwise = (bool)clockwiseField.GetValue(seatsLayout);

        Debug.Log($"=== Отладка углов мест ===");
        Debug.Log($"MaxSeats: {maxSeats}, StartAngle: {startAngleDeg}, Clockwise: {clockwise}");

        float dir = clockwise ? -1f : 1f;
        
        for (int i = 0; i < maxSeats; i++)
        {
            float t = i / (float)maxSeats;
            float angleDeg = startAngleDeg + dir * t * 360f;
            float norm = Mathf.Repeat(angleDeg, 360f);
            
            string position = "";
            float rot = 0f;
            
            if (norm >= 315f || norm < 45f)
            {
                position = "Низ";
                rot = 0f;
            }
            else if (norm >= 45f && norm < 135f)
            {
                position = "Право";
                rot = 90f;
            }
            else if (norm >= 135f && norm < 225f)
            {
                position = "Верх";
                rot = 180f;
            }
            else if (norm >= 225f && norm < 315f)
            {
                position = "Лево";
                rot = -90f;
            }

            Debug.Log($"Место {i + 1}: угол={norm:F1}°, позиция={position}, поворот карт={rot}°");
        }
    }
}