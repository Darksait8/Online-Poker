using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class DeleteUsersEditor
{
    [MenuItem("Tools/Poker/Delete All Users Except Victor and Artem", false, 100)]
    public static void DeleteAllUsersExceptVictorAndArtem()
    {
        // Подтверждение действия
        bool confirmed = EditorUtility.DisplayDialog(
            "Удаление пользователей",
            "Вы уверены, что хотите удалить всех пользователей кроме victor и artem?\n\nЭто действие нельзя отменить!",
            "Да, удалить",
            "Отмена"
        );

        if (!confirmed)
        {
            Debug.Log("DeleteUsersEditor: Операция отменена пользователем");
            return;
        }

        List<string> usersToKeep = new List<string> { "victor", "artem" };
        
        int deletedCount = AuthManager.DeleteAllUsersExcept(usersToKeep);
        
        if (deletedCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Удаление завершено",
                $"Удалено {deletedCount} пользователей.\n\nОставлены только: victor и artem",
                "OK"
            );
            Debug.Log($"DeleteUsersEditor: Успешно удалено {deletedCount} пользователей. Оставлены: victor, artem");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Результат",
                "Пользователи для удаления не найдены.\n\nВозможно, в базе уже только victor и artem.",
                "OK"
            );
            Debug.Log("DeleteUsersEditor: Пользователи для удаления не найдены");
        }
    }
}
