public static class TableRuntimeConfig
{
    public static bool HasConfig { get; private set; }
    public static int SmallBlind { get; private set; } = 10; // Малый блайнд (ставка до раздачи карт)
    public static int BigBlind { get; private set; } = 20;   // Большой блайнд (обычно в 2 раза больше малого)
    public static int MaxSeats { get; private set; } = 6;
    public static string TableId { get; private set; } = ""; // ID стола для онлайн-игры
    public static string TableName { get; private set; } = ""; // Название стола
    public static bool IsOnlineTable { get; private set; } = false; // Онлайн-стол или локальный
    public static TableDifficulty Difficulty { get; private set; } = TableDifficulty.Medium; // Уровень сложности

    public static void SetPreset(int smallBlind, int maxSeats, TableDifficulty difficulty = TableDifficulty.Medium)
    {
        SmallBlind = smallBlind;
        BigBlind = smallBlind * 2; // Большой блайнд всегда в 2 раза больше малого
        MaxSeats = maxSeats;
        Difficulty = difficulty;
        HasConfig = true;
    }
    
    public static void SetOnlineTable(string tableId, string tableName, int smallBlind, int maxSeats, TableDifficulty difficulty = TableDifficulty.Medium)
    {
        TableId = tableId;
        TableName = tableName;
        SmallBlind = smallBlind;
        BigBlind = smallBlind * 2;
        MaxSeats = maxSeats;
        Difficulty = difficulty;
        IsOnlineTable = true;
        HasConfig = true;
    }

    public static void Clear()
    {
        HasConfig = false;
        TableId = "";
        TableName = "";
        IsOnlineTable = false;
        Difficulty = TableDifficulty.Medium;
    }
}



