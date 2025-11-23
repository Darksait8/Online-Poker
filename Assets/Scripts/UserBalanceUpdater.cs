using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Утилита для обновления баланса всех пользователей до 1000 фишек
/// </summary>
public class UserBalanceUpdater : MonoBehaviour
{
    [Header("Настройки обновления")]
    [SerializeField] private int newDefaultBalance = 1000;
    [SerializeField] private bool updateOnStart = false;
    
    private void Start()
    {
        if (updateOnStart)
        {
            UpdateAllUsersBalance();
        }
    }
    
    /// <summary>
    /// Обновляет баланс всех пользователей до указанного значения
    /// </summary>
    [ContextMenu("Обновить баланс всех пользователей")]
    public void UpdateAllUsersBalance()
    {
        Debug.Log("UserBalanceUpdater: Начинаем обновление баланса всех пользователей...");
        
        List<UserProfile> allProfiles = UserDataManager.LoadAllProfiles();
        int updatedCount = 0;
        
        foreach (UserProfile profile in allProfiles)
        {
            if (profile == null) continue;
            
            int oldBalance = profile.chips;
            
            // НЕ изменяем баланс - оставляем как есть
            // profile.chips = newDefaultBalance; // ОТКЛЮЧЕНО
            
            // Сбрасываем недельные ограничения для всех пользователей
            profile.currentWeekDeposits = 0;
            profile.weekStartDate = profile.GetStartOfWeek(System.DateTime.Now);
            
            // Сохраняем профиль
            if (UserDataManager.SaveUserProfile(profile))
            {
                updatedCount++;
                Debug.Log($"UserBalanceUpdater: Обновлен пользователь {profile.username}: {oldBalance} -> {profile.chips} фишек");
            }
            else
            {
                Debug.LogError($"UserBalanceUpdater: Ошибка сохранения профиля {profile.username}");
            }
        }
        
        Debug.Log($"UserBalanceUpdater: Обновление завершено. Обновлено {updatedCount} из {allProfiles.Count} пользователей");
        
        // Если текущий пользователь авторизован, обновляем его в памяти
        UserProfile currentUser = AuthManager.CurrentUser;
        if (currentUser != null)
        {
            // Не изменяем баланс - только сбрасываем недельные лимиты
            currentUser.currentWeekDeposits = 0;
            currentUser.weekStartDate = currentUser.GetStartOfWeek(System.DateTime.Now);
            
            // Сохраняем текущего пользователя, что автоматически вызовет событие обновления
            AuthManager.SaveCurrentUser();
            
            Debug.Log($"UserBalanceUpdater: Текущий пользователь {currentUser.username} также обновлен");
        }
    }
    
    /// <summary>
    /// Сбрасывает недельные лимиты для всех пользователей
    /// </summary>
    [ContextMenu("Сбросить недельные лимиты")]
    public void ResetWeeklyLimitsForAllUsers()
    {
        Debug.Log("UserBalanceUpdater: Сброс недельных лимитов для всех пользователей...");
        
        List<UserProfile> allProfiles = UserDataManager.LoadAllProfiles();
        int updatedCount = 0;
        
        foreach (UserProfile profile in allProfiles)
        {
            if (profile == null) continue;
            
            // Сбрасываем недельные ограничения
            profile.currentWeekDeposits = 0;
            profile.weekStartDate = profile.GetStartOfWeek(System.DateTime.Now);
            
            // Сохраняем профиль
            if (UserDataManager.SaveUserProfile(profile))
            {
                updatedCount++;
                Debug.Log($"UserBalanceUpdater: Сброшены лимиты для {profile.username}");
            }
        }
        
        Debug.Log($"UserBalanceUpdater: Сброс лимитов завершен для {updatedCount} пользователей");
    }
    
    /// <summary>
    /// Показывает статистику по всем пользователям
    /// </summary>
    [ContextMenu("Показать статистику пользователей")]
    public void ShowUsersStatistics()
    {
        List<UserProfile> allProfiles = UserDataManager.LoadAllProfiles();
        
        Debug.Log($"=== СТАТИСТИКА ПОЛЬЗОВАТЕЛЕЙ ===");
        Debug.Log($"Всего пользователей: {allProfiles.Count}");
        
        int totalBalance = 0;
        int usersWithLimitExceeded = 0;
        
        foreach (UserProfile profile in allProfiles)
        {
            if (profile == null) continue;
            
            totalBalance += profile.chips;
            
            if (profile.currentWeekDeposits >= profile.weeklyDepositLimit)
            {
                usersWithLimitExceeded++;
            }
            
            Debug.Log($"Пользователь: {profile.username}, Баланс: {profile.chips}, " +
                     $"Пополнено за неделю: {profile.currentWeekDeposits}/{profile.weeklyDepositLimit}");
        }
        
        Debug.Log($"Общий баланс всех пользователей: {totalBalance}");
        Debug.Log($"Пользователей с исчерпанным лимитом: {usersWithLimitExceeded}");
        Debug.Log($"================================");
    }
}
