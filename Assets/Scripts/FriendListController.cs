using System;
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

