using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class UserProfile
{
    [Header("Основная информация")]
    public string username;
    public string email;
    public string passwordHash; // Хешированный пароль
    public DateTime registrationDate;
    public DateTime lastLoginDate;
    public bool isLoggedIn;
    
    [Header("Игровые данные")]
    public int chips; // Игровые фишки
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
    
    [Header("Сессионные данные")]
    public int currentSessionChips; // Фишки в текущей сессии
    public int currentSessionGames;
    public DateTime sessionStartTime;
    
    public UserProfile()
    {
        username = "";
        email = "";
        passwordHash = "";
        registrationDate = DateTime.Now;
        lastLoginDate = DateTime.MinValue;
        isLoggedIn = false;
        
        // Игровые данные
        chips = 10000; // Стартовые фишки
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
        
        // Сессия
        currentSessionChips = 0;
        currentSessionGames = 0;
        sessionStartTime = DateTime.Now;
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
