using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class UserProfile
{
    public const string CustomAvatarId = "custom";

    [Header("Основная информация")]
    public string username;
    public string email;
    public string passwordHash; // Хешированный пароль
    public DateTime registrationDate;
    public DateTime lastLoginDate;
    public bool isLoggedIn;
    
    [Header("Игровые данные")]
    public int chips; // Игровые фишки
    public int XP; // Опыт игрока
    public int totalGamesPlayed;
    public int gamesWon;
    public int gamesLost;
    public int totalWinnings;
    public int totalLosses;
    public int biggestWin;
    public int biggestLoss;
    public float winRate; // Процент побед
    
    [Header("Статистика игр")]
    public int handsPlayed;
    public int handsWon;
    public int handsLost;
    public int handsFolded;
    public int handsRaised;
    public int handsCalled;
    public int handsChecked;
    
    [Header("Настройки игрока")]
    public GameSettings gameSettings;
    
    [Header("Достижения")]
    public List<string> achievements;
    public List<string> unlockedAvatars;
    
    [Header("Аватар")]
    public string avatarId = "default";
    public string customAvatarPath;

    public int Level => PlayerProgressionService.GetLevel(XP);
    public int XpToNextLevel => PlayerProgressionService.GetXpToNextLevel(XP);
    public float LevelProgress => PlayerProgressionService.GetProgress01(XP);

    [Header("Друзья")]
    public List<string> friends;
    public List<FriendRequestData> incomingFriendRequests;
    public List<FriendRequestData> outgoingFriendRequests;
    
    [Header("Сессионные данные")]
    public int currentSessionChips; // Фишки в текущей сессии
    public int currentSessionGames;
    public DateTime sessionStartTime;
    
    [Header("Ограничения пополнения")]
    public int weeklyDepositLimit = 10000; // Недельный лимит пополнения
    public int currentWeekDeposits = 0; // Пополнения за текущую неделю
    public DateTime weekStartDate; // Начало текущей недели для отсчета лимита
    
    public UserProfile()
    {
        username = "";
        email = "";
        passwordHash = "";
        registrationDate = DateTime.Now;
        lastLoginDate = DateTime.MinValue;
        isLoggedIn = false;
        
        // Игровые данные
        chips = 1000; // Стартовые фишки
        XP = 0; // Стартовый опыт
        totalGamesPlayed = 0;
        gamesWon = 0;
        gamesLost = 0;
        totalWinnings = 0;
        totalLosses = 0;
        biggestWin = 0;
        biggestLoss = 0;
        winRate = 0f;
        
        // Статистика
        handsPlayed = 0;
        handsWon = 0;
        handsLost = 0;
        handsFolded = 0;
        handsRaised = 0;
        handsCalled = 0;
        handsChecked = 0;
        
        // Настройки
        gameSettings = new GameSettings();
        
        // Достижения
        achievements = new List<string>();
        unlockedAvatars = new List<string>();
        if (!unlockedAvatars.Contains("default"))
            unlockedAvatars.Add("default");

        friends = new List<string>();
        incomingFriendRequests = new List<FriendRequestData>();
        outgoingFriendRequests = new List<FriendRequestData>();
        
        // Сессия
        currentSessionChips = 0;
        currentSessionGames = 0;
        sessionStartTime = DateTime.Now;
        
        // Ограничения пополнения
        weeklyDepositLimit = 10000;
        currentWeekDeposits = 0;
        weekStartDate = GetStartOfWeek(DateTime.Now);
    }
    
    /// <summary>
    /// Обновляет статистику после игры
    /// </summary>
    public void UpdateGameStats(bool won, int chipsWon, int chipsLost)
    {
        totalGamesPlayed++;
        
        if (won)
        {
            gamesWon++;
            totalWinnings += chipsWon;
            if (chipsWon > biggestWin)
                biggestWin = chipsWon;
        }
        else
        {
            gamesLost++;
            totalLosses += chipsLost;
            if (chipsLost > biggestLoss)
                biggestLoss = chipsLost;
        }
        
        // Обновляем процент побед
        winRate = totalGamesPlayed > 0 ? (float)gamesWon / totalGamesPlayed * 100f : 0f;
        
        // Обновляем фишки
        chips += chipsWon - chipsLost;
        
        // Обновляем сессионные данные
        currentSessionChips += chipsWon - chipsLost;
        currentSessionGames++;
    }
    
    /// <summary>
    /// Обновляет статистику руки
    /// </summary>
    public void UpdateHandStats(HandResult result, HandAction action)
    {
        handsPlayed++;
        
        switch (result)
        {
            case HandResult.Won:
                handsWon++;
                break;
            case HandResult.Lost:
                handsLost++;
                break;
            case HandResult.Folded:
                handsFolded++;
                break;
        }
        
        switch (action)
        {
            case HandAction.Raise:
                handsRaised++;
                break;
            case HandAction.Call:
                handsCalled++;
                break;
            case HandAction.Check:
                handsChecked++;
                break;
        }
    }
    
    /// <summary>
    /// Начинает новую игровую сессию
    /// </summary>
    public void StartNewSession()
    {
        currentSessionChips = 0;
        currentSessionGames = 0;
        sessionStartTime = DateTime.Now;
    }
    
    /// <summary>
    /// Проверяет, разблокировано ли достижение
    /// </summary>
    public bool HasAchievement(string achievementId)
    {
        return achievements.Contains(achievementId);
    }
    
    /// <summary>
    /// Разблокирует достижение
    /// </summary>
    public void UnlockAchievement(string achievementId)
    {
        if (!achievements.Contains(achievementId))
        {
            achievements.Add(achievementId);
            Debug.Log($"Достижение разблокировано: {achievementId}");
        }
    }
    
    /// <summary>
    /// Проверяет, разблокирован ли аватар
    /// </summary>
    public bool HasAvatar(string avatarId)
    {
        return unlockedAvatars.Contains(avatarId);
    }
    
    /// <summary>
    /// Разблокирует аватар
    /// </summary>
    public void UnlockAvatar(string avatarId)
    {
        if (!unlockedAvatars.Contains(avatarId))
        {
            unlockedAvatars.Add(avatarId);
            Debug.Log($"Аватар разблокирован: {avatarId}");
        }
    }
    
    /// <summary>
    /// Устанавливает текущий аватар пользователя
    /// </summary>
    public void SetAvatar(string avatarId)
    {
        if (string.IsNullOrWhiteSpace(avatarId))
            return;
        
        this.avatarId = avatarId;
        if (avatarId != CustomAvatarId)
        {
            customAvatarPath = null;
        }
        UnlockAvatar(avatarId);
    }

    /// <summary>
    /// Устанавливает пользовательский аватар
    /// </summary>
    public void SetCustomAvatar(string avatarPath)
    {
        if (string.IsNullOrWhiteSpace(avatarPath))
            return;

        avatarId = CustomAvatarId;
        customAvatarPath = avatarPath;
        UnlockAvatar(avatarId);
    }
    
    /// <summary>
    /// Проверяет, можно ли пополнить на указанную сумму (не превышает недельный лимит)
    /// </summary>
    public bool CanDeposit(int amount)
    {
        CheckAndResetWeeklyLimit();
        return (currentWeekDeposits + amount) <= weeklyDepositLimit;
    }
    
    /// <summary>
    /// Возвращает оставшуюся сумму для пополнения на этой неделе
    /// </summary>
    public int GetRemainingWeeklyDeposit()
    {
        CheckAndResetWeeklyLimit();
        return Mathf.Max(0, weeklyDepositLimit - currentWeekDeposits);
    }
    
    /// <summary>
    /// Добавляет пополнение к недельному счетчику
    /// </summary>
    public bool AddDeposit(int amount)
    {
        CheckAndResetWeeklyLimit();
        
        if (!CanDeposit(amount))
        {
            Debug.LogWarning($"Попытка пополнить на {amount}, но превышен недельный лимит. Осталось: {GetRemainingWeeklyDeposit()}");
            return false;
        }
        
        currentWeekDeposits += amount;
        chips += amount;
        
        Debug.Log($"Пополнение на {amount} фишек. Использовано за неделю: {currentWeekDeposits}/{weeklyDepositLimit}");
        return true;
    }
    
    /// <summary>
    /// Проверяет, нужно ли сбросить недельный лимит (новая неделя)
    /// </summary>
    private void CheckAndResetWeeklyLimit()
    {
        DateTime currentWeekStart = GetStartOfWeek(DateTime.Now);
        
        if (currentWeekStart > weekStartDate)
        {
            // Новая неделя - сбрасываем счетчик
            currentWeekDeposits = 0;
            weekStartDate = currentWeekStart;
            Debug.Log("Недельный лимит пополнений сброшен - началась новая неделя");
        }
    }
    
    /// <summary>
    /// Возвращает начало недели (понедельник) для указанной даты
    /// </summary>
    public DateTime GetStartOfWeek(DateTime date)
    {
        // Получаем понедельник текущей недели
        int daysFromMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.Date.AddDays(-daysFromMonday);
    }
}

