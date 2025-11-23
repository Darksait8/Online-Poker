using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
        EnsureSocialCollections(_currentUser);
        
        UserDataManager.SaveUserProfile(_currentUser);
        EnsureCardThemeApplied(_currentUser.gameSettings);
        OnUserLoggedIn?.Invoke(_currentUser);
        OnUserProfileChanged?.Invoke(_currentUser);
        NotifySocialChanged();
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
        EnsureSocialCollections(_currentUser);
        
        // Сохраняем профиль
        if (UserDataManager.SaveUserProfile(_currentUser))
        {
            EnsureCardThemeApplied(_currentUser.gameSettings);
            OnUserLoggedIn?.Invoke(_currentUser);
            OnUserProfileChanged?.Invoke(_currentUser);
            NotifySocialChanged();
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
        EnsureSocialCollections(_currentUser);
        EnsureCardThemeApplied(_currentUser.gameSettings);
        OnUserLoggedIn?.Invoke(_currentUser);
        OnUserProfileChanged?.Invoke(_currentUser);
        NotifySocialChanged();
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
        OnUserProfileChanged?.Invoke(null);
        OnFriendsChanged?.Invoke(new List<string>());
        OnFriendRequestsChanged?.Invoke();
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
                EnsureSocialCollections(_currentUser);
                break;
            }
        }

        if (_currentUser != null)
        {
            NotifySocialChanged();
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
    /// Обновляет баланс игрока (аналогично SetUserChips, но с другим именем для консистентности)
    /// </summary>
    public static void UpdatePlayerBalance(int newBalance)
    {
        if (_currentUser != null)
        {
            _currentUser.chips = Mathf.Max(0, newBalance);
            SaveCurrentUser();
            OnUserProfileChanged?.Invoke(_currentUser);
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
        
        // Если не найден локально, проверяем на сервере
        if (resolvedUsername == null)
        {
            resolvedUsername = CheckUserExistsOnServer(targetUsername);
            if (resolvedUsername == null)
            {
                error = "Пользователь не найден.";
                return false;
            }
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
        
        // Если не найден локально, пытаемся загрузить с сервера
        if (targetProfile == null)
        {
            targetProfile = LoadUserProfileFromServer(resolvedUsername);
            if (targetProfile == null)
            {
                error = "Пользователь не найден.";
                return false;
            }
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

        // Отправляем заявку на сервер, если включена серверная авторизация
        AuthServerSync authSync = UnityEngine.Object.FindObjectOfType<AuthServerSync>();
        if (authSync != null)
        {
            var useServerAuthField = typeof(AuthServerSync).GetField("useServerAuth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool useServerAuth = useServerAuthField != null && 
                                (bool)(useServerAuthField.GetValue(authSync) ?? false);
            
            if (useServerAuth)
            {
                var authClientField = typeof(AuthServerSync).GetField("authClient", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var authClient = authClientField?.GetValue(authSync) as AuthServerClient;
                
                if (authClient != null && authClient.IsConnected())
                {
                    // Отправляем заявку на сервер
                    authClient.SendFriendRequest(_currentUser.username, resolvedUsername);
                    // Заявка будет сохранена на сервере, локально тоже сохраняем для совместимости
                }
            }
        }
        
        _currentUser.outgoingFriendRequests.Add(CreateRequest(_currentUser.username, resolvedUsername));
        SaveCurrentUser();

        targetProfile.incomingFriendRequests.Add(CreateRequest(_currentUser.username, resolvedUsername));
        UserDataManager.SaveUserProfile(targetProfile);

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
    
    /// <summary>
    /// Проверяет существование пользователя на сервере
    /// </summary>
    private static string CheckUserExistsOnServer(string targetUsername)
    {
        AuthServerSync authSync = UnityEngine.Object.FindObjectOfType<AuthServerSync>();
        if (authSync == null)
            return null;
        
        var useServerAuthField = typeof(AuthServerSync).GetField("useServerAuth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bool useServerAuth = useServerAuthField != null && 
                            (bool)(useServerAuthField.GetValue(authSync) ?? false);
        
        if (!useServerAuth)
            return null;
        
        var authClientField = typeof(AuthServerSync).GetField("authClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var authClient = authClientField?.GetValue(authSync) as AuthServerClient;
        
        if (authClient == null || !authClient.IsConnected())
            return null;
        
        // Возвращаем targetUsername для проверки через GetProfile
        return targetUsername;
    }
    
    /// <summary>
    /// Загружает профиль пользователя с сервера
    /// </summary>
    private static UserProfile LoadUserProfileFromServer(string username)
    {
        AuthServerSync authSync = UnityEngine.Object.FindObjectOfType<AuthServerSync>();
        if (authSync == null)
            return null;
        
        var authClientField = typeof(AuthServerSync).GetField("authClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var authClient = authClientField?.GetValue(authSync) as AuthServerClient;
        
        if (authClient == null || !authClient.IsConnected())
            return null;
        
        // Делаем синхронный запрос (в реальности это должно быть асинхронно)
        UserProfile profile = null;
        bool requestCompleted = false;
        
        System.Action<bool, Dictionary<string, object>> handler = null;
        handler = (success, data) =>
        {
            authClient.OnProfileResponse -= handler;
            if (success && data != null)
            {
                profile = new UserProfile
                {
                    username = data.ContainsKey("username") ? data["username"].ToString() : username,
                    email = data.ContainsKey("email") ? data["email"].ToString() : "",
                    passwordHash = "",
                    registrationDate = DateTime.Parse(data.ContainsKey("registration_date") ? data["registration_date"].ToString() : DateTime.Now.ToString()),
                    lastLoginDate = DateTime.Now,
                    isLoggedIn = false,
                    chips = data.ContainsKey("chips") ? Convert.ToInt32(data["chips"]) : 1000,
                    XP = data.ContainsKey("xp") ? Convert.ToInt32(data["xp"]) : 0
                };
                EnsureSocialCollections(profile);
            }
            requestCompleted = true;
        };
        
        authClient.OnProfileResponse += handler;
        authClient.GetProfile(username);
        
        // Ждем ответа (в реальности это должно быть асинхронно через корутину)
        float timeout = 3f;
        float elapsed = 0f;
        while (!requestCompleted && elapsed < timeout)
        {
            System.Threading.Thread.Sleep(100);
            elapsed += 0.1f;
        }
        
        return profile;
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

    private static void NotifySocialChanged()
    {
        OnFriendsChanged?.Invoke(GetFriends().ToList());
        OnFriendRequestsChanged?.Invoke();
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
