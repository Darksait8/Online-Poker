using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер для отправки инвайтов к столу
/// </summary>
public static class TableInviteManager
{
    /// <summary>
    /// Отправляет инвайт к столу пользователю
    /// </summary>
    public static void SendTableInvite(TableInfo table, string invitedUserId, string invitedUsername)
    {
        if (table == null)
        {
            Debug.LogError("TableInviteManager: table не может быть null");
            return;
        }

        if (string.IsNullOrEmpty(invitedUserId))
        {
            Debug.LogError("TableInviteManager: invitedUserId не может быть пустым");
            return;
        }

        if (!AuthManager.IsLoggedIn)
        {
            Debug.LogError("TableInviteManager: Пользователь не авторизован");
            return;
        }

        string creatorId = AuthManager.CurrentUser?.username;
        string creatorUsername = AuthManager.CurrentUser?.username;

        if (string.IsNullOrEmpty(creatorId))
        {
            Debug.LogError("TableInviteManager: Не удалось получить имя пользователя создателя");
            return;
        }

        string tableId = $"{table.tableName}_{creatorId}";
        TableInvite invite = new TableInvite(tableId, table.tableName, creatorId, creatorUsername, invitedUserId, invitedUsername);

        // Добавляем инвайт через TableListController
        TableListController.AddInvite(invite);

        Debug.Log($"Инвайт к столу '{table.tableName}' отправлен пользователю {invitedUsername} (локально)");
    }

    /// <summary>
    /// Получает список всех созданных пользователем столов
    /// </summary>
    public static List<TableInfo> GetUserCreatedTables()
    {
        List<TableInfo> userTables = new List<TableInfo>();
        
        if (!AuthManager.IsLoggedIn)
            return userTables;

        string creatorId = AuthManager.CurrentUser?.username;
        if (string.IsNullOrEmpty(creatorId))
            return userTables;

        // Загружаем сохраненные столы
        string savedTablesJson = PlayerPrefs.GetString("UserCreatedTables", "");
        if (!string.IsNullOrEmpty(savedTablesJson))
        {
            try
            {
                TableListData data = JsonUtility.FromJson<TableListData>(savedTablesJson);
                if (data != null && data.tables != null)
                {
                    foreach (var table in data.tables)
                    {
                        if (table.creatorId == creatorId)
                        {
                            userTables.Add(table);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Не удалось загрузить созданные столы: {e.Message}");
            }
        }

        return userTables;
    }
}

