using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendListController : MonoBehaviour
{
    [SerializeField] private Transform itemsRoot;
    [SerializeField] private GameObject itemTemplate;
    [SerializeField] private InputField addFriendInput;
    [SerializeField] private Button addFriendButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Button closeButton;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();

    public event Action OnCloseRequested;

    private void Awake()
    {
        if (itemTemplate != null)
            itemTemplate.SetActive(false);

        if (addFriendButton != null)
            addFriendButton.onClick.AddListener(OnAddFriendClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => OnCloseRequested?.Invoke());
    }

    private void OnEnable()
    {
        AuthManager.OnFriendsChanged += HandleFriendsChanged;
        RefreshList();
        SetStatus(string.Empty);
    }

    private void OnDisable()
    {
        AuthManager.OnFriendsChanged -= HandleFriendsChanged;
    }

    public void RefreshList()
    {
        ClearItems();
        var friends = AuthManager.GetFriends();
        foreach (string friend in friends)
        {
            CreateFriendItem(friend);
        }
    }

    private void HandleFriendsChanged(List<string> friends)
    {
        RefreshList();
    }

    private void OnAddFriendClicked()
    {
        string candidate = addFriendInput != null ? addFriendInput.text : string.Empty;
        candidate = candidate?.Trim();
        
        if (string.IsNullOrEmpty(candidate))
        {
            SetStatus("Введите имя пользователя.");
            return;
        }
        
        // Пытаемся добавить друга через сервер, если включена серверная авторизация
        AuthServerSync authSync = FindObjectOfType<AuthServerSync>();
        if (authSync != null)
        {
            var useServerAuthField = typeof(AuthServerSync).GetField("useServerAuth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool useServerAuth = useServerAuthField != null && 
                                (bool)(useServerAuthField.GetValue(authSync) ?? false);
            
            if (useServerAuth)
            {
                // Проверяем пользователя на сервере асинхронно
                StartCoroutine(CheckUserAndAddFriend(candidate, authSync));
                return;
            }
        }
        
        // Fallback на локальную проверку
        if (AuthManager.TrySendFriendRequest(candidate, out string error))
        {
            SetStatus("Заявка отправлена");
            if (addFriendInput != null)
                addFriendInput.text = string.Empty;
        }
        else
        {
            SetStatus(error);
        }
    }
    
    private System.Collections.IEnumerator CheckUserAndAddFriend(string username, AuthServerSync authSync)
    {
        SetStatus("Проверка пользователя...");
        
        var authClientField = typeof(AuthServerSync).GetField("authClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var authClient = authClientField?.GetValue(authSync) as AuthServerClient;
        
        if (authClient == null)
        {
            SetStatus("Ошибка: AuthServerClient не найден");
            yield break;
        }
        
        // Проверяем подключение
        if (!authClient.IsConnected())
        {
            authClient.Connect();
            float timeout = 5f;
            float elapsed = 0f;
            while (!authClient.IsConnected() && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (!authClient.IsConnected())
            {
                SetStatus("Не удалось подключиться к серверу");
                yield break;
            }
        }
        
        // Запрашиваем профиль пользователя
        bool requestCompleted = false;
        bool userExists = false;
        Dictionary<string, object> userData = null;
        
        System.Action<bool, Dictionary<string, object>> handler = null;
        handler = (success, data) =>
        {
            authClient.OnProfileResponse -= handler;
            userExists = success && data != null;
            userData = data;
            requestCompleted = true;
        };
        
        authClient.OnProfileResponse += handler;
        authClient.GetProfile(username);
        
        // Ждем ответа
        float waitTimeout = 5f;
        float waitElapsed = 0f;
        while (!requestCompleted && waitElapsed < waitTimeout)
        {
            yield return new WaitForSeconds(0.1f);
            waitElapsed += 0.1f;
        }
        
        if (!userExists)
        {
            SetStatus("Пользователь не найден");
            yield break;
        }
        
        // Пользователь найден на сервере, добавляем в друзья
        if (AuthManager.TrySendFriendRequest(username, out string error))
        {
            SetStatus("Заявка отправлена");
            if (addFriendInput != null)
                addFriendInput.text = string.Empty;
        }
        else
        {
            SetStatus(error);
        }
    }

    private void OnRemoveFriendClicked(string friendName)
    {
        if (AuthManager.TryRemoveFriend(friendName, out string error))
        {
            SetStatus("Друг удалён");
        }
        else
        {
            SetStatus(error);
        }
    }

    private void CreateFriendItem(string friendName)
    {
        if (itemsRoot == null || itemTemplate == null)
            return;

        GameObject item = Instantiate(itemTemplate, itemsRoot);
        item.SetActive(true);
        spawnedItems.Add(item);

        Text label = item.transform.Find("Name")?.GetComponent<Text>() ??
                     item.GetComponentInChildren<Text>();
        if (label != null)
            label.text = friendName;

        Button removeButton = item.transform.Find("RemoveButton")?.GetComponent<Button>();
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() => OnRemoveFriendClicked(friendName));
        }
    }

    private void ClearItems()
    {
        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
                Destroy(item);
        }
        spawnedItems.Clear();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}

