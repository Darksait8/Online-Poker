using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// Панель для приглашения друзей к столу во время игры
/// </summary>
public class InviteFriendsPanel : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform friendsListContainer;
    [SerializeField] private GameObject friendItemPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text statusText;
    [SerializeField] private TMP_Text statusTextTMP;

    [Header("Информация о текущем столе")]
    [SerializeField] private Text currentTableInfo;
    [SerializeField] private TMP_Text currentTableInfoTMP;

    private TableInfo currentTable;

    private PauseMenuController pauseController;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (panel != null)
            panel.SetActive(false);
    }

    private void Start()
    {
        // Находим PauseMenuController для уведомления о закрытии
        pauseController = FindObjectOfType<PauseMenuController>();
    }

    private void OnCloseClicked()
    {
        ClosePanel();
        // Уведомляем PauseMenuController, чтобы вернуть главное меню паузы
        if (pauseController != null)
        {
            pauseController.HideInviteFriends();
        }
    }

    public void ShowPanel()
    {
        // Получаем информацию о текущем столе
        if (TableRuntimeConfig.HasConfig)
        {
            string creatorId = AuthManager.IsLoggedIn ? AuthManager.CurrentUser?.username : null;
            string tableName = $"Текущий стол ({TableRuntimeConfig.SmallBlind}/{TableRuntimeConfig.BigBlind} BB)";
            currentTable = new TableInfo(tableName, TableRuntimeConfig.SmallBlind, TableRuntimeConfig.MaxSeats, false, creatorId);
            currentTable.tableId = TableRuntimeConfig.TableId;
        }
        else
        {
            // Если нет конфигурации, создаем стандартный стол
            string creatorId = AuthManager.IsLoggedIn ? AuthManager.CurrentUser?.username : null;
            currentTable = new TableInfo("Текущий стол", 10, 6, false, creatorId);
            // Генерируем tableId для локального стола
            if (!string.IsNullOrEmpty(creatorId))
            {
                currentTable.tableId = $"table_{currentTable.tableName.Replace(" ", "_")}_{creatorId}";
            }
            else
            {
                currentTable.tableId = $"table_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            }
        }

        UpdateCurrentTableInfo();
        RefreshFriendsList();

        if (panel != null)
            panel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void UpdateCurrentTableInfo()
    {
        string info = $"Пригласить друзей к столу:\n{currentTable.GetDisplayName()}";
        
        if (currentTableInfo != null)
            currentTableInfo.text = info;
        
        if (currentTableInfoTMP != null)
            currentTableInfoTMP.text = info;
    }

    private void RefreshFriendsList()
    {
        if (friendsListContainer == null)
            return;

        // Очищаем список
        foreach (Transform child in friendsListContainer)
        {
            Destroy(child.gameObject);
        }

        // Получаем список друзей
        var friends = AuthManager.GetFriends();
        
        if (friends == null || friends.Count == 0)
        {
            SetStatus("У вас нет друзей. Добавьте друзей в главном меню.");
            return;
        }

        // Создаем элементы для каждого друга
        foreach (string friendName in friends)
        {
            CreateFriendItem(friendName);
        }
    }

    private void CreateFriendItem(string friendName)
    {
        GameObject itemObj;

        if (friendItemPrefab != null)
        {
            itemObj = Instantiate(friendItemPrefab, friendsListContainer);
        }
        else
        {
            // Создаем элемент программно
            itemObj = new GameObject($"FriendItem_{friendName}", typeof(RectTransform), typeof(Image));
            itemObj.transform.SetParent(friendsListContainer, false);

            RectTransform rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 60f);

            Image image = itemObj.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            HorizontalLayoutGroup layout = itemObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = false;

            // Имя друга
            GameObject nameObj = new GameObject("Name", typeof(Text));
            nameObj.transform.SetParent(itemObj.transform, false);
            Text nameText = nameObj.GetComponent<Text>();
            nameText.text = friendName;
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = 18;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(200f, 40f);
        }

        // Добавляем кнопку "Пригласить"
        GameObject inviteButtonObj = new GameObject("InviteButton", typeof(Image), typeof(Button));
        inviteButtonObj.transform.SetParent(itemObj.transform, false);
        Button inviteButton = inviteButtonObj.GetComponent<Button>();
        Image inviteImage = inviteButtonObj.GetComponent<Image>();
        inviteImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        RectTransform inviteRect = inviteButtonObj.GetComponent<RectTransform>();
        inviteRect.sizeDelta = new Vector2(150f, 40f);

        GameObject inviteTextObj = new GameObject("Text", typeof(Text));
        inviteTextObj.transform.SetParent(inviteButtonObj.transform, false);
        Text inviteText = inviteTextObj.GetComponent<Text>();
        inviteText.text = "Пригласить";
        inviteText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        inviteText.fontSize = 16;
        inviteText.color = Color.white;
        inviteText.alignment = TextAnchor.MiddleCenter;
        RectTransform inviteTextRect = inviteTextObj.GetComponent<RectTransform>();
        inviteTextRect.anchorMin = Vector2.zero;
        inviteTextRect.anchorMax = Vector2.one;
        inviteTextRect.sizeDelta = Vector2.zero;

        // Обработчик нажатия
        inviteButton.onClick.AddListener(() => OnInviteFriendClicked(friendName));
    }

    private void OnInviteFriendClicked(string friendName)
    {
        if (currentTable == null)
        {
            SetStatus("Ошибка: информация о столе не найдена");
            return;
        }

        // Устанавливаем tableId если его нет
        if (string.IsNullOrEmpty(currentTable.tableId) && TableRuntimeConfig.HasConfig)
        {
            currentTable.tableId = TableRuntimeConfig.TableId;
        }

        // Отправляем инвайт через Photon если подключены, иначе локально
        bool sent = false;
        if (PhotonSocialManager.Instance != null && Photon.Pun.PhotonNetwork.IsConnected)
        {
            sent = PhotonSocialManager.Instance.SendTableInviteViaPhoton(currentTable, friendName);
        }
        
        if (!sent)
        {
            // Fallback на локальную отправку
            TableInviteManager.SendTableInvite(currentTable, friendName, friendName);
        }
        
        SetStatus($"Приглашение отправлено {friendName}!");
        
        Debug.Log($"Инвайт к столу '{currentTable.tableName}' отправлен другу {friendName}");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        
        if (statusTextTMP != null)
            statusTextTMP.text = message;
    }
}

