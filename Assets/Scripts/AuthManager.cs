using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using System.Threading.Tasks;

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
    public static event Action<UserProfile> OnUserProfileChanged;
    public static event Action<List<string>> OnFriendsChanged;
    public static event Action OnFriendRequestsChanged;
    
    /// <summary>
    /// Инициализация системы авторизации
    /// </summary>
    public static void Initialize()
    {
        UserDataManager.Initialize();
        LoadCurrentUser();
        if (_currentUser != null)
        {
            EnsureCardThemeApplied(_currentUser.gameSettings);
            OnUserProfileChanged?.Invoke(_currentUser);
        }
        else
        {
            EnsureCardThemeApplied(null);
        }
        
        // Инициализируем UGS и загружаем профиль из облака (если доступно)
        InitializeUGSAsync();
    }
    
    /// <summary>
    /// Асинхронная инициализация UGS и загрузка профиля из облака
    /// </summary>
    private static async void InitializeUGSAsync()
    {
        try
        {
            // Ждем инициализации UGS
            if (UGSServiceManager.Instance != null)
            {
                if (!UGSServiceManager.Instance.IsInitialized)
                {
                    await UGSServiceManager.Instance.InitializeAsync();
                }
                
                // Если не авторизован, делаем анонимный вход
                if (!UGSServiceManager.Instance.IsSignedIn)
                {
                    await UGSServiceManager.Instance.SignInAnonymousAsync();
                }
                
                // Загружаем профиль из облака, если он есть
                if (UGSServiceManager.Instance.IsSignedIn && UGSCloudSaveManager.Instance != null)
                {
                    var cloudProfile = await UGSCloudSaveManager.Instance.LoadPlayerProfileAsync();
                    if (cloudProfile != null && _currentUser != null)
                    {
                        // Синхронизируем данные из облака с локальным профилем
                        SyncProfileFromCloud(cloudProfile);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Не удалось инициализировать UGS: {e.Message}");
        }
    }
    
    /// <summary>
    /// Синхронизация профиля из облака с локальным профилем
    /// </summary>
    private static void SyncProfileFromCloud(UserProfile cloudProfile)
    {
        if (_currentUser == null || cloudProfile == null) return;
        
        // Обновляем данные из облака (приоритет облачным данным)
        _currentUser.chips = cloudProfile.chips;
        _currentUser.XP = cloudProfile.XP;
        _currentUser.totalGamesPlayed = cloudProfile.totalGamesPlayed;
        _currentUser.gamesWon = cloudProfile.gamesWon;
        _currentUser.gamesLost = cloudProfile.gamesLost;
        _currentUser.totalWinnings = cloudProfile.totalWinnings;
        _currentUser.totalLosses = cloudProfile.totalLosses;
        _currentUser.handsPlayed = cloudProfile.handsPlayed;
        _currentUser.handsWon = cloudProfile.handsWon;
        _currentUser.handsLost = cloudProfile.handsLost;
        
        SaveCurrentUser();
        OnUserProfileChanged?.Invoke(_currentUser);
        
        Debug.Log("Профиль синхронизирован из облака");
    }
    
    /// <summary>
    /// Попытка входа с именем пользователя и паролем
    /// </summary>
    /// <summary>
    /// Вход в систему (через UGS, с fallback на локальные аккаунты для старых пользователей)
    /// </summary>
    public static async void Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            OnAuthError?.Invoke("Имя пользователя и пароль не могут быть пустыми");
            return;
        }
        
        // Нормализуем username
        string normalizedUsername = username.Trim();
        
        Debug.Log($"Попытка входа: {normalizedUsername}");
        
        // Сначала пытаемся войти через UGS
        bool ugsSuccess = false;
        try
        {
            await LoginToUGSAsync(normalizedUsername, password);
            ugsSuccess = _currentUser != null && _currentUser.isLoggedIn;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Ошибка входа через UGS: {e.Message}");
        }
        
        // Если UGS не сработал, пробуем локальный аккаунт (для старых пользователей)
        if (!ugsSuccess)
        {
            Debug.Log($"Попытка входа через локальный аккаунт для {normalizedUsername}...");
            
            UserProfile profile = UserDataManager.LoadUserProfile(normalizedUsername);
            
            if (profile != null && UserDataManager.VerifyPassword(password, profile.passwordHash))
            {
                // Локальный аккаунт найден и пароль верный
                _currentUser = profile;
                _currentUser.isLoggedIn = true;
                _currentUser.lastLoginDate = DateTime.Now;
                _currentUser.StartNewSession();
                EnsureSocialCollections(_currentUser);
                EnsureCardThemeApplied(_currentUser.gameSettings);
                
                OnUserLoggedIn?.Invoke(_currentUser);
                OnUserProfileChanged?.Invoke(_currentUser);
                NotifySocialChanged();
                
                Debug.Log($"Вход выполнен через локальный аккаунт для {normalizedUsername}");
                
                // Пытаемся мигрировать в UGS (в фоне)
                MigrateLocalAccountToUGSAsync(normalizedUsername, password);
            }
            else
            {
                OnAuthError?.Invoke("Неверный логин или пароль");
            }
        }
    }
    
    /// <summary>
    /// Мигрирует локальный аккаунт в UGS (в фоне)
    /// </summary>
    private static async void MigrateLocalAccountToUGSAsync(string username, string password)
    {
        try
        {
            if (UGSServiceManager.Instance == null) return;
            
            if (!UGSServiceManager.Instance.IsInitialized)
            {
                await UGSServiceManager.Instance.InitializeAsync();
            }
            
            if (!UGSServiceManager.Instance.IsInitialized) return;
            
            // Пытаемся зарегистрироваться в UGS
            bool registered = await UGSServiceManager.Instance.RegisterWithUsernamePasswordAsync(username, password);
            
            if (registered && _currentUser != null)
            {
                // Сохраняем профиль в облако
                if (UGSCloudSaveManager.Instance != null)
                {
                    await UGSCloudSaveManager.Instance.SavePlayerProfileAsync(_currentUser);
                    Debug.Log($"Локальный аккаунт {username} мигрирован в UGS");
                }
            }
        }
        catch (System.Exception e)
        {
            // Игнорируем ошибки миграции - пользователь уже залогинен локально
            Debug.LogWarning($"Не удалось мигрировать аккаунт в UGS: {e.Message}");
        }
    }
    
    /// <summary>
    /// Регистрация нового пользователя (только через UGS, без локального хранения)
    /// </summary>
    public static async void Register(string username, string email, string password, string confirmPassword)
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
        
        // Нормализуем username
        string normalizedUsername = username.Trim();
        
        // Валидация username по требованиям UGS
        string usernameError = ValidateUsername(normalizedUsername);
        if (!string.IsNullOrEmpty(usernameError))
        {
            OnAuthError?.Invoke(usernameError);
            return;
        }
        
        // Валидация пароля по требованиям UGS
        string passwordError = ValidatePassword(password);
        if (!string.IsNullOrEmpty(passwordError))
        {
            OnAuthError?.Invoke(passwordError);
            return;
        }
        
        Debug.Log($"Регистрация через UGS: {normalizedUsername}");
        
        // Регистрируемся только через UGS
        await RegisterToUGSAsync(normalizedUsername, email, password);
    }
    
    /// <summary>
    /// Валидация username по требованиям UGS Authentication
    /// </summary>
    private static string ValidateUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return "Имя пользователя не может быть пустым";
        
        // Длина: минимум 3, максимум 20
        if (username.Length < 3)
            return "Имя пользователя должно содержать минимум 3 символа";
        
        if (username.Length > 20)
            return "Имя пользователя должно содержать максимум 20 символов";
        
        // Разрешенные символы: буквы, цифры, и символы: . - _ @
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9._@-]+$");
        if (!regex.IsMatch(username))
        {
            return "Имя пользователя может содержать только буквы, цифры и символы: . - _ @";
        }
        
        return null; // Валидация пройдена
    }
    
    /// <summary>
    /// Валидация пароля по требованиям UGS Authentication
    /// </summary>
    private static string ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "Пароль не может быть пустым";
        
        // Длина: минимум 8, максимум 30
        if (password.Length < 8)
            return "Пароль должен содержать минимум 8 символов";
        
        if (password.Length > 30)
            return "Пароль должен содержать максимум 30 символов";
        
        // Проверка наличия заглавной буквы
        bool hasUpper = false;
        // Проверка наличия строчной буквы
        bool hasLower = false;
        // Проверка наличия цифры
        bool hasDigit = false;
        // Проверка наличия символа
        bool hasSymbol = false;
        
        foreach (char c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSymbol = true;
        }
        
        if (!hasUpper)
            return "Пароль должен содержать минимум 1 заглавную букву (A-Z)";
        
        if (!hasLower)
            return "Пароль должен содержать минимум 1 строчную букву (a-z)";
        
        if (!hasDigit)
            return "Пароль должен содержать минимум 1 цифру (0-9)";
        
        if (!hasSymbol)
            return "Пароль должен содержать минимум 1 символ (например: ! @ # $ % и т.д.)";
        
        return null; // Валидация пройдена
    }
    
    /// <summary>
    /// Вход как гость
    /// </summary>
    public static async void LoginAsGuest()
    {
        // Вход как гость через UGS анонимный вход
        try
        {
            if (UGSServiceManager.Instance == null)
            {
                OnAuthError?.Invoke("Сервис авторизации недоступен. Попробуйте позже.");
                return;
            }
            
            // Инициализируем UGS, если нужно
            if (!UGSServiceManager.Instance.IsInitialized)
            {
                bool initialized = await UGSServiceManager.Instance.InitializeAsync();
                if (!initialized)
                {
                    OnAuthError?.Invoke("Ошибка инициализации сервиса. Проверьте интернет-соединение.");
                    return;
                }
            }
            
            // Входим анонимно
            bool signedIn = await UGSServiceManager.Instance.SignInAnonymousAsync();
            if (!signedIn || !UGSServiceManager.Instance.IsSignedIn)
            {
                OnAuthError?.Invoke("Ошибка входа как гость");
                return;
            }
            
            // Загружаем или создаем профиль гостя
            UserProfile guestProfile = null;
            if (UGSCloudSaveManager.Instance != null)
            {
                guestProfile = await UGSCloudSaveManager.Instance.LoadPlayerProfileAsync();
            }
            
            if (guestProfile == null)
            {
                // Создаем новый профиль гостя
                guestProfile = new UserProfile
                {
                    username = "Guest_" + DateTime.Now.Ticks,
                    email = "",
                    registrationDate = DateTime.Now,
                    lastLoginDate = DateTime.Now,
                    isLoggedIn = true
                };
                
                guestProfile.StartNewSession();
                EnsureSocialCollections(guestProfile);
                
                // Сохраняем в облако
                if (UGSCloudSaveManager.Instance != null)
                {
                    await UGSCloudSaveManager.Instance.SavePlayerProfileAsync(guestProfile);
                }
            }
            
            _currentUser = guestProfile;
            EnsureCardThemeApplied(_currentUser.gameSettings);
            OnUserLoggedIn?.Invoke(_currentUser);
            OnUserProfileChanged?.Invoke(_currentUser);
            NotifySocialChanged();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка входа как гость: {e.Message}");
            OnAuthError?.Invoke($"Ошибка входа как гость: {e.Message}");
        }
    }
    
    /// <summary>
    /// Выход из системы
    /// </summary>
    public static async void Logout()
    {
        if (_currentUser != null)
        {
            _currentUser.isLoggedIn = false;
            // Сохраняем в облако перед выходом
            await SaveCurrentUserAsync();
        }
        
        // Выходим из UGS
        if (UGSServiceManager.Instance != null && UGSServiceManager.Instance.IsSignedIn)
        {
            UGSServiceManager.Instance.SignOut();
        }
        
        _currentUser = null;
        OnUserLoggedOut?.Invoke();
        OnUserProfileChanged?.Invoke(null);
        OnFriendsChanged?.Invoke(new List<string>());
        OnFriendRequestsChanged?.Invoke();
    }
    
    /// <summary>
    /// Загружает текущего пользователя из сохраненных данных
    /// </summary>
    private static async void LoadCurrentUser()
    {
        // Не загружаем локально - только из облака
        // Если UGS доступен и пользователь залогинен, загружаем из облака
        if (UGSServiceManager.Instance != null && UGSServiceManager.Instance.IsSignedIn && UGSCloudSaveManager.Instance != null)
        {
            try
            {
                var cloudProfile = await UGSCloudSaveManager.Instance.LoadPlayerProfileAsync();
                if (cloudProfile != null)
                {
                    _currentUser = cloudProfile;
                    EnsureSocialCollections(_currentUser);
                    NotifySocialChanged();
                    Debug.Log("Профиль загружен из облака при старте");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Не удалось загрузить профиль из облака: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Устанавливает текущего пользователя (для использования с серверной синхронизацией)
    /// </summary>
    public static void SetCurrentUser(UserProfile user)
    {
        _currentUser = user;
        if (user != null)
        {
            EnsureSocialCollections(_currentUser);
            EnsureCardThemeApplied(_currentUser.gameSettings);
            OnUserLoggedIn?.Invoke(_currentUser);
            OnUserProfileChanged?.Invoke(_currentUser);
            NotifySocialChanged();
        }
    }
    
    /// <summary>
    /// Сохраняет текущего пользователя в облако (UGS Cloud Save)
    /// </summary>
    public static async System.Threading.Tasks.Task SaveCurrentUserAsync()
    {
        if (_currentUser != null && UGSCloudSaveManager.Instance != null && UGSServiceManager.Instance != null && UGSServiceManager.Instance.IsSignedIn)
        {
            try
            {
                await UGSCloudSaveManager.Instance.SavePlayerProfileAsync(_currentUser);
                Debug.Log("Профиль сохранен в облако");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка сохранения профиля в облако: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Сохраняет текущего пользователя в облако (синхронная обертка для обратной совместимости)
    /// </summary>
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static void SaveCurrentUser()
    {
        // Вызываем async версию в фоне, не блокируя выполнение
        if (_currentUser != null)
        {
            _ = SaveCurrentUserAsync();
        }
    }
#pragma warning restore CS4014
    
    /// <summary>
    /// Обновляет игровую статистику
    /// </summary>
    public static void UpdateGameStats(bool won, int chipsWon, int chipsLost)
    {
        if (_currentUser != null)
        {
            _currentUser.UpdateGameStats(won, chipsWon, chipsLost);
            SaveCurrentUser();
            
            // Синхронизируем профиль через Photon, если играем онлайн
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.UpdatePlayerProfile();
            }
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
    /// Обновляет баланс игрока (аналогично SetUserChips, но с другим именем для консистентности)
    /// </summary>
    public static void UpdatePlayerBalance(int newBalance)
    {
        if (_currentUser != null)
        {
            int oldBalance = _currentUser.chips;
            _currentUser.chips = Mathf.Max(0, newBalance);
            SaveCurrentUser();
            
            Debug.Log($"AuthManager: Баланс обновлен {oldBalance} -> {newBalance}");
            
            OnUserProfileChanged?.Invoke(_currentUser);
            
            // Синхронизируем профиль через Photon, если играем онлайн
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.UpdatePlayerProfile();
            }
            
            // Сохраняем в Cloud Save через UGS (если доступно)
            SaveToCloudSaveAsync();
            
            // Обновляем рейтинг в таблице лидеров через UGS (если доступно)
            UpdateLeaderboardAsync();
        }
    }
    
    /// <summary>
    /// Асинхронное сохранение в Cloud Save
    /// </summary>
    private static async void SaveToCloudSaveAsync()
    {
        if (_currentUser == null) return;
        
        try
        {
            if (UGSServiceManager.Instance != null && UGSServiceManager.Instance.IsSignedIn)
            {
                if (UGSCloudSaveManager.Instance != null)
                {
                    await UGSCloudSaveManager.Instance.SavePlayerProfileAsync(_currentUser);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Не удалось сохранить в Cloud Save: {e.Message}");
        }
    }
    
    /// <summary>
    /// Асинхронное обновление таблицы лидеров
    /// </summary>
    private static async void UpdateLeaderboardAsync()
    {
        if (_currentUser == null) return;
        
        try
        {
            if (UGSServiceManager.Instance != null && UGSServiceManager.Instance.IsSignedIn)
            {
                if (UGSLeaderboardManager.Instance != null)
                {
                    await UGSLeaderboardManager.Instance.UpdatePlayerRatingFromProfile(_currentUser);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Не удалось обновить таблицу лидеров: {e.Message}");
        }
    }
    
    /// <summary>
    /// Добавляет XP игроку
    /// </summary>
    public static void AddPlayerXp(int xpAmount)
    {
        if (_currentUser != null && xpAmount > 0)
        {
            _currentUser.XP = Mathf.Max(0, _currentUser.XP + xpAmount);
            SaveCurrentUser();
            OnUserProfileChanged?.Invoke(_currentUser);
            
            // Синхронизируем профиль через Photon, если играем онлайн
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.UpdatePlayerProfile();
            }
            
            // Сохраняем в Cloud Save через UGS (если доступно)
            SaveToCloudSaveAsync();
            
            // Обновляем рейтинг в таблице лидеров через UGS (если доступно)
            UpdateLeaderboardAsync();
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
            EnsureCardThemeApplied(settings);
            OnUserProfileChanged?.Invoke(_currentUser);
        }
    }
    
    /// <summary>
    /// Обновляет никнейм пользователя
    /// </summary>
    public static void UpdateNickname(string newNickname)
    {
        if (_currentUser == null) return;
        if (string.IsNullOrWhiteSpace(newNickname)) return;
        
        string trimmed = newNickname.Trim();
        if (trimmed.Length == 0) return;
        
        _currentUser.username = trimmed;
        SaveCurrentUser();
        OnUserProfileChanged?.Invoke(_currentUser);
    }
    
    /// <summary>
    /// Обновляет аватар пользователя
    /// </summary>
    public static void UpdateAvatar(string avatarId)
    {
        if (_currentUser == null) return;
        if (string.IsNullOrWhiteSpace(avatarId)) return;
        
        _currentUser.SetAvatar(avatarId.Trim());
        SaveCurrentUser();
        OnUserProfileChanged?.Invoke(_currentUser);
    }

    /// <summary>
    /// Обновляет пользовательский аватар из файла
    /// </summary>
    public static void UpdateCustomAvatar(string sourcePath)
    {
        if (_currentUser == null) return;
        if (string.IsNullOrWhiteSpace(sourcePath)) return;

        string importedPath = CustomAvatarManager.ImportAvatar(_currentUser.username, sourcePath);
        if (string.IsNullOrEmpty(importedPath))
        {
            Debug.LogWarning("Не удалось импортировать пользовательский аватар.");
            return;
        }

        Debug.Log($"[AuthManager] Импортирован пользовательский аватар: {importedPath}");

        if (!string.IsNullOrEmpty(_currentUser.customAvatarPath) &&
            _currentUser.customAvatarPath != importedPath)
        {
            CustomAvatarManager.ReleaseSprite(_currentUser.customAvatarPath);
            try
            {
                if (File.Exists(_currentUser.customAvatarPath))
                    File.Delete(_currentUser.customAvatarPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Не удалось удалить предыдущий аватар: {e.Message}");
            }
        }

        _currentUser.SetCustomAvatar(importedPath);
        CustomAvatarManager.LoadSprite(importedPath);
        SaveCurrentUser();
        OnUserProfileChanged?.Invoke(_currentUser);
    }
    
    public static Sprite GetCurrentAvatarSprite()
    {
        if (_currentUser == null)
            return AvatarLibrary.GetAvatarSprite("default");
        if (_currentUser.avatarId == UserProfile.CustomAvatarId)
        {
            Sprite sprite = CustomAvatarManager.LoadSprite(_currentUser.customAvatarPath);
            if (sprite == null)
                Debug.LogWarning($"[AuthManager] Не удалось загрузить пользовательский аватар по пути: {_currentUser.customAvatarPath}");
            else
                Debug.Log($"[AuthManager] Загрузили пользовательский аватар: {_currentUser.customAvatarPath}");
            return sprite ?? AvatarLibrary.GetAvatarSprite("default");
        }
        return AvatarLibrary.GetAvatarSprite(_currentUser.avatarId);
    }

    public static IReadOnlyList<string> GetFriends()
    {
        if (_currentUser == null)
            return Array.Empty<string>();
        EnsureSocialCollections(_currentUser);
        return new List<string>(_currentUser.friends);
    }

    public static IReadOnlyList<FriendRequestData> GetIncomingFriendRequests()
    {
        if (_currentUser == null)
            return Array.Empty<FriendRequestData>();
        EnsureSocialCollections(_currentUser);
        return CloneRequestList(_currentUser.incomingFriendRequests);
    }

    public static IReadOnlyList<FriendRequestData> GetOutgoingFriendRequests()
    {
        if (_currentUser == null)
            return Array.Empty<FriendRequestData>();
        EnsureSocialCollections(_currentUser);
        return CloneRequestList(_currentUser.outgoingFriendRequests);
    }

    public static bool TrySendFriendRequest(string targetUsername, out string error)
    {
        error = string.Empty;
        if (_currentUser == null)
        {
            error = "Сначала войдите в профиль.";
            return false;
        }

        targetUsername = targetUsername?.Trim();
        if (string.IsNullOrEmpty(targetUsername))
        {
            error = "Введите имя пользователя.";
            return false;
        }

        if (string.Equals(targetUsername, _currentUser.username, StringComparison.OrdinalIgnoreCase))
        {
            error = "Нельзя добавить себя.";
            return false;
        }

        string resolvedUsername = ResolveUsernameCaseInsensitive(targetUsername);
        
        if (resolvedUsername == null)
        {
            error = "Пользователь не найден.";
            return false;
        }

        EnsureSocialCollections(_currentUser);
        if (_currentUser.friends.Any(f => string.Equals(f, resolvedUsername, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Пользователь уже в списке друзей.";
            return false;
        }
        if (_currentUser.outgoingFriendRequests.Any(r => string.Equals(r.to, resolvedUsername, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Заявка уже отправлена.";
            return false;
        }

        // Пытаемся загрузить профиль пользователя
        UserProfile targetProfile = UserDataManager.LoadUserProfile(resolvedUsername);
        
        if (targetProfile == null)
        {
            error = "Пользователь не найден.";
            return false;
        }

        EnsureSocialCollections(targetProfile);

        if (targetProfile.friends.Any(f => string.Equals(f, _currentUser.username, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Вы уже друзья.";
            return false;
        }
        if (targetProfile.incomingFriendRequests.Any(r => string.Equals(r.from, _currentUser.username, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Заявка уже ожидает подтверждения.";
            return false;
        }

        _currentUser.outgoingFriendRequests.Add(CreateRequest(_currentUser.username, resolvedUsername));
        SaveCurrentUser();

        targetProfile.incomingFriendRequests.Add(CreateRequest(_currentUser.username, resolvedUsername));
        UserDataManager.SaveUserProfile(targetProfile);

        // Обновляем Custom Properties в Photon если подключены
        if (PhotonSocialManager.Instance != null && Photon.Pun.PhotonNetwork.IsConnected)
        {
            PhotonSocialManager.Instance.UpdatePlayerCustomProperties();
        }

        NotifySocialChanged();
        return true;
    }

    public static bool TryCancelFriendRequest(string targetUsername, out string error)
    {
        error = string.Empty;
        if (_currentUser == null)
        {
            error = "Сначала войдите в профиль.";
            return false;
        }

        targetUsername = targetUsername?.Trim();
        if (string.IsNullOrEmpty(targetUsername))
        {
            error = "Введите имя пользователя.";
            return false;
        }

        string resolvedUsername = ResolveUsernameCaseInsensitive(targetUsername);
        if (resolvedUsername == null)
        {
            error = "Пользователь не найден.";
            return false;
        }

        EnsureSocialCollections(_currentUser);
        bool removed = _currentUser.outgoingFriendRequests.RemoveAll(r => string.Equals(r.to, resolvedUsername, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed)
        {
            error = "Заявка не найдена.";
            return false;
        }

        SaveCurrentUser();

        UserProfile targetProfile = UserDataManager.LoadUserProfile(resolvedUsername);
        if (targetProfile != null)
        {
            EnsureSocialCollections(targetProfile);
            bool removedIncoming = targetProfile.incomingFriendRequests.RemoveAll(r => string.Equals(r.from, _currentUser.username, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removedIncoming)
                UserDataManager.SaveUserProfile(targetProfile);
        }

        NotifySocialChanged();
        return true;
    }

    public static bool TryAcceptFriendRequest(string requesterUsername, out string error)
    {
        error = string.Empty;
        if (_currentUser == null)
        {
            error = "Сначала войдите в профиль.";
            return false;
        }

        requesterUsername = requesterUsername?.Trim();
        if (string.IsNullOrEmpty(requesterUsername))
        {
            error = "Введите имя пользователя.";
            return false;
        }

        string resolvedUsername = ResolveUsernameCaseInsensitive(requesterUsername);
        if (resolvedUsername == null)
        {
            error = "Пользователь не найден.";
            return false;
        }

        EnsureSocialCollections(_currentUser);
        bool removed = _currentUser.incomingFriendRequests.RemoveAll(r => string.Equals(r.from, resolvedUsername, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed)
        {
            error = "Заявка не найдена.";
            return false;
        }

        if (!_currentUser.friends.Contains(resolvedUsername, StringComparer.OrdinalIgnoreCase))
            _currentUser.friends.Add(resolvedUsername);
        SaveCurrentUser();

        UserProfile requesterProfile = UserDataManager.LoadUserProfile(resolvedUsername);
        if (requesterProfile != null)
        {
            EnsureSocialCollections(requesterProfile);
            requesterProfile.outgoingFriendRequests.RemoveAll(r => string.Equals(r.to, _currentUser.username, StringComparison.OrdinalIgnoreCase));
            if (!requesterProfile.friends.Contains(_currentUser.username, StringComparer.OrdinalIgnoreCase))
                requesterProfile.friends.Add(_currentUser.username);
            UserDataManager.SaveUserProfile(requesterProfile);
        }

        // Обновляем Custom Properties в Photon если подключены
        if (PhotonSocialManager.Instance != null && Photon.Pun.PhotonNetwork.IsConnected)
        {
            PhotonSocialManager.Instance.UpdatePlayerCustomProperties();
        }

        NotifySocialChanged();
        return true;
    }

    public static bool TryDeclineFriendRequest(string requesterUsername, out string error)
    {
        error = string.Empty;
        if (_currentUser == null)
        {
            error = "Сначала войдите в профиль.";
            return false;
        }

        requesterUsername = requesterUsername?.Trim();
        if (string.IsNullOrEmpty(requesterUsername))
        {
            error = "Введите имя пользователя.";
            return false;
        }

        string resolvedUsername = ResolveUsernameCaseInsensitive(requesterUsername);
        if (resolvedUsername == null)
        {
            error = "Пользователь не найден.";
            return false;
        }

        EnsureSocialCollections(_currentUser);
        bool removed = _currentUser.incomingFriendRequests.RemoveAll(r => string.Equals(r.from, resolvedUsername, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed)
        {
            error = "Заявка не найдена.";
            return false;
        }

        SaveCurrentUser();

        UserProfile requesterProfile = UserDataManager.LoadUserProfile(resolvedUsername);
        if (requesterProfile != null)
        {
            EnsureSocialCollections(requesterProfile);
            bool removedOutgoing = requesterProfile.outgoingFriendRequests.RemoveAll(r => string.Equals(r.to, _currentUser.username, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removedOutgoing)
                UserDataManager.SaveUserProfile(requesterProfile);
        }

        NotifySocialChanged();
        return true;
    }

    public static bool TryRemoveFriend(string friendUsername, out string error)
    {
        error = string.Empty;
        if (_currentUser == null)
        {
            error = "Сначала войдите в профиль.";
            return false;
        }

        friendUsername = friendUsername?.Trim();
        if (string.IsNullOrEmpty(friendUsername))
        {
            error = "Введите имя пользователя.";
            return false;
        }

        string resolvedUsername = ResolveUsernameCaseInsensitive(friendUsername);
        if (resolvedUsername == null)
        {
            error = "Пользователь не найден.";
            return false;
        }

        EnsureFriendsList(_currentUser);
        bool removed = _currentUser.friends.RemoveAll(f => string.Equals(f, resolvedUsername, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed)
        {
            error = "Пользователь не найден в списке друзей.";
            return false;
        }

        SaveCurrentUser();

        UserProfile friendProfile = UserDataManager.LoadUserProfile(resolvedUsername);
        if (friendProfile != null)
        {
            EnsureSocialCollections(friendProfile);
            bool friendRemoved = friendProfile.friends.RemoveAll(f => string.Equals(f, _currentUser.username, StringComparison.OrdinalIgnoreCase)) > 0;
            if (friendRemoved)
                UserDataManager.SaveUserProfile(friendProfile);
        }

        NotifySocialChanged();
        return true;
    }

    private static string ResolveUsernameCaseInsensitive(string target)
    {
        // Проверяем локальные данные
        var usernames = UserDataManager.GetAllUsernames();
        return usernames.FirstOrDefault(u => string.Equals(u, target, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureFriendsList(UserProfile profile)
    {
        EnsureSocialCollections(profile);
    }

    private static void EnsureSocialCollections(UserProfile profile)
    {
        if (profile == null)
            return;

        if (profile.friends == null)
            profile.friends = new List<string>();
        if (profile.incomingFriendRequests == null)
            profile.incomingFriendRequests = new List<FriendRequestData>();
        if (profile.outgoingFriendRequests == null)
            profile.outgoingFriendRequests = new List<FriendRequestData>();
    }

    private static List<FriendRequestData> CloneRequestList(List<FriendRequestData> source)
    {
        if (source == null)
            return new List<FriendRequestData>();

        var list = new List<FriendRequestData>(source.Count);
        foreach (var request in source)
        {
            if (request == null) continue;
            list.Add(new FriendRequestData
            {
                from = request.from,
                to = request.to,
                createdAtTicks = request.createdAtTicks
            });
        }
        return list;
    }

    private static FriendRequestData CreateRequest(string from, string to)
    {
        return new FriendRequestData
        {
            from = from,
            to = to,
            createdAtTicks = DateTime.UtcNow.Ticks
        };
    }

    public static void NotifySocialChanged()
    {
        OnFriendsChanged?.Invoke(GetFriends().ToList());
        OnFriendRequestsChanged?.Invoke();
    }

    /// <summary>
    /// Вход в UGS с логином и паролем
    /// </summary>
    private static async System.Threading.Tasks.Task LoginToUGSAsync(string username, string password)
    {
        try
        {
            if (UGSServiceManager.Instance == null)
            {
                OnAuthError?.Invoke("Сервис авторизации недоступен. Попробуйте позже.");
                return;
            }
            
            // Инициализируем UGS, если нужно
            if (!UGSServiceManager.Instance.IsInitialized)
            {
                Debug.Log("Инициализация UGS...");
                bool initialized = await UGSServiceManager.Instance.InitializeAsync();
                if (!initialized)
                {
                    OnAuthError?.Invoke("Ошибка инициализации сервиса. Проверьте интернет-соединение.");
                    return;
                }
            }
            
            // Если уже залогинен, сначала выходим
            if (UGSServiceManager.Instance.IsSignedIn)
            {
                Debug.Log("Выход из текущей сессии...");
                UGSServiceManager.Instance.SignOut();
                await System.Threading.Tasks.Task.Delay(300);
            }
            
            // Входим с логином и паролем
            Debug.Log($"Вход в UGS для {username}...");
            bool signedIn = await UGSServiceManager.Instance.SignInWithUsernamePasswordAsync(username, password);
            
            if (!signedIn || !UGSServiceManager.Instance.IsSignedIn)
            {
                OnAuthError?.Invoke("Неверный логин или пароль");
                return;
            }
            
            Debug.Log("Успешный вход в UGS! Загружаем профиль из облака...");
            
            // Загружаем профиль из облака
            if (UGSCloudSaveManager.Instance != null)
            {
                Debug.Log($"AuthManager: Начинаем загрузку профиля для {username} из Cloud Save...");
                var cloudProfile = await UGSCloudSaveManager.Instance.LoadPlayerProfileAsync();
                
                if (cloudProfile != null && !string.IsNullOrWhiteSpace(cloudProfile.username))
                {
                    // Профиль найден в облаке - используем его
                    Debug.Log($"AuthManager: Профиль найден в облаке для {username}. Чипсы: {cloudProfile.chips}, XP: {cloudProfile.XP}");
                    _currentUser = cloudProfile;
                    _currentUser.isLoggedIn = true;
                    _currentUser.lastLoginDate = DateTime.Now;
                    _currentUser.StartNewSession();
                    EnsureSocialCollections(_currentUser);
                    EnsureCardThemeApplied(_currentUser.gameSettings);
                    
                    // Сохраняем в облако (обновляем lastLoginDate)
                    bool saved = await UGSCloudSaveManager.Instance.SavePlayerProfileAsync(_currentUser);
                    if (saved)
                    {
                        Debug.Log($"AuthManager: Профиль обновлен в облаке для {username}");
                    }
                    else
                    {
                        Debug.LogWarning($"AuthManager: Не удалось обновить профиль в облаке для {username}");
                    }
                    
                    OnUserLoggedIn?.Invoke(_currentUser);
                    OnUserProfileChanged?.Invoke(_currentUser);
                    NotifySocialChanged();
                    
                    Debug.Log($"AuthManager: Профиль загружен из облака для {username}");
                }
                else
                {
                    // Профиль не найден - создаем новый
                    Debug.Log($"AuthManager: Профиль не найден в облаке для {username}, создаем новый...");
                    _currentUser = new UserProfile
                    {
                        username = username,
                        email = "", // Email будет из UGS, если доступен
                        registrationDate = DateTime.Now,
                        lastLoginDate = DateTime.Now,
                        isLoggedIn = true
                    };
                    
                    _currentUser.StartNewSession();
                    EnsureSocialCollections(_currentUser);
                    EnsureCardThemeApplied(_currentUser.gameSettings);
                    
                    // Сохраняем новый профиль в облако
                    bool saved = await UGSCloudSaveManager.Instance.SavePlayerProfileAsync(_currentUser);
                    if (saved)
                    {
                        Debug.Log($"AuthManager: Создан новый профиль в облаке для {username}");
                    }
                    else
                    {
                        Debug.LogError($"AuthManager: Не удалось сохранить новый профиль в облаке для {username}");
                    }
                    
                    OnUserLoggedIn?.Invoke(_currentUser);
                    OnUserProfileChanged?.Invoke(_currentUser);
                    NotifySocialChanged();
                }
            }
            else
            {
                Debug.LogError("AuthManager: UGSCloudSaveManager.Instance == null!");
                OnAuthError?.Invoke("Сервис сохранения недоступен. Попробуйте позже.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка входа в UGS: {e.Message}\n{e.StackTrace}");
            OnAuthError?.Invoke($"Ошибка входа: {e.Message}");
        }
    }
    
    /// <summary>
    /// Регистрация в UGS с логином и паролем
    /// </summary>
    private static async System.Threading.Tasks.Task RegisterToUGSAsync(string username, string email, string password)
    {
        try
        {
            if (UGSServiceManager.Instance == null)
            {
                OnAuthError?.Invoke("Сервис авторизации недоступен. Попробуйте позже.");
                return;
            }
            
            // Инициализируем UGS, если нужно
            if (!UGSServiceManager.Instance.IsInitialized)
            {
                Debug.Log("Инициализация UGS...");
                bool initialized = await UGSServiceManager.Instance.InitializeAsync();
                if (!initialized)
                {
                    OnAuthError?.Invoke("Ошибка инициализации сервиса. Проверьте интернет-соединение.");
                    return;
                }
            }
            
            // Если уже залогинен, сначала выходим
            if (UGSServiceManager.Instance.IsSignedIn)
            {
                Debug.Log("Выход из текущей сессии...");
                UGSServiceManager.Instance.SignOut();
                await System.Threading.Tasks.Task.Delay(500); // Увеличили задержку
            }
            
            // Регистрируемся в UGS
            Debug.Log($"Регистрация в UGS для {username}...");
            
            // Подписываемся на событие ошибки для получения детального сообщения
            string lastError = null;
            System.Action<string> errorHandler = (errorMsg) => { lastError = errorMsg; };
            UGSServiceManager.OnSignInFailed += errorHandler;
            
            bool registered = await UGSServiceManager.Instance.RegisterWithUsernamePasswordAsync(username, password);
            
            // Отписываемся от события
            UGSServiceManager.OnSignInFailed -= errorHandler;
            
            if (!registered)
            {
                // Показываем ошибку пользователю
                if (!string.IsNullOrEmpty(lastError))
                {
                    OnAuthError?.Invoke(lastError);
                }
                else
                {
                    OnAuthError?.Invoke("Ошибка регистрации. Возможно, пользователь с таким именем уже существует.");
                }
                return;
            }
            
            if (!UGSServiceManager.Instance.IsSignedIn)
            {
                OnAuthError?.Invoke("Ошибка регистрации. Не удалось войти после регистрации.");
                return;
            }
            
            Debug.Log("Успешная регистрация в UGS! Создаем профиль в облаке...");
            
            // Создаем новый профиль
            _currentUser = new UserProfile
            {
                username = username,
                email = email,
                registrationDate = DateTime.Now,
                lastLoginDate = DateTime.Now,
                isLoggedIn = true
            };
            
            _currentUser.StartNewSession();
            EnsureSocialCollections(_currentUser);
            EnsureCardThemeApplied(_currentUser.gameSettings);
            
            // Сохраняем профиль в облако
            if (UGSCloudSaveManager.Instance != null)
            {
                await UGSCloudSaveManager.Instance.SavePlayerProfileAsync(_currentUser);
                
                // Обновляем рейтинг в таблице лидеров
                if (UGSLeaderboardManager.Instance != null)
                {
                    await UGSLeaderboardManager.Instance.UpdatePlayerRatingFromProfile(_currentUser);
                }
            }
            
            OnUserLoggedIn?.Invoke(_currentUser);
            OnUserProfileChanged?.Invoke(_currentUser);
            NotifySocialChanged();
            
            Debug.Log($"Профиль создан в облаке для {username}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка регистрации в UGS: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            
            // Более понятное сообщение об ошибке
            string errorMessage = e.Message;
            if (e.Message.Contains("already exists") || e.Message.Contains("already registered"))
            {
                errorMessage = "Пользователь с таким именем уже существует в системе";
            }
            else if (e.Message.Contains("Invalid state"))
            {
                errorMessage = "Ошибка состояния. Попробуйте позже или перезапустите игру.";
            }
            
            OnAuthError?.Invoke($"Ошибка регистрации: {errorMessage}");
        }
    }
    
    private static void EnsureCardThemeApplied(GameSettings settings)
    {
        string themeId = settings?.cardThemeId;
        CardThemeService.ApplyTheme(string.IsNullOrWhiteSpace(themeId) ? CardThemeService.GetSavedThemeId() : themeId);
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

    public struct MatchResultSummary
    {
        public bool isWinner;
        public int finalStack;
        public int stackDelta;
        public int xpEarned;
        public int handsPlayed;
    }

    public static void ApplyMatchResult(MatchResultSummary summary)
    {
        if (_currentUser == null)
            return;

        _currentUser.totalGamesPlayed++;
        if (summary.isWinner)
            _currentUser.gamesWon++;
        else
            _currentUser.gamesLost++;

        if (summary.stackDelta > 0)
        {
            _currentUser.totalWinnings += summary.stackDelta;
            _currentUser.biggestWin = Mathf.Max(_currentUser.biggestWin, summary.stackDelta);
        }
        else if (summary.stackDelta < 0)
        {
            int loss = Mathf.Abs(summary.stackDelta);
            _currentUser.totalLosses += loss;
            _currentUser.biggestLoss = Mathf.Max(_currentUser.biggestLoss, loss);
        }

        _currentUser.chips = Mathf.Max(0, summary.finalStack);
        _currentUser.currentSessionChips += summary.stackDelta;
        _currentUser.currentSessionGames++;

        int xpGain = Mathf.Max(0, summary.xpEarned);
        _currentUser.XP = Mathf.Max(0, _currentUser.XP + xpGain);

        int handsDelta = Mathf.Max(0, summary.handsPlayed);
        _currentUser.handsPlayed += handsDelta;
        if (summary.isWinner)
            _currentUser.handsWon += handsDelta;
        else
            _currentUser.handsLost += handsDelta;

        _currentUser.lastLoginDate = DateTime.Now;

        if (_currentUser.totalGamesPlayed > 0)
            _currentUser.winRate = (float)_currentUser.gamesWon / _currentUser.totalGamesPlayed * 100f;

        SaveCurrentUser();
        OnUserProfileChanged?.Invoke(_currentUser);
    }

    public static List<UserProfile> GetAllProfilesSnapshot()
    {
        return UserDataManager.LoadAllProfiles();
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
    /// Удаляет всех пользователей кроме указанных
    /// </summary>
    public static int DeleteAllUsersExcept(List<string> usernamesToKeep)
    {
        // Если текущий пользователь будет удален, выходим из системы
        if (_currentUser != null && usernamesToKeep != null)
        {
            bool currentUserWillBeKept = usernamesToKeep.Any(u => 
                string.Equals(u, _currentUser.username, StringComparison.OrdinalIgnoreCase));
            
            if (!currentUserWillBeKept)
            {
                _currentUser = null;
                OnUserLoggedOut?.Invoke();
            }
        }

        return UserDataManager.DeleteAllUsersExcept(usernamesToKeep);
    }
    
    /// <summary>
    /// Получает информацию о системе данных
    /// </summary>
    public static string GetDataInfo()
    {
        return UserDataManager.GetDataInfo();
    }
}
