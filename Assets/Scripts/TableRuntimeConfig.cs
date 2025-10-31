public static class TableRuntimeConfig
{
    public static bool HasConfig { get; private set; }
    public static int SmallBlind { get; private set; } = 500; // пример: SB, если нужен
    public static int BigBlind { get; private set; } = 1000;  // используем как стартовый блайнд
    public static int MaxSeats { get; private set; } = 6;

    public static void SetPreset(int bigBlind, int maxSeats)
    {
        BigBlind = bigBlind;
        SmallBlind = bigBlind / 2;
        MaxSeats = maxSeats;
        HasConfig = true;
    }

    public static void Clear()
    {
        HasConfig = false;
    }
}



