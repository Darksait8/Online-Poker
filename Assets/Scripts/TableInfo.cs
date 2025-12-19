using System;

[Serializable]
public class TableInfo
{
    public string tableName;
    public int smallBlind; // Малый блайнд (ставка до раздачи карт)
    public int bigBlind;   // Большой блайнд (обычно в 2 раза больше малого)
    public int maxSeats;
    public bool isDefault; // true для стандартных столов, false для пользовательских
    public string creatorId; // ID создателя (для пользовательских столов)
    public DateTime createdAt; // Дата создания
    public bool isPrivate; // true для закрытых столов, false для открытых
    public string password; // Пароль для закрытого стола (может быть пустым, если доступ только по инвайту)
    public string tableId; // Уникальный ID стола для онлайн игры
    public TableDifficulty difficulty; // Уровень сложности стола

    public TableInfo(string name, int smallBlind, int seats, bool isDefault = false, string creatorId = null, bool isPrivate = false, string password = null, TableDifficulty difficulty = TableDifficulty.Medium)
    {
        this.tableName = name;
        this.smallBlind = smallBlind;
        this.bigBlind = smallBlind * 2; // Большой блайнд всегда в 2 раза больше малого
        this.maxSeats = seats;
        this.isDefault = isDefault;
        this.creatorId = creatorId;
        this.createdAt = DateTime.Now;
        this.isPrivate = isPrivate;
        this.password = password;
        this.difficulty = difficulty;
    }

    public string GetDisplayName()
    {
        string privacy = isPrivate ? "🔒" : "🌐";
        string diffIcon = difficulty == TableDifficulty.Easy ? "🟢" : (difficulty == TableDifficulty.Medium ? "🟡" : "🔴");
        return $"{privacy} {diffIcon} {tableName} (Блайнд: {smallBlind}/{bigBlind}, Мест: {maxSeats})";
    }

    public bool RequiresPassword()
    {
        return isPrivate && !string.IsNullOrEmpty(password);
    }
}

