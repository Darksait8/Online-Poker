using UnityEngine;
using Unity.Services.Friends;
using Unity.Services.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System;

/// <summary>
/// Менеджер для работы с друзьями через Unity Gaming Services Friends API
/// </summary>
public class UGSFriendsManager : MonoBehaviour
{
    public static UGSFriendsManager Instance { get; private set; }
    
    // Используем обертки для данных из UGS Friends API
    private List<UGSFriendInfo> _friendsList = new List<UGSFriendInfo>();
    private List<UGSFriendRequestInfo> _incomingRequests = new List<UGSFriendRequestInfo>();
    private List<UGSFriendRequestInfo> _outgoingRequests = new List<UGSFriendRequestInfo>();
    
    // Публичные свойства
    public List<UGSFriendInfo> Friends => _friendsList;
    public List<UGSFriendRequestInfo> IncomingRequests => _incomingRequests;
    public List<UGSFriendRequestInfo> OutgoingRequests => _outgoingRequests;
    
    public static event System.Action<List<UGSFriendInfo>> OnFriendsUpdated;
    public static event System.Action<List<UGSFriendRequestInfo>> OnIncomingRequestsUpdated;
    public static event System.Action<List<UGSFriendRequestInfo>> OnOutgoingRequestsUpdated;
    public static event System.Action<string> OnFriendRemoved;
    public static event System.Action<string> OnFriendRequestSent;
    public static event System.Action<string> OnFriendRequestAccepted;
    public static event System.Action<string> OnFriendRequestDeclined;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Обновить список друзей и заявок
    /// </summary>
    public async Task RefreshFriendsAsync()
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return;
        }
        
        try
        {
            // Получение списка друзей
            // Используем рефлексию для работы с разными версиями API
            _friendsList.Clear();
            
            try
            {
                var friendsServiceType = FriendsService.Instance.GetType();
                var getFriendsMethod = friendsServiceType.GetMethod("GetFriendsAsync") 
                    ?? friendsServiceType.GetMethod("GetFriends");
                
                if (getFriendsMethod != null)
                {
                    object friendsResponse;
                    if (typeof(Task).IsAssignableFrom(getFriendsMethod.ReturnType))
                    {
                        var task = (Task)getFriendsMethod.Invoke(FriendsService.Instance, null);
                        await task;
                        friendsResponse = task.GetType().GetProperty("Result")?.GetValue(task);
                    }
                    else
                    {
                        friendsResponse = getFriendsMethod.Invoke(FriendsService.Instance, null);
                    }
                    
                    if (friendsResponse != null)
                    {
                        var enumerable = friendsResponse as System.Collections.IEnumerable;
                        if (enumerable != null)
                        {
                            foreach (object friend in enumerable)
                            {
                                var friendInfo = new UGSFriendInfo();
                                
                                try
                                {
                                    // Используем рефлексию для получения свойств
                                    var friendType = friend.GetType();
                                    var idProp = friendType.GetProperty("Id") ?? friendType.GetProperty("PlayerId");
                                    var nameProp = friendType.GetProperty("DisplayName") ?? friendType.GetProperty("Name");
                                    
                                    if (idProp != null)
                                        friendInfo.Id = idProp.GetValue(friend)?.ToString() ?? "";
                                    
                                    if (nameProp != null)
                                        friendInfo.DisplayName = nameProp.GetValue(friend)?.ToString() ?? friendInfo.Id;
                                    else
                                        friendInfo.DisplayName = friendInfo.Id;
                                }
                                catch
                                {
                                    friendInfo.Id = friend.ToString();
                                    friendInfo.DisplayName = friendInfo.Id;
                                }
                                
                                _friendsList.Add(friendInfo);
                            }
                        }
                    }
                }
                
                OnFriendsUpdated?.Invoke(_friendsList);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FriendsService.GetFriends не доступен: {e.Message}. Возможно, пакет Friends не установлен.");
            }
            
            // Получение входящих заявок
            try
            {
                _incomingRequests.Clear();
                
                var friendsServiceType = FriendsService.Instance.GetType();
                var getIncomingMethod = friendsServiceType.GetMethod("GetIncomingFriendRequestsAsync")
                    ?? friendsServiceType.GetMethod("GetIncomingFriendRequests");
                
                if (getIncomingMethod != null)
                {
                    object incomingResponse;
                    if (typeof(Task).IsAssignableFrom(getIncomingMethod.ReturnType))
                    {
                        var task = (Task)getIncomingMethod.Invoke(FriendsService.Instance, null);
                        await task;
                        incomingResponse = task.GetType().GetProperty("Result")?.GetValue(task);
                    }
                    else
                    {
                        incomingResponse = getIncomingMethod.Invoke(FriendsService.Instance, null);
                    }
                    
                    if (incomingResponse != null)
                    {
                        var enumerable = incomingResponse as System.Collections.IEnumerable;
                        if (enumerable != null)
                        {
                            foreach (object request in enumerable)
                            {
                                var requestInfo = new UGSFriendRequestInfo();
                                
                                try
                                {
                                    // Используем рефлексию для получения свойств
                                    var requestType = request.GetType();
                                    var idProp = requestType.GetProperty("Id") ?? requestType.GetProperty("RequestId");
                                    var fromProp = requestType.GetProperty("FromPlayerId") ?? requestType.GetProperty("FromId");
                                    var toProp = requestType.GetProperty("ToPlayerId") ?? requestType.GetProperty("ToId");
                                    
                                    if (idProp != null)
                                        requestInfo.Id = idProp.GetValue(request)?.ToString() ?? "";
                                    
                                    if (fromProp != null)
                                        requestInfo.FromPlayerId = fromProp.GetValue(request)?.ToString() ?? "";
                                    
                                    if (toProp != null)
                                        requestInfo.ToPlayerId = toProp.GetValue(request)?.ToString() ?? "";
                                }
                                catch
                                {
                                    requestInfo.Id = request.ToString();
                                }
                                
                                _incomingRequests.Add(requestInfo);
                            }
                        }
                    }
                }
                
                OnIncomingRequestsUpdated?.Invoke(_incomingRequests);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FriendsService.GetIncomingFriendRequests не доступен: {e.Message}");
            }
            
            // Получение исходящих заявок
            try
            {
                _outgoingRequests.Clear();
                
                var friendsServiceType = FriendsService.Instance.GetType();
                var getOutgoingMethod = friendsServiceType.GetMethod("GetOutgoingFriendRequestsAsync")
                    ?? friendsServiceType.GetMethod("GetOutgoingFriendRequests");
                
                if (getOutgoingMethod != null)
                {
                    object outgoingResponse;
                    if (typeof(Task).IsAssignableFrom(getOutgoingMethod.ReturnType))
                    {
                        var task = (Task)getOutgoingMethod.Invoke(FriendsService.Instance, null);
                        await task;
                        outgoingResponse = task.GetType().GetProperty("Result")?.GetValue(task);
                    }
                    else
                    {
                        outgoingResponse = getOutgoingMethod.Invoke(FriendsService.Instance, null);
                    }
                    
                    if (outgoingResponse != null)
                    {
                        var enumerable = outgoingResponse as System.Collections.IEnumerable;
                        if (enumerable != null)
                        {
                            foreach (object request in enumerable)
                            {
                                var requestInfo = new UGSFriendRequestInfo();
                                
                                try
                                {
                                    // Используем рефлексию для получения свойств
                                    var requestType = request.GetType();
                                    var idProp = requestType.GetProperty("Id") ?? requestType.GetProperty("RequestId");
                                    var fromProp = requestType.GetProperty("FromPlayerId") ?? requestType.GetProperty("FromId");
                                    var toProp = requestType.GetProperty("ToPlayerId") ?? requestType.GetProperty("ToId");
                                    
                                    if (idProp != null)
                                        requestInfo.Id = idProp.GetValue(request)?.ToString() ?? "";
                                    
                                    if (fromProp != null)
                                        requestInfo.FromPlayerId = fromProp.GetValue(request)?.ToString() ?? "";
                                    
                                    if (toProp != null)
                                        requestInfo.ToPlayerId = toProp.GetValue(request)?.ToString() ?? "";
                                }
                                catch
                                {
                                    requestInfo.Id = request.ToString();
                                }
                                
                                _outgoingRequests.Add(requestInfo);
                            }
                        }
                    }
                }
                
                OnOutgoingRequestsUpdated?.Invoke(_outgoingRequests);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FriendsService.GetOutgoingFriendRequests не доступен: {e.Message}");
            }
            
            Debug.Log($"Друзья обновлены: {_friendsList.Count} друзей, {_incomingRequests.Count} входящих, {_outgoingRequests.Count} исходящих заявок");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка обновления друзей: {e.Message}");
        }
    }
    
    /// <summary>
    /// Отправить заявку в друзья по Player ID
    /// </summary>
    public async Task<bool> SendFriendRequestAsync(string playerId)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return false;
        }
        
        try
        {
            // Используем рефлексию для вызова метода
            var friendsServiceType = FriendsService.Instance.GetType();
            var sendMethod = friendsServiceType.GetMethod("SendFriendRequestAsync") 
                ?? friendsServiceType.GetMethod("SendFriendRequest");
            
            if (sendMethod != null)
            {
                var parameters = new object[] { playerId };
                var result = sendMethod.Invoke(FriendsService.Instance, parameters);
                
                if (result is Task task)
                {
                    await task;
                }
                
                Debug.Log($"Заявка в друзья отправлена: {playerId}");
                OnFriendRequestSent?.Invoke(playerId);
                
                // Обновляем список заявок
                await RefreshFriendsAsync();
                
                return true;
            }
            else
            {
                Debug.LogWarning("Метод SendFriendRequest не найден в FriendsService");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка отправки заявки: {e.Message}. Возможно, пакет Friends не установлен или API изменился.");
            return false;
        }
    }
    
    /// <summary>
    /// Отправить заявку в друзья по имени пользователя (если доступно)
    /// </summary>
    public Task<bool> SendFriendRequestByUsernameAsync(string username)
    {
        // UGS Friends работает по Player ID, не по username
        // Нужно сначала найти Player ID по username через другие сервисы
        Debug.LogWarning("UGS Friends работает по Player ID. Используйте SendFriendRequestAsync(playerId)");
        return Task.FromResult(false);
    }
    
    /// <summary>
    /// Принять заявку в друзья
    /// </summary>
    public async Task<bool> AcceptFriendRequestAsync(string requestId)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return false;
        }
        
        try
        {
            var friendsServiceType = FriendsService.Instance.GetType();
            var acceptMethod = friendsServiceType.GetMethod("AcceptFriendRequestAsync")
                ?? friendsServiceType.GetMethod("AcceptFriendRequest");
            
            if (acceptMethod != null)
            {
                var parameters = new object[] { requestId };
                var result = acceptMethod.Invoke(FriendsService.Instance, parameters);
                
                if (result is Task task)
                {
                    await task;
                }
                
                Debug.Log($"Заявка принята: {requestId}");
                OnFriendRequestAccepted?.Invoke(requestId);
                
                await RefreshFriendsAsync();
                return true;
            }
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка принятия заявки: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Отклонить заявку в друзья
    /// </summary>
    public async Task<bool> DeclineFriendRequestAsync(string requestId)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return false;
        }
        
        try
        {
            var friendsServiceType = FriendsService.Instance.GetType();
            var declineMethod = friendsServiceType.GetMethod("DeclineFriendRequestAsync")
                ?? friendsServiceType.GetMethod("DeclineFriendRequest");
            
            if (declineMethod != null)
            {
                var parameters = new object[] { requestId };
                var result = declineMethod.Invoke(FriendsService.Instance, parameters);
                
                if (result is Task task)
                {
                    await task;
                }
                
                Debug.Log($"Заявка отклонена: {requestId}");
                OnFriendRequestDeclined?.Invoke(requestId);
                
                await RefreshFriendsAsync();
                return true;
            }
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка отклонения заявки: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Удалить друга
    /// </summary>
    public async Task<bool> RemoveFriendAsync(string friendId)
    {
        if (!UGSServiceManager.Instance.IsSignedIn)
        {
            Debug.LogWarning("Игрок не авторизован!");
            return false;
        }
        
        try
        {
            var friendsServiceType = FriendsService.Instance.GetType();
            var deleteMethod = friendsServiceType.GetMethod("DeleteFriendAsync")
                ?? friendsServiceType.GetMethod("DeleteFriend");
            
            if (deleteMethod != null)
            {
                var parameters = new object[] { friendId };
                var result = deleteMethod.Invoke(FriendsService.Instance, parameters);
                
                if (result is Task task)
                {
                    await task;
                }
                
                Debug.Log($"Друг удален: {friendId}");
                OnFriendRemoved?.Invoke(friendId);
                
                await RefreshFriendsAsync();
                return true;
            }
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка удаления друга: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Получить список имен друзей (для совместимости с существующим кодом)
    /// </summary>
    public List<string> GetFriendsUsernames()
    {
        return Friends.Select(f => f.DisplayName ?? f.Id).ToList();
    }
    
    /// <summary>
    /// Проверить, является ли игрок другом
    /// </summary>
    public bool IsFriend(string playerId)
    {
        return Friends.Any(f => f.Id == playerId);
    }
    
    /// <summary>
    /// Получить информацию о друге по ID
    /// </summary>
    public UGSFriendInfo GetFriend(string friendId)
    {
        return Friends.FirstOrDefault(f => f.Id == friendId);
    }
}

/// <summary>
/// Информация о друге из UGS
/// </summary>
[System.Serializable]
public class UGSFriendInfo
{
    public string Id;
    public string DisplayName;
}

/// <summary>
/// Информация о заявке в друзья из UGS
/// </summary>
[System.Serializable]
public class UGSFriendRequestInfo
{
    public string Id;
    public string FromPlayerId;
    public string ToPlayerId;
}

