using UnityEngine;
using System;

public static class AuthManager
{
    private static UserProfile _currentUser;
    
    public static UserProfile CurrentUser
    {
        get
        {
            if (_currentUser == null)
            {
                LoadCurrentUser();
            }
            return _currentUser;
        }
    }
    
    public static bool IsLoggedIn => _currentUser != null && _currentUser.isLoggedIn;
    
    public static event Action<UserProfile> OnUserLoggedIn;
    public static event Action OnUserLoggedOut;
    public static event Action<string> OnAuthError;
    
    /// <summary>
    /// Инициализация системы авторизации
    /// </summary>
    public static void Initialize()
    {
        UserDataManager.Initialize();
        LoadCurrentUser();
    }
    
    /// <summary>
    /// Попытка входа с именем пользователя и паролем
    /// </summary>
    public static void Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            OnAuthError?.Invoke("Имя пользователя и пароль не могут быть пустыми");
            return;
        }
        
        UserProfile profile = UserDataManager.LoadUserProfile(username);
        
        if (profile == null)
        {
            OnAuthError?.Invoke("Пользователь не найден");
            return;
        }
        
        if (!UserDataManager.VerifyPassword(password, profile.passwordHash))
        {
            OnAuthError?.Invoke("Неверный пароль");
            return;
        }
        
        // Успешный вход
        _currentUser = profile;
        _currentUser.isLoggedIn = true;
        _currentUser.lastLoginDate = DateTime.Now;
        _currentUser.StartNewSession();
        
        UserDataManager.SaveUserProfile(_currentUser);
        OnUserLoggedIn?.Invoke(_currentUser);
    }
    
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public static void Register(string username, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            OnAuthError?.Invoke("Все поля должны быть заполнены");
            return;
        }
        
        if (password != confirmPassword)
        {
            OnAuthError?.Invoke("Пароли не совпадают");
            return;
        }
        
        if (password.Length < 6)
        {
            OnAuthError?.Invoke("Пароль должен содержать минимум 6 символов");
            return;
        }
        
        if (UserDataManager.ProfileExists(username))
        {
            OnAuthError?.Invoke("Пользователь с таким именем уже существует");
            return;
        }
        
        // Создаем нового пользователя
        _currentUser = new UserProfile
        {
            username = username,
            email = email,
            passwordHash = UserDataManager.HashPassword(password),
            registrationDate = DateTime.Now,
            lastLoginDate = DateTime.Now,
            isLoggedIn = true
        };
        
        _currentUser.StartNewSession();
        
        // Сохраняем профиль
        if (UserDataManager.SaveUserProfile(_currentUser))
        {
            OnUserLoggedIn?.Invoke(_currentUser);
        }
        else
        {
            OnAuthError?.Invoke("Ошибка сохранения профиля");
        }
    }
    
    /// <summary>
    /// Вход как гость
    /// </summary>
    public static void LoginAsGuest()
    {
        _currentUser = new UserProfile
        {
            username = "Guest_" + DateTime.Now.Ticks,
            email = "",
            passwordHash = "",
            registrationDate = DateTime.Now,
            lastLoginDate = DateTime.Now,
            isLoggedIn = true
        };
        
        _currentUser.StartNewSession();
        OnUserLoggedIn?.Invoke(_currentUser);
    }
    
    /// <summary>
    /// Выход из системы
    /// </summary>
    public static void Logout()
    {
        if (_currentUser != null)
        {
            _currentUser.isLoggedIn = false;
            UserDataManager.SaveUserProfile(_currentUser);
        }
        
        _currentUser = null;
        OnUserLoggedOut?.Invoke();
    }
    
    /// <summary>
    /// Загружает текущего пользователя из сохраненных данных
    /// </summary>
    private static void LoadCurrentUser()
    {
        // Пытаемся загрузить последнего авторизованного пользователя
        var usernames = UserDataManager.GetAllUsernames();
        
        foreach (string username in usernames)
        {
            UserProfile profile = UserDataManager.LoadUserProfile(username);
            if (profile != null && profile.isLoggedIn)
            {
                _currentUser = profile;
                break;
            }
        }
    }
    
    /// <summary>
    /// Сохраняет текущего пользователя
    /// </summary>
    public static void SaveCurrentUser()
    {
        if (_currentUser != null)
        {
            UserDataManager.SaveUserProfile(_currentUser);
        }
    }
    
    /// <summary>
    /// Обновляет игровую статистику
    /// </summary>
    public static void UpdateGameStats(bool won, int chipsWon, int chipsLost)
    {
        if (_currentUser != null)
        {
            _currentUser.UpdateGameStats(won, chipsWon, chipsLost);
            SaveCurrentUser();
        }
    }
    
    /// <summary>
    /// Обновляет статистику руки
    /// </summary>
    public static void UpdateHandStats(HandResult result, HandAction action)
    {
        if (_currentUser != null)
        {
            _currentUser.UpdateHandStats(result, action);
            SaveCurrentUser();
        }
    }
    
    /// <summary>
    /// Получает игровые фишки пользователя
    /// </summary>
    public static int GetUserChips()
    {
        return _currentUser?.chips ?? 0;
    }
    
    /// <summary>
    /// Устанавливает игровые фишки пользователя
    /// </summary>
    public static void SetUserChips(int chips)
    {
        if (_currentUser != null)
        {
            _currentUser.chips = chips;
            SaveCurrentUser();
        }
    }
    
    /// <summary>
    /// Получает настройки игры пользователя
    /// </summary>
    public static GameSettings GetGameSettings()
    {
        return _currentUser?.gameSettings ?? new GameSettings();
    }
    
    /// <summary>
    /// Устанавливает настройки игры пользователя
    /// </summary>
    public static void SetGameSettings(GameSettings settings)
    {
        if (_currentUser != null)
        {
            _currentUser.gameSettings = settings;
            SaveCurrentUser();
        }
    }
    
    /// <summary>
    /// Разблокирует достижение
    /// </summary>
    public static void UnlockAchievement(string achievementId)
    {
        if (_currentUser != null)
        {
            _currentUser.UnlockAchievement(achievementId);
            SaveCurrentUser();
        }
    }
    
    /// <summary>
    /// Разблокирует аватар
    /// </summary>
    public static void UnlockAvatar(string avatarId)
    {
        if (_currentUser != null)
        {
            _currentUser.UnlockAvatar(avatarId);
            SaveCurrentUser();
        }
    }
    
    /// <summary>
    /// Очищает все данные пользователей (для тестирования)
    /// </summary>
    public static void ClearAllUserData()
    {
        UserDataManager.ClearAllData();
        _currentUser = null;
    }
    
    /// <summary>
    /// Получает информацию о системе данных
    /// </summary>
    public static string GetDataInfo()
    {
        return UserDataManager.GetDataInfo();
    }
}
