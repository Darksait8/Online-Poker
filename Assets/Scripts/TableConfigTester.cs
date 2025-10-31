using UnityEngine;

/// <summary>
/// Скрипт для тестирования конфигурации стола
/// Добавьте этот компонент на любой GameObject в сцене для тестирования
/// </summary>
public class TableConfigTester : MonoBehaviour
{
    [Header("Тестовые настройки")]
    [SerializeField] private bool testOnStart = true;
    [SerializeField] private int testMaxSeats = 9;
    [SerializeField] private int testPlayersToJoin = 9;
    
    private void Awake()
    {
        if (testOnStart)
        {
            // Принудительно обновляем AutoSeatFiller ПЕРЕД его запуском
            var autoSeatFiller = FindObjectOfType<AutoSeatFiller>();
            if (autoSeatFiller != null)
            {
                var fillerType = typeof(AutoSeatFiller);
                var playersField = fillerType.GetField("playersToJoin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (playersField != null)
                {
                    playersField.SetValue(autoSeatFiller, testPlayersToJoin);
                    Debug.Log($"TableConfigTester: Обновил AutoSeatFiller в Awake: playersToJoin = {testPlayersToJoin}");
                }
            }
            
            // Очищаем TableRuntimeConfig чтобы использовать наши настройки
            TableRuntimeConfig.Clear();
            TableRuntimeConfig.SetPreset(1000, testMaxSeats);
            Debug.Log($"TableConfigTester: Установлен конфиг: MaxSeats = {testMaxSeats}, PlayersToJoin = {testPlayersToJoin}");
        }
    }
    
    private void Start()
    {
        if (testOnStart)
        {
            // Используем UnifiedPlayerManager для управления игроками
            var playerManager = FindObjectOfType<UnifiedPlayerManager>();
            if (playerManager != null)
            {
                playerManager.SetTargetPlayerCount(testPlayersToJoin);
                Debug.Log($"TableConfigTester: Используем UnifiedPlayerManager для {testPlayersToJoin} игроков");
            }
            else
            {
                Debug.LogWarning("TableConfigTester: UnifiedPlayerManager не найден, используем старую логику");
                // Fallback к старой логике
                var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
                if (seatsLayout != null)
                {
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
                    Debug.Log($"TableConfigTester: Принудительно очистили все {seatsLayout.transform.childCount} мест");
                    
                    // Добавляем нужное количество игроков
                    for (int i = 0; i < testPlayersToJoin; i++)
                    {
                        seatsLayout.TryJoin($"Игрок {i + 1}", 1000);
                    }
                    Debug.Log($"TableConfigTester: Принудительно установлено {testPlayersToJoin} игроков");
                }
            }
            
            TestTableConfiguration();
        }
    }
    
    [ContextMenu("Test Table Configuration")]
    public void TestTableConfiguration()
    {
        Debug.Log("=== ТЕСТ КОНФИГУРАЦИИ СТОЛА ===");
        
        // Очищаем предыдущий конфиг
        TableRuntimeConfig.Clear();
        
        // Устанавливаем тестовый конфиг
        TableRuntimeConfig.SetPreset(1000, testMaxSeats);
        
        Debug.Log($"TableRuntimeConfig установлен: MaxSeats = {TableRuntimeConfig.MaxSeats}, HasConfig = {TableRuntimeConfig.HasConfig}");
        
        // Находим компоненты
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        var autoSeatFiller = FindObjectOfType<AutoSeatFiller>();
        
        if (seatsLayout != null)
        {
            Debug.Log($"SeatsLayoutRadial найден: MaxSeats = {seatsLayout.MaxSeats}, OccupiedCount = {seatsLayout.OccupiedCount}");
        }
        else
        {
            Debug.LogError("SeatsLayoutRadial не найден!");
        }
        
        if (autoSeatFiller != null)
        {
            // Обновляем настройки AutoSeatFiller
            var fillerType = typeof(AutoSeatFiller);
            var playersField = fillerType.GetField("playersToJoin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playersField != null)
            {
                playersField.SetValue(autoSeatFiller, testPlayersToJoin);
                Debug.Log($"AutoSeatFiller обновлен: playersToJoin = {testPlayersToJoin}");
            }
        }
        else
        {
            Debug.LogError("AutoSeatFiller не найден!");
        }
        
        Debug.Log("=== КОНЕЦ ТЕСТА ===");
    }
    
    [ContextMenu("Clear Table Config")]
    public void ClearTableConfig()
    {
        TableRuntimeConfig.Clear();
        Debug.Log("TableRuntimeConfig очищен");
    }
    
    [ContextMenu("Force Update Player Count")]
    public void ForceUpdatePlayerCount()
    {
        Debug.Log("=== ПРИНУДИТЕЛЬНОЕ ОБНОВЛЕНИЕ КОЛИЧЕСТВА ИГРОКОВ ===");
        
        var autoSeatFiller = FindObjectOfType<AutoSeatFiller>();
        if (autoSeatFiller != null)
        {
            // Обновляем настройки AutoSeatFiller
            var fillerType = typeof(AutoSeatFiller);
            var playersField = fillerType.GetField("playersToJoin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playersField != null)
            {
                playersField.SetValue(autoSeatFiller, testPlayersToJoin);
                Debug.Log($"AutoSeatFiller обновлен: playersToJoin = {testPlayersToJoin}");
            }
            
            // Очищаем текущих игроков и добавляем новых
            var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
            if (seatsLayout != null)
            {
                // Удаляем всех текущих игроков
                var occupiedSeats = seatsLayout.GetOccupiedSeats();
                for (int i = occupiedSeats.Count - 1; i >= 0; i--)
                {
                    seatsLayout.Leave($"Игрок {i + 1}");
                }
                
                // Добавляем нужное количество игроков
                for (int i = 0; i < testPlayersToJoin; i++)
                {
                    seatsLayout.TryJoin($"Игрок {i + 1}", 1000);
                }
                Debug.Log($"Добавлено {testPlayersToJoin} игроков");
            }
        }
        else
        {
            Debug.LogError("AutoSeatFiller не найден!");
        }
        
        Debug.Log("=== КОНЕЦ ОБНОВЛЕНИЯ ===");
    }
    
    [ContextMenu("Restart Game With New Settings")]
    public void RestartGameWithNewSettings()
    {
        Debug.Log("=== ПЕРЕЗАПУСК ИГРЫ С НОВЫМИ НАСТРОЙКАМИ ===");
        
        // Очищаем конфиг
        TableRuntimeConfig.Clear();
        
        // Устанавливаем новый конфиг
        TableRuntimeConfig.SetPreset(1000, testMaxSeats);
        
        // Пересоздаем места
        var seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout != null)
        {
            seatsLayout.SpawnSeats();
            Debug.Log("Места пересозданы");
        }
        
        // Перезапускаем AutoSeatFiller
        var autoSeatFiller = FindObjectOfType<AutoSeatFiller>();
        if (autoSeatFiller != null)
        {
            // Обновляем настройки
            var fillerType = typeof(AutoSeatFiller);
            var playersField = fillerType.GetField("playersToJoin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playersField != null)
            {
                playersField.SetValue(autoSeatFiller, testPlayersToJoin);
            }
            
            // Запускаем логику добавления игроков напрямую
            var seats = FindObjectOfType<SeatsLayoutRadial>();
            if (seats != null)
            {
                for (int i = 0; i < testPlayersToJoin; i++)
                {
                    seats.TryJoin($"Игрок {i + 1}", 1000);
                }
                Debug.Log($"Добавлено {testPlayersToJoin} игроков напрямую");
            }
        }
        
        Debug.Log("=== КОНЕЦ ПЕРЕЗАПУСКА ===");
    }
}