[System.Serializable]
public class FriendRequestData
{
    public string from;
    public string to;
    public long createdAtTicks;
}

[System.Serializable]
public class GameSettings
{
    [Header("Звук")]
    public float masterVolume = 1f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 1f;
    public bool muteAll = false;
    
    [Header("Графика")]
    public int qualityLevel = 2; // 0=Low, 1=Medium, 2=High
    public bool fullscreen = true;
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public int refreshRate = 60;
    
    [Header("Игровые настройки")]
    public bool autoFold = false;
    public bool autoCall = false;
    public bool showCards = true;
    public bool showAnimations = true;
    public bool showChat = true;
    public bool showPlayerStats = true;
    
    [Header("Интерфейс")]
    public AppLanguage language = AppLanguage.Russian;
    public float uiScale = 1f;
    public bool showTooltips = true;
    public bool compactMode = false;
    public float brightness = 1f;
    
    [Header("Внешний вид карт")]
    public string cardThemeId = "default";
    
    [Header("Уведомления")]
    public bool enableNotifications = true;
    public bool soundNotifications = true;
    public bool vibrationNotifications = false;
    
    public GameSettings()
    {
        // Значения по умолчанию уже установлены выше
    }
}

public enum HandResult
{
    Won,
    Lost,
    Folded,
    Tie
}

public enum HandAction
{
    Fold,
    Check,
    Call,
    Raise,
    AllIn
}
