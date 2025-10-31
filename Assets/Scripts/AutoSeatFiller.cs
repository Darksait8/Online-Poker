using UnityEngine;

public class AutoSeatFiller : MonoBehaviour
{
    [SerializeField, Range(0, 9)] private int playersToJoin = 2;
    [SerializeField] private int defaultStack = 1000;

    private void Start()
    {
        var seats = GetComponent<SeatsLayoutRadial>();
        if (seats == null) seats = FindObjectOfType<SeatsLayoutRadial>();
        if (seats == null) return;

        // Проверяем, есть ли TableConfigTester с настройками
        var configTester = FindObjectOfType<TableConfigTester>();
        int actualPlayersToJoin = playersToJoin;
        
        if (configTester != null)
        {
            // Получаем настройки из TableConfigTester через рефлексию
            var testerType = typeof(TableConfigTester);
            var testPlayersField = testerType.GetField("testPlayersToJoin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (testPlayersField != null)
            {
                int testPlayers = (int)testPlayersField.GetValue(configTester);
                actualPlayersToJoin = testPlayers;
                Debug.Log($"AutoSeatFiller: Используем настройки из TableConfigTester: {actualPlayersToJoin} игроков");
            }
        }

        // НЕ ограничиваем количество игроков максимальным количеством мест
        // Используем ТОЧНО то количество, которое указано в настройках
        Debug.Log($"AutoSeatFiller: Будем добавлять ТОЧНО {actualPlayersToJoin} игроков (не больше)");
        
        // ПРИНУДИТЕЛЬНО очищаем всех игроков перед добавлением новых
        var occupiedSeats = seats.GetOccupiedSeats();
        Debug.Log($"AutoSeatFiller: Найдено {occupiedSeats.Count} занятых мест");
        
        // Очищаем все места принудительно через SetPlayer
        for (int i = 0; i < seats.transform.childCount; i++)
        {
            var seat = seats.transform.GetChild(i);
            var ui = seat.GetComponent<NewBehaviourScript>();
            if (ui != null)
            {
                ui.SetPlayer("Свободно", 0);
                ui.ShowBet(0);
                ui.SetDealer(false);
                ui.HideHoles();
            }
        }
        Debug.Log($"AutoSeatFiller: Принудительно очистили все {seats.transform.childCount} мест");
        
        Debug.Log($"AutoSeatFiller: playersToJoin = {playersToJoin}, maxSeats = {seats.MaxSeats}, actualPlayersToJoin = {actualPlayersToJoin}");

        for (int i = 0; i < actualPlayersToJoin; i++)
        {
            seats.TryJoin($"Игрок {i + 1}", defaultStack);
        }
    }
}


