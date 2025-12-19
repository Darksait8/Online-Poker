using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

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
        
        // Подписываемся на события UGS Friends, если доступны
        if (UGSFriendsManager.Instance != null)
        {
            UGSFriendsManager.OnFriendsUpdated += HandleUGSFriendsUpdated;
        }
        
        RefreshList();
        SetStatus(string.Empty);
    }

    private void OnDisable()
    {
        AuthManager.OnFriendsChanged -= HandleFriendsChanged;
        
        if (UGSFriendsManager.Instance != null)
        {
            UGSFriendsManager.OnFriendsUpdated -= HandleUGSFriendsUpdated;
        }
    }
    
    private async void HandleUGSFriendsUpdated(List<UGSFriendInfo> ugsFriends)
    {
        // Обновляем список друзей из UGS
        await RefreshUGSFriendsAsync();
    }
    
    private async System.Threading.Tasks.Task RefreshUGSFriendsAsync()
    {
        if (UGSFriendsManager.Instance != null && UGSServiceManager.Instance != null && UGSServiceManager.Instance.IsSignedIn)
        {
            try
            {
                await UGSFriendsManager.Instance.RefreshFriendsAsync();
                RefreshList();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Ошибка обновления друзей из UGS: {e.Message}");
            }
        }
    }

    public void RefreshList()
    {
        ClearItems();
        
        // Сначала добавляем локальных друзей
        var friends = AuthManager.GetFriends();
        foreach (string friend in friends)
        {
            CreateFriendItem(friend);
        }
        
        // Затем добавляем друзей из UGS, если доступны
        if (UGSFriendsManager.Instance != null && UGSServiceManager.Instance != null && UGSServiceManager.Instance.IsSignedIn)
        {
            var ugsFriends = UGSFriendsManager.Instance.Friends;
            foreach (var ugsFriend in ugsFriends)
            {
                string friendName = ugsFriend.DisplayName ?? ugsFriend.Id;
                // Проверяем, чтобы не дублировать друзей (используем LINQ Contains)
                if (!friends.Any(f => string.Equals(f, friendName, StringComparison.OrdinalIgnoreCase)))
                {
                    CreateFriendItem(friendName);
                }
            }
        }
    }

    private void HandleFriendsChanged(List<string> friends)
    {
        RefreshList();
    }

    private async void OnAddFriendClicked()
    {
        string candidate = addFriendInput != null ? addFriendInput.text : string.Empty;
        candidate = candidate?.Trim();
        
        if (string.IsNullOrEmpty(candidate))
        {
            SetStatus("Введите имя пользователя или Player ID.");
            return;
        }
        
        bool sent = false;
        
        // Пытаемся отправить через UGS, если доступен и candidate похож на Player ID
        if (UGSFriendsManager.Instance != null && UGSServiceManager.Instance != null && UGSServiceManager.Instance.IsSignedIn)
        {
            // Если candidate похож на Player ID (обычно длинная строка), пробуем UGS
            if (candidate.Length > 20 || candidate.Contains("-"))
            {
                try
                {
                    sent = await UGSFriendsManager.Instance.SendFriendRequestAsync(candidate);
                    if (sent)
                    {
                        SetStatus("Заявка отправлена через UGS");
                        if (addFriendInput != null)
                            addFriendInput.text = string.Empty;
                        await RefreshUGSFriendsAsync();
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Ошибка отправки заявки через UGS: {e.Message}");
                }
            }
        }
        
        // Пытаемся отправить через Photon если подключены
        if (!sent && PhotonSocialManager.Instance != null && Photon.Pun.PhotonNetwork.IsConnected)
        {
            sent = PhotonSocialManager.Instance.SendFriendRequestViaPhoton(candidate);
        }
        
        if (!sent)
        {
            // Fallback на локальную отправку
            if (AuthManager.TrySendFriendRequest(candidate, out string error))
            {
                SetStatus("Заявка отправлена (локально)");
                if (addFriendInput != null)
                    addFriendInput.text = string.Empty;
            }
            else
            {
                SetStatus(error);
            }
        }
        else
        {
            SetStatus("Заявка отправлена");
            if (addFriendInput != null)
                addFriendInput.text = string.Empty;
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

