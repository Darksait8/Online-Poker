using UnityEngine;

/// <summary>
/// Скрипт для тестирования поворота карт
/// Добавьте этот компонент на любой GameObject в сцене для тестирования
/// </summary>
public class CardRotationTester : MonoBehaviour
{
    [Header("Тестовые настройки")]
    [SerializeField] private bool testOnStart = true;
    
    private void Start()
    {
        if (testOnStart)
        {
            TestCardRotations();
        }
    }
    
    [ContextMenu("Test Card Rotations")]
    public void TestCardRotations()
    {
        Debug.Log("=== ТЕСТ ПОВОРОТА КАРТ ===");
        
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout == null)
        {
            Debug.LogError("SeatsLayoutRadial не найден!");
            return;
        }
        
        // Пересоздаем места для применения новых настроек поворота
        seatsLayout.SpawnSeats();
        
        Debug.Log("Места пересозданы с новыми настройками поворота карт");
        Debug.Log("=== КОНЕЦ ТЕСТА ===");
    }
    
    [ContextMenu("Log Current Card Rotations")]
    public void LogCurrentCardRotations()
    {
        Debug.Log("=== ТЕКУЩИЕ ПОВОРОТЫ КАРТ ===");
        
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout == null)
        {
            Debug.LogError("SeatsLayoutRadial не найден!");
            return;
        }
        
        for (int i = 0; i < seatsLayout.transform.childCount; i++)
        {
            var seat = seatsLayout.transform.GetChild(i);
            var ui = seat.GetComponent<NewBehaviourScript>();
            if (ui != null)
            {
                // Получаем информацию о картах через рефлексию
                var hole1Field = ui.GetType().GetField("hole1Image", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hole2Field = ui.GetType().GetField("hole2Image", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (hole1Field != null && hole1Field.GetValue(ui) is UnityEngine.UI.Image hole1)
                {
                    float rotation1 = hole1.rectTransform.localEulerAngles.z;
                    Debug.Log($"{seat.name} Hole1: поворот = {rotation1:F1}°");
                }
                
                if (hole2Field != null && hole2Field.GetValue(ui) is UnityEngine.UI.Image hole2)
                {
                    float rotation2 = hole2.rectTransform.localEulerAngles.z;
                    Debug.Log($"{seat.name} Hole2: поворот = {rotation2:F1}°");
                }
            }
        }
        
        Debug.Log("=== КОНЕЦ ЛОГИРОВАНИЯ ===");
    }
}
