using UnityEngine;

/// <summary>
/// Скрипт для принудительного исправления количества игроков
/// Добавьте этот компонент на любой GameObject в сцене
/// </summary>
public class PlayerCountFixer : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private int targetPlayerCount = 3;
    [SerializeField] private bool fixOnStart = true;
    
    private void Start()
    {
        if (fixOnStart)
        {
            FixPlayerCount();
        }
    }
    
    [ContextMenu("Fix Player Count")]
    public void FixPlayerCount()
    {
        Debug.Log($"=== ИСПРАВЛЕНИЕ КОЛИЧЕСТВА ИГРОКОВ: {targetPlayerCount} ===");
        
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout == null)
        {
            Debug.LogError("SeatsLayoutRadial не найден!");
            return;
        }
        
        // Очищаем всех игроков принудительно
        Debug.Log($"Очищаем все места...");
        
        // Очищаем все места принудительно через SetPlayer
        for (int i = 0; i < seatsLayout.transform.childCount; i++)
        {
            var seat = seatsLayout.transform.GetChild(i);
            var ui = seat.GetComponent<NewBehaviourScript>();
            if (ui != null)
            {
                ui.SetPlayer("Свободно", 0);
                ui.ShowBet(0);
                ui.SetDealer(false);
                ui.HideHoles();
            }
        }
        Debug.Log($"Принудительно очистили все {seatsLayout.transform.childCount} мест");
        
        // Добавляем нужное количество игроков
        for (int i = 0; i < targetPlayerCount; i++)
        {
            seatsLayout.TryJoin($"Игрок {i + 1}", 1000);
        }
        Debug.Log($"Добавили {targetPlayerCount} игроков");
        
        Debug.Log("=== ИСПРАВЛЕНИЕ ЗАВЕРШЕНО ===");
    }
    
    [ContextMenu("Clear All Players")]
    public void ClearAllPlayers()
    {
        Debug.Log("=== ОЧИСТКА ВСЕХ ИГРОКОВ ===");
        
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout == null)
        {
            Debug.LogError("SeatsLayoutRadial не найден!");
            return;
        }
        
        var occupiedSeats = seatsLayout.GetOccupiedSeats();
        for (int i = occupiedSeats.Count - 1; i >= 0; i--)
        {
            seatsLayout.Leave($"Игрок {i + 1}");
        }
        Debug.Log($"Очистили {occupiedSeats.Count} игроков");
    }
}
