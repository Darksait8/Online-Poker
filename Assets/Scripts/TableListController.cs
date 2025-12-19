using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TableListController : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private GameObject tableListPanel;
    [SerializeField] private Transform tableListContainer; // Контейнер для элементов списка
    [SerializeField] private GameObject tableItemPrefab; // Префаб элемента стола (если есть)
    [SerializeField] private Button backButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Transform invitesContainer; // Контейнер для инвайтов (опционально)
    [SerializeField] private GameObject inviteItemPrefab; // Префаб элемента инвайта (опционально)

    [Header("Что скрывать при открытии")]
    [SerializeField] private GameObject primaryButtons;
    [SerializeField] private GameObject menuBackground;

    private List<TableInfo> availableTables = new List<TableInfo>();
    private List<TableInvite> userInvites = new List<TableInvite>();
    private MenuPlayLauncher menuPlayLauncher;

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(CloseTableList);
        
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshTableList);

        if (tableListPanel != null)
            tableListPanel.SetActive(false);

        menuPlayLauncher = FindObjectOfType<MenuPlayLauncher>();
    }

    public void ShowTableList()
    {
        LoadTables();
        LoadInvites();
        RefreshTableList();
        RefreshInvitesList();
        
        if (tableListPanel != null)
            tableListPanel.SetActive(true);
        
        if (primaryButtons != null)
            primaryButtons.SetActive(false);
        
        if (menuBackground != null)
            menuBackground.SetActive(false);
    }

    public void CloseTableList()
    {
        if (tableListPanel != null)
            tableListPanel.SetActive(false);
        
        if (primaryButtons != null)
            primaryButtons.SetActive(true);
        
        if (menuBackground != null)
            menuBackground.SetActive(true);
    }

    private void LoadTables()
    {
        availableTables.Clear();

        // Добавляем стандартные столы (малый блайнд)
        availableTables.Add(new TableInfo("Стандартный стол", 10, 4, true)); // Малый блайнд = 10, большой = 20
        availableTables.Add(new TableInfo("Стол для новичков", 5, 6, true)); // Малый блайнд = 5, большой = 10
        availableTables.Add(new TableInfo("Стол для профессионалов", 50, 9, true)); // Малый блайнд = 50, большой = 100

        // Загружаем пользовательские столы (если есть сохраненные)
        LoadUserCreatedTables();
    }

    private void LoadUserCreatedTables()
    {
        // Загружаем сохраненные пользовательские столы
        string savedTablesJson = PlayerPrefs.GetString("UserCreatedTables", "");
        if (!string.IsNullOrEmpty(savedTablesJson))
        {
            try
            {
                TableListData data = JsonUtility.FromJson<TableListData>(savedTablesJson);
                if (data != null && data.tables != null)
                {
                    foreach (var table in data.tables)
                    {
                        // Для старых столов без поля difficulty устанавливаем Medium по умолчанию
                        // (если difficulty == 0 и это не явно Easy, значит поле отсутствовало)
                        // Но на самом деле, если поле отсутствует в JSON, оно будет 0 (Easy)
                        // Поэтому просто проверяем, что значение валидное
                        if (table.difficulty < TableDifficulty.Easy || table.difficulty > TableDifficulty.Hard)
                        {
                            table.difficulty = TableDifficulty.Medium;
                        }
                        availableTables.Add(table);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Не удалось загрузить пользовательские столы: {e.Message}");
            }
        }
    }

    public void RefreshTableList()
    {
        LoadTables();
        UpdateTableListUI();
    }

    private void UpdateTableListUI()
    {
        if (tableListContainer == null)
        {
            Debug.LogError("TableListController: tableListContainer не назначен!");
            return;
        }

        // Очищаем существующие элементы
        foreach (Transform child in tableListContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаем элементы для каждого стола
        foreach (var table in availableTables)
        {
            CreateTableItem(table);
        }
    }

    private void CreateTableItem(TableInfo table)
    {
        GameObject itemObj;

        if (tableItemPrefab != null)
        {
            itemObj = Instantiate(tableItemPrefab, tableListContainer);
        }
        else
        {
            // Создаем элемент программно
            itemObj = new GameObject($"TableItem_{table.tableName}", typeof(RectTransform), typeof(Image), typeof(Button));
            itemObj.transform.SetParent(tableListContainer, false);

            RectTransform rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 80f);

            Image image = itemObj.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Добавляем текст
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(itemObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);

            #if TM_PRO
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = table.GetDisplayName();
            tmpText.fontSize = 18;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.MidlineLeft;
            #else
            Text text = textObj.AddComponent<Text>();
            text.text = table.GetDisplayName();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            #endif
        }

        // Настраиваем кнопку
        Button button = itemObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnTableSelected(table));
        }
    }

    private void OnTableSelected(TableInfo table)
    {
        Debug.Log($"Выбран стол: {table.GetDisplayName()}");
        
        // Проверяем, является ли стол закрытым
        if (table.isPrivate)
        {
            // Проверяем, есть ли у пользователя инвайт на этот стол
            bool hasInvite = HasInviteForTable(table);
            
            if (!hasInvite)
            {
                // Если нет инвайта, проверяем пароль
                if (table.RequiresPassword())
                {
                    ShowPasswordDialog(table);
                    return;
                }
                else
                {
                    // Стол закрытый, но без пароля - доступ только по инвайту
                    ShowMessage("Этот стол закрыт. Для присоединения нужно получить приглашение от создателя.");
                    return;
                }
            }
            else
            {
                // Есть инвайт - помечаем его как принятый
                MarkInviteAsAccepted(table);
            }
        }
        
        // Генерируем или получаем tableId
        string tableId = GetTableId(table);
        
        // Устанавливаем конфигурацию стола (включая tableId для онлайн-игры)
        TableRuntimeConfig.Clear();
        TableRuntimeConfig.SetOnlineTable(tableId, table.tableName, table.smallBlind, table.maxSeats, table.difficulty);
        
        // Закрываем панель списка
        CloseTableList();
        
        // Загружаем сцену игры
        if (menuPlayLauncher != null)
        {
            menuPlayLauncher.LoadGameSceneDirectly();
        }
        else
        {
            // Fallback если MenuPlayLauncher не найден
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadGameScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            }
        }
    }

    private bool HasInviteForTable(TableInfo table)
    {
        if (!AuthManager.IsLoggedIn)
            return false;

        string currentUserId = AuthManager.CurrentUser?.username;
        if (string.IsNullOrEmpty(currentUserId))
            return false;

        string tableId = GetTableId(table);
        foreach (var invite in userInvites)
        {
            if (invite.tableId == tableId && 
                invite.invitedUserId == currentUserId && 
                !invite.isAccepted && 
                !invite.isDeclined)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Генерирует уникальный ID для стола
    /// </summary>
    private string GetTableId(TableInfo table)
    {
        if (!AuthManager.IsLoggedIn || string.IsNullOrEmpty(table.creatorId))
        {
            // Для стандартных столов используем имя стола
            return $"table_{table.tableName.Replace(" ", "_")}";
        }
        
        // Для пользовательских столов: tableName_creatorId
        return $"{table.tableName}_{table.creatorId}".Replace(" ", "_");
    }

    private void MarkInviteAsAccepted(TableInfo table)
    {
        string currentUserId = AuthManager.CurrentUser?.username;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        string tableId = GetTableId(table);
        foreach (var invite in userInvites)
        {
            if (invite.tableId == tableId && invite.invitedUserId == currentUserId)
            {
                invite.isAccepted = true;
                SaveInvites();
                break;
            }
        }
    }

    private void ShowPasswordDialog(TableInfo table)
    {
        // Простая реализация через Input.GetKeyDown - в реальном приложении лучше использовать UI диалог
        // Для Unity можно использовать простой InputField в модальном окне
        Debug.Log($"Требуется пароль для стола: {table.tableName}");
        
        // Создаем простое модальное окно с полем ввода пароля
        GameObject passwordDialog = new GameObject("PasswordDialog", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = passwordDialog.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        CanvasScaler scaler = passwordDialog.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Фон
        GameObject bg = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(passwordDialog.transform, false);
        Image bgImage = bg.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Панель диалога
        GameObject panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(passwordDialog.transform, false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 200);
        panelRect.anchoredPosition = Vector2.zero;
        
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childAlignment = TextAnchor.MiddleCenter;
        
        // Текст
        GameObject textObj = new GameObject("Text", typeof(Text));
        textObj.transform.SetParent(panel.transform, false);
        Text text = textObj.GetComponent<Text>();
        text.text = $"Введите пароль для стола:\n{table.tableName}";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(360, 40);
        
        // Поле ввода пароля
        GameObject inputObj = new GameObject("PasswordInput", typeof(Image), typeof(InputField));
        inputObj.transform.SetParent(panel.transform, false);
        InputField input = inputObj.GetComponent<InputField>();
        input.contentType = InputField.ContentType.Password;
        input.characterLimit = 50;
        
        GameObject inputTextObj = new GameObject("Text", typeof(Text));
        inputTextObj.transform.SetParent(inputObj.transform, false);
        Text inputText = inputTextObj.GetComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        inputText.fontSize = 18;
        inputText.color = Color.black;
        input.textComponent = inputText;
        
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(360, 40);
        
        RectTransform inputTextRect = inputTextObj.GetComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = new Vector2(10, 0);
        inputTextRect.offsetMax = new Vector2(-10, 0);
        
        // Кнопки
        GameObject buttonsObj = new GameObject("Buttons", typeof(HorizontalLayoutGroup));
        buttonsObj.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup buttonsLayout = buttonsObj.GetComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 10f;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childControlHeight = true;
        RectTransform buttonsRect = buttonsObj.GetComponent<RectTransform>();
        buttonsRect.sizeDelta = new Vector2(360, 50);
        
        // Кнопка OK
        GameObject okButtonObj = new GameObject("OKButton", typeof(Image), typeof(Button));
        okButtonObj.transform.SetParent(buttonsObj.transform, false);
        Button okButton = okButtonObj.GetComponent<Button>();
        Image okImage = okButtonObj.GetComponent<Image>();
        okImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        
        GameObject okTextObj = new GameObject("Text", typeof(Text));
        okTextObj.transform.SetParent(okButtonObj.transform, false);
        Text okText = okTextObj.GetComponent<Text>();
        okText.text = "OK";
        okText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        okText.fontSize = 18;
        okText.color = Color.white;
        okText.alignment = TextAnchor.MiddleCenter;
        RectTransform okRect = okButtonObj.GetComponent<RectTransform>();
        okRect.sizeDelta = new Vector2(170, 50);
        
        RectTransform okTextRect = okTextObj.GetComponent<RectTransform>();
        okTextRect.anchorMin = Vector2.zero;
        okTextRect.anchorMax = Vector2.one;
        okTextRect.sizeDelta = Vector2.zero;
        
        // Кнопка Отмена
        GameObject cancelButtonObj = new GameObject("CancelButton", typeof(Image), typeof(Button));
        cancelButtonObj.transform.SetParent(buttonsObj.transform, false);
        Button cancelButton = cancelButtonObj.GetComponent<Button>();
        Image cancelImage = cancelButtonObj.GetComponent<Image>();
        cancelImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        
        GameObject cancelTextObj = new GameObject("Text", typeof(Text));
        cancelTextObj.transform.SetParent(cancelButtonObj.transform, false);
        Text cancelText = cancelTextObj.GetComponent<Text>();
        cancelText.text = "Отмена";
        cancelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        cancelText.fontSize = 18;
        cancelText.color = Color.white;
        cancelText.alignment = TextAnchor.MiddleCenter;
        RectTransform cancelRect = cancelButtonObj.GetComponent<RectTransform>();
        cancelRect.sizeDelta = new Vector2(170, 50);
        
        RectTransform cancelTextRect = cancelTextObj.GetComponent<RectTransform>();
        cancelTextRect.anchorMin = Vector2.zero;
        cancelTextRect.anchorMax = Vector2.one;
        cancelTextRect.sizeDelta = Vector2.zero;
        
        // Обработчики
        TableInfo tableCopy = table; // Захватываем копию для замыкания
        okButton.onClick.AddListener(() => {
            string enteredPassword = input.text;
            if (enteredPassword == tableCopy.password)
            {
                Destroy(passwordDialog);
                // Пароль верный - присоединяемся
                JoinTableWithPassword(tableCopy);
            }
            else
            {
                ShowMessage("Неверный пароль!");
            }
        });
        
        cancelButton.onClick.AddListener(() => {
            Destroy(passwordDialog);
        });
    }

    private void JoinTableWithPassword(TableInfo table)
    {
        // Генерируем или получаем tableId
        string tableId = GetTableId(table);
        
        // Устанавливаем конфигурацию стола (включая tableId для онлайн-игры)
        TableRuntimeConfig.Clear();
        TableRuntimeConfig.SetOnlineTable(tableId, table.tableName, table.smallBlind, table.maxSeats, table.difficulty);
        
        // Закрываем панель списка
        CloseTableList();
        
        // Загружаем сцену игры
        if (menuPlayLauncher != null)
        {
            menuPlayLauncher.LoadGameSceneDirectly();
        }
        else
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadGameScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            }
        }
    }

    private void ShowMessage(string message)
    {
        Debug.LogWarning(message);
        // В реальном приложении здесь должен быть UI для отображения сообщений
    }

    public static void SaveUserCreatedTable(TableInfo table)
    {
        // Загружаем существующие столы
        List<TableInfo> userTables = new List<TableInfo>();
        string savedTablesJson = PlayerPrefs.GetString("UserCreatedTables", "");
        
        if (!string.IsNullOrEmpty(savedTablesJson))
        {
            try
            {
                TableListData data = JsonUtility.FromJson<TableListData>(savedTablesJson);
                if (data != null && data.tables != null)
                {
                    userTables.AddRange(data.tables);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Не удалось загрузить пользовательские столы: {e.Message}");
            }
        }

        // Добавляем новый стол
        userTables.Add(table);

        // Сохраняем обратно
        TableListData newData = new TableListData { tables = userTables.ToArray() };
        string json = JsonUtility.ToJson(newData);
        PlayerPrefs.SetString("UserCreatedTables", json);
        PlayerPrefs.Save();
    }

    private void LoadInvites()
    {
        userInvites.Clear();
        
        if (!AuthManager.IsLoggedIn)
            return;

        string currentUserId = AuthManager.CurrentUser?.username;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        // Загружаем инвайты из PlayerPrefs
        string savedInvitesJson = PlayerPrefs.GetString($"TableInvites_{currentUserId}", "");
        if (!string.IsNullOrEmpty(savedInvitesJson))
        {
            try
            {
                TableInviteListData data = JsonUtility.FromJson<TableInviteListData>(savedInvitesJson);
                if (data != null && data.invites != null)
                {
                    // Фильтруем только не принятые и не отклоненные инвайты
                    foreach (var invite in data.invites)
                    {
                        if (!invite.isAccepted && !invite.isDeclined)
                        {
                            userInvites.Add(invite);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Не удалось загрузить инвайты: {e.Message}");
            }
        }
    }

    private void SaveInvites()
    {
        if (!AuthManager.IsLoggedIn)
            return;

        string currentUserId = AuthManager.CurrentUser?.username;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        TableInviteListData data = new TableInviteListData { invites = userInvites.ToArray() };
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString($"TableInvites_{currentUserId}", json);
        PlayerPrefs.Save();
    }

    private void RefreshInvitesList()
    {
        if (invitesContainer == null)
            return;

        // Очищаем существующие элементы
        foreach (Transform child in invitesContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаем элементы для каждого инвайта
        foreach (var invite in userInvites)
        {
            CreateInviteItem(invite);
        }
    }

    private void CreateInviteItem(TableInvite invite)
    {
        GameObject itemObj;

        if (inviteItemPrefab != null)
        {
            itemObj = Instantiate(inviteItemPrefab, invitesContainer);
        }
        else
        {
            // Создаем элемент программно
            itemObj = new GameObject($"InviteItem_{invite.tableId}", typeof(RectTransform), typeof(Image), typeof(Button));
            itemObj.transform.SetParent(invitesContainer, false);

            RectTransform rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 80f);

            Image image = itemObj.GetComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.5f, 0.8f);

            // Добавляем текст
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(itemObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = new Vector2(0.7f, 1f);
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);

            #if TM_PRO
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = invite.GetDisplayMessage();
            tmpText.fontSize = 16;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.MidlineLeft;
            #else
            Text text = textObj.AddComponent<Text>();
            text.text = invite.GetDisplayMessage();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            #endif

            // Кнопка "Принять"
            GameObject acceptButtonObj = new GameObject("AcceptButton", typeof(Image), typeof(Button));
            acceptButtonObj.transform.SetParent(itemObj.transform, false);
            Button acceptButton = acceptButtonObj.GetComponent<Button>();
            Image acceptImage = acceptButtonObj.GetComponent<Image>();
            acceptImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
            RectTransform acceptRect = acceptButtonObj.GetComponent<RectTransform>();
            acceptRect.anchorMin = new Vector2(0.7f, 0f);
            acceptRect.anchorMax = new Vector2(1f, 1f);
            acceptRect.offsetMin = new Vector2(5f, 5f);
            acceptRect.offsetMax = new Vector2(-5f, -5f);

            GameObject acceptTextObj = new GameObject("Text", typeof(Text));
            acceptTextObj.transform.SetParent(acceptButtonObj.transform, false);
            Text acceptText = acceptTextObj.GetComponent<Text>();
            acceptText.text = "Принять";
            acceptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            acceptText.fontSize = 14;
            acceptText.color = Color.white;
            acceptText.alignment = TextAnchor.MiddleCenter;
            RectTransform acceptTextRect = acceptTextObj.GetComponent<RectTransform>();
            acceptTextRect.anchorMin = Vector2.zero;
            acceptTextRect.anchorMax = Vector2.one;
            acceptTextRect.sizeDelta = Vector2.zero;

            TableInvite inviteCopy = invite; // Для замыкания
            acceptButton.onClick.AddListener(() => {
                inviteCopy.isAccepted = true;
                SaveInvites();
                LoadInvites();
                RefreshInvitesList();
                
                // Находим стол по инвайту и присоединяемся
                TableInfo table = FindTableById(inviteCopy.tableId);
                if (table != null)
                {
                    OnTableSelected(table);
                }
            });
        }
    }

    private TableInfo FindTableById(string tableId)
    {
        foreach (var table in availableTables)
        {
            if (GetTableId(table) == tableId)
            {
                return table;
            }
        }
        return null;
    }

    public static void AddInvite(TableInvite invite)
    {
        if (!AuthManager.IsLoggedIn)
            return;

        string currentUserId = AuthManager.CurrentUser?.username;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        // Загружаем существующие инвайты
        List<TableInvite> invites = new List<TableInvite>();
        string savedInvitesJson = PlayerPrefs.GetString($"TableInvites_{invite.invitedUserId}", "");
        
        if (!string.IsNullOrEmpty(savedInvitesJson))
        {
            try
            {
                TableInviteListData data = JsonUtility.FromJson<TableInviteListData>(savedInvitesJson);
                if (data != null && data.invites != null)
                {
                    invites.AddRange(data.invites);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Не удалось загрузить инвайты: {e.Message}");
            }
        }

        // Проверяем, нет ли уже такого инвайта
        bool exists = false;
        foreach (var existingInvite in invites)
        {
            if (existingInvite.tableId == invite.tableId && existingInvite.invitedUserId == invite.invitedUserId)
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            invites.Add(invite);
        }

        // Сохраняем обратно
        TableInviteListData newData = new TableInviteListData { invites = invites.ToArray() };
        string json = JsonUtility.ToJson(newData);
        PlayerPrefs.SetString($"TableInvites_{invite.invitedUserId}", json);
        PlayerPrefs.Save();

        Debug.Log($"Инвайт добавлен для пользователя {invite.invitedUsername}");
    }
}

[System.Serializable]
public class TableInviteListData
{
    public TableInvite[] invites;
}

[System.Serializable]
public class TableListData
{
    public TableInfo[] tables;
}

