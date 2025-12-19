using System;

[Serializable]
public class TableInvite
{
    public string tableId; // ID стола (можно использовать tableName + creatorId как уникальный идентификатор)
    public string tableName;
    public string creatorId;
    public string creatorUsername;
    public string invitedUserId; // ID приглашенного пользователя
    public string invitedUsername; // Имя приглашенного пользователя
    public DateTime createdAt;
    public bool isAccepted;
    public bool isDeclined;

    public TableInvite(string tableId, string tableName, string creatorId, string creatorUsername, string invitedUserId, string invitedUsername)
    {
        this.tableId = tableId;
        this.tableName = tableName;
        this.creatorId = creatorId;
        this.creatorUsername = creatorUsername;
        this.invitedUserId = invitedUserId;
        this.invitedUsername = invitedUsername;
        this.createdAt = DateTime.Now;
        this.isAccepted = false;
        this.isDeclined = false;
    }

    public string GetDisplayMessage()
    {
        return $"{creatorUsername} приглашает вас присоединиться к столу \"{tableName}\"";
    }
}

