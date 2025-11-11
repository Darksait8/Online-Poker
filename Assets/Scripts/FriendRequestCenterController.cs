using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendRequestCenterController : MonoBehaviour
{
    [SerializeField] private Transform incomingRoot;
    [SerializeField] private Transform outgoingRoot;
    [SerializeField] private GameObject incomingTemplate;
    [SerializeField] private GameObject outgoingTemplate;
    [SerializeField] private Text statusText;
    [SerializeField] private Button closeButton;

    private readonly List<GameObject> incomingItems = new List<GameObject>();
    private readonly List<GameObject> outgoingItems = new List<GameObject>();

    public event Action OnCloseRequested;

    private void Awake()
    {
        if (incomingTemplate != null)
            incomingTemplate.SetActive(false);
        if (outgoingTemplate != null)
            outgoingTemplate.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => OnCloseRequested?.Invoke());
    }

    private void OnEnable()
    {
        AuthManager.OnFriendRequestsChanged += HandleRequestsChanged;
        Refresh();
        SetStatus(string.Empty);
    }

    private void OnDisable()
    {
        AuthManager.OnFriendRequestsChanged -= HandleRequestsChanged;
    }

    public void Refresh()
    {
        PopulateIncoming();
        PopulateOutgoing();
    }

    private void HandleRequestsChanged()
    {
        Refresh();
    }

    private void PopulateIncoming()
    {
        ClearItems(incomingItems);
        var incoming = AuthManager.GetIncomingFriendRequests();
        if (incomingRoot == null || incomingTemplate == null)
            return;

        foreach (FriendRequestData request in incoming)
        {
            GameObject item = Instantiate(incomingTemplate, incomingRoot);
            item.SetActive(true);
            incomingItems.Add(item);

            Text label = item.transform.Find("Name")?.GetComponent<Text>() ?? item.GetComponentInChildren<Text>();
            if (label != null)
                label.text = request.from;

            Button acceptButton = item.transform.Find("AcceptButton")?.GetComponent<Button>();
            if (acceptButton != null)
                acceptButton.onClick.AddListener(() => OnAcceptClicked(request.from));

            Button declineButton = item.transform.Find("DeclineButton")?.GetComponent<Button>();
            if (declineButton != null)
                declineButton.onClick.AddListener(() => OnDeclineClicked(request.from));
        }
    }

    private void PopulateOutgoing()
    {
        ClearItems(outgoingItems);
        var outgoing = AuthManager.GetOutgoingFriendRequests();
        if (outgoingRoot == null || outgoingTemplate == null)
            return;

        foreach (FriendRequestData request in outgoing)
        {
            GameObject item = Instantiate(outgoingTemplate, outgoingRoot);
            item.SetActive(true);
            outgoingItems.Add(item);

            Text label = item.transform.Find("Name")?.GetComponent<Text>() ?? item.GetComponentInChildren<Text>();
            if (label != null)
                label.text = request.to;

            Text dateLabel = item.transform.Find("Info")?.GetComponent<Text>();
            if (dateLabel != null)
            {
                if (request.createdAtTicks > 0)
                {
                    DateTime created = new DateTime(request.createdAtTicks, DateTimeKind.Utc).ToLocalTime();
                    dateLabel.text = $"отправлено {created:dd.MM.yyyy HH:mm}";
                }
                else
                {
                    dateLabel.text = "отправлено недавно";
                }
            }

            Button cancelButton = item.transform.Find("CancelButton")?.GetComponent<Button>();
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => OnCancelClicked(request.to));
        }
    }

    private void OnAcceptClicked(string fromUser)
    {
        if (AuthManager.TryAcceptFriendRequest(fromUser, out string error))
        {
            SetStatus("Заявка принята");
        }
        else
        {
            SetStatus(error);
        }
    }

    private void OnDeclineClicked(string fromUser)
    {
        if (AuthManager.TryDeclineFriendRequest(fromUser, out string error))
        {
            SetStatus("Заявка отклонена");
        }
        else
        {
            SetStatus(error);
        }
    }

    private void OnCancelClicked(string toUser)
    {
        if (AuthManager.TryCancelFriendRequest(toUser, out string error))
        {
            SetStatus("Заявка отменена");
        }
        else
        {
            SetStatus(error);
        }
    }

    private void ClearItems(List<GameObject> collection)
    {
        foreach (GameObject go in collection)
        {
            if (go != null)
                Destroy(go);
        }
        collection.Clear();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}

