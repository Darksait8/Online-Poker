using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuPlayLauncher : MonoBehaviour
{
    [Header("UI (выбор)")]
    [SerializeField] private Button openCreateTableButton;  // открыть создание стола

    [Header("UI (создание стола)")]
    [SerializeField] private GameObject createPanel;        // панель редактирования
    [SerializeField] private InputField bigBlindInput;      // стартовый блайнд (BB)
    [SerializeField] private Slider maxSeatsSlider;         // 2..9 слайдер
    [SerializeField] private Text maxSeatsValueText;        // отображение текущего значения слайдера
    [SerializeField] private Button difficultyEasyButton;   // кнопка легкой сложности
    [SerializeField] private Button difficultyMediumButton; // кнопка средней сложности
    [SerializeField] private Button difficultyHardButton;   // кнопка тяжелой сложности
    [SerializeField] private Toggle isPrivateToggle;       // переключатель открытый/закрытый
    [SerializeField] private InputField passwordInput;      // пароль для закрытого стола
    [SerializeField] private Button createAndPlayButton;    // создать и зайти
    [SerializeField] private Button cancelCreateButton;     // назад
    
    private TableDifficulty selectedDifficulty = TableDifficulty.Medium;

    [Header("Что скрывать/показывать при переходах")]
    [SerializeField] private GameObject primaryButtons;     // блок главных кнопок
    [SerializeField] private GameObject menuBackground;     // фон главного меню (если есть)
    [SerializeField] private Button backToAuthButton;       // вернуться к регистрации
    [SerializeField] private Button exitGameButton;         // выйти из игры

    [Header("Список столов")]
    [SerializeField] private TableListController tableListController; // контроллер списка столов

    [Header("Сцена со столом")]
    [SerializeField] private string gameSceneName = "Main";

    private void Awake()
    {
        if (openCreateTableButton != null) openCreateTableButton.onClick.AddListener(OpenCreatePanel);
        if (createAndPlayButton != null) createAndPlayButton.onClick.AddListener(CreateAndPlay);
        if (cancelCreateButton != null) cancelCreateButton.onClick.AddListener(CloseCreatePanel);
        if (exitGameButton != null) exitGameButton.onClick.AddListener(ExitGame);
        EnsureBackButtonHook();

        // Настраиваем слайдер количества мест
        if (maxSeatsSlider != null)
        {
            maxSeatsSlider.minValue = 2;
            maxSeatsSlider.maxValue = 9;
            maxSeatsSlider.wholeNumbers = true;
            maxSeatsSlider.value = 6; // По умолчанию 6 мест
            maxSeatsSlider.onValueChanged.AddListener(OnMaxSeatsSliderChanged);
            UpdateMaxSeatsText();
        }
        
        // Настраиваем кнопки сложности
        if (difficultyEasyButton != null)
            difficultyEasyButton.onClick.AddListener(() => OnDifficultyButtonClicked(TableDifficulty.Easy));
        if (difficultyMediumButton != null)
            difficultyMediumButton.onClick.AddListener(() => OnDifficultyButtonClicked(TableDifficulty.Medium));
        if (difficultyHardButton != null)
            difficultyHardButton.onClick.AddListener(() => OnDifficultyButtonClicked(TableDifficulty.Hard));
        
        UpdateDifficultyButtonsHighlight();

        if (createPanel != null) createPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (openCreateTableButton != null) openCreateTableButton.onClick.RemoveListener(OpenCreatePanel);
        if (createAndPlayButton != null) createAndPlayButton.onClick.RemoveListener(CreateAndPlay);
        if (cancelCreateButton != null) cancelCreateButton.onClick.RemoveListener(CloseCreatePanel);
        if (exitGameButton != null) exitGameButton.onClick.RemoveListener(ExitGame);
        if (backToAuthButton != null) backToAuthButton.onClick.RemoveListener(HandleBackToAuth);
    }

    private void OpenCreatePanel()
    {
        if (createPanel != null) createPanel.SetActive(true);
        // Скрываем основной фон и кнопки, чтобы панели не накладывались
        if (primaryButtons != null) primaryButtons.SetActive(false);
        if (menuBackground != null) menuBackground.SetActive(false);
    }

    public void CloseCreatePanel()
    {
        if (createPanel != null) createPanel.SetActive(false);
        // Возвращаем главное меню
        if (primaryButtons != null) primaryButtons.SetActive(true);
        if (menuBackground != null) menuBackground.SetActive(true);
    }

    public bool IsCreatePanelOpen => createPanel != null && createPanel.activeSelf;

    public void SetMainMenuVisible(bool visible)
    {
        if (!visible)
        {
            if (createPanel != null) createPanel.SetActive(false);
            if (primaryButtons != null) primaryButtons.SetActive(false);
            if (menuBackground != null) menuBackground.SetActive(false);
        }
        else
        {
            if (primaryButtons != null) primaryButtons.SetActive(true);
            if (menuBackground != null) menuBackground.SetActive(true);
        }
    }

    private void CreateAndPlay()
    {
        // Проверяем авторизацию перед началом игры
        if (!AuthManager.IsLoggedIn)
        {
            Debug.LogWarning("Пользователь не авторизован. Перенаправляем к авторизации.");
            SceneTransitionManager.Instance?.LoadAuthScene();
            return;
        }
        
        int smallBlind = 10; // Значение по умолчанию для малого блайнда
        int seats = 6;
        TableDifficulty difficulty = TableDifficulty.Medium; // По умолчанию средняя сложность
        bool isPrivate = false;
        string password = null;

        // Читаем значение малого блайнда
        if (bigBlindInput != null)
        {
            string sbText = bigBlindInput.text.Trim();
            Debug.Log($"Small Blind input text: '{sbText}'");
            
            if (int.TryParse(sbText, out var parsed))
            {
                smallBlind = Mathf.Clamp(parsed, 1, 1000000);
                Debug.Log($"Small Blind parsed: {smallBlind}");
            }
            else
            {
                Debug.LogWarning($"Не удалось распарсить малый блайнд: '{sbText}', используем значение по умолчанию: {smallBlind}");
            }
        }
        else
        {
            Debug.LogWarning("bigBlindInput не назначен в Inspector!");
        }

        // Читаем значение Max Seats из слайдера
        if (maxSeatsSlider != null)
        {
            seats = Mathf.Clamp((int)maxSeatsSlider.value, 2, 9);
            Debug.Log($"Max Seats slider value: {maxSeatsSlider.value}, calculated seats: {seats}");
        }
        else
        {
            Debug.LogWarning("maxSeatsSlider не назначен в Inspector!");
        }

        // Читаем значение сложности из выбранной кнопки
        difficulty = selectedDifficulty;
        Debug.Log($"Selected difficulty: {difficulty}");

        // Проверяем ограничения для легкой сложности
        if (difficulty == TableDifficulty.Easy)
        {
            // Ограничиваем малый блайнд максимум 50 для легкой сложности
            if (smallBlind > 50)
            {
                smallBlind = 50;
                Debug.LogWarning($"Для легкой сложности малый блайнд ограничен 50. Установлено: {smallBlind}");
            }
        }

        // Читаем значение isPrivate
        if (isPrivateToggle != null)
        {
            isPrivate = isPrivateToggle.isOn;
            Debug.Log($"Is Private toggle: {isPrivate}");
        }

        // Читаем пароль (только если стол закрытый)
        if (isPrivate && passwordInput != null)
        {
            password = passwordInput.text.Trim();
            if (string.IsNullOrEmpty(password))
            {
                password = null; // Если пароль пустой, доступ только по инвайту
            }
            Debug.Log($"Password set: {(string.IsNullOrEmpty(password) ? "только по инвайту" : "с паролем")}");
        }

        // Очищаем предыдущую конфигурацию и устанавливаем новую
        int bigBlind = smallBlind * 2; // Большой блайнд всегда в 2 раза больше малого
        Debug.Log($"Очищаем предыдущую конфигурацию и устанавливаем новую: Small Blind = {smallBlind}, Big Blind = {bigBlind}, Max Seats = {seats}, Private = {isPrivate}");
        
        // Сохраняем созданный стол в список пользовательских столов
        string creatorId = AuthManager.IsLoggedIn ? AuthManager.CurrentUser?.username : null;
        string tableName = $"Стол {smallBlind}/{bigBlind} ({seats} мест)";
        TableInfo newTable = new TableInfo(tableName, smallBlind, seats, false, creatorId, isPrivate, password, difficulty);
        TableListController.SaveUserCreatedTable(newTable);
        Debug.Log($"Созданный стол сохранен: {newTable.GetDisplayName()}");
        
        // Генерируем уникальный ID для стола
        string tableId = $"{tableName}_{creatorId}_{System.DateTime.Now.Ticks}".Replace(" ", "_");
        
        // Устанавливаем конфигурацию как онлайн стол
        TableRuntimeConfig.Clear();
        TableRuntimeConfig.SetOnlineTable(tableId, tableName, smallBlind, seats, difficulty);
        
        // Проверяем, что конфигурация установлена
        Debug.Log($"Конфигурация установлена: Big Blind = {TableRuntimeConfig.BigBlind}, Max Seats = {TableRuntimeConfig.MaxSeats}, HasConfig = {TableRuntimeConfig.HasConfig}, IsOnline = {TableRuntimeConfig.IsOnlineTable}");
        
        HideMainMenu();
        LoadGameScene();
    }

    private void HideMainMenu()
    {
        if (primaryButtons != null) primaryButtons.SetActive(false);
        if (menuBackground != null) menuBackground.SetActive(false);
        // саму панель можно также скрыть
        gameObject.SetActive(false);
    }

    private void LoadGameScene()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadGameScene();
            return;
        }

        var sceneName = string.IsNullOrWhiteSpace(gameSceneName) ? "Main" : gameSceneName;
        SceneManager.LoadScene(sceneName);
    }

    public void LoadGameSceneDirectly()
    {
        LoadGameScene();
    }

    private void HandleBackToAuth()
    {
        if (AuthManager.IsLoggedIn)
            AuthManager.Logout();

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadAuthScene();
        else
            SceneManager.LoadScene("Auth");
    }

    private void ExitGame()
    {
        Debug.Log("Выход из игры");
        
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.QuitGame();
        }
        else
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }

    private void OnMaxSeatsSliderChanged(float value)
    {
        UpdateMaxSeatsText();
    }

    private void UpdateMaxSeatsText()
    {
        if (maxSeatsValueText != null && maxSeatsSlider != null)
        {
            maxSeatsValueText.text = $"{(int)maxSeatsSlider.value} игроков";
        }
    }

    private void OnDifficultyButtonClicked(TableDifficulty difficulty)
    {
        selectedDifficulty = difficulty;
        UpdateDifficultyButtonsHighlight();
    }

    private void UpdateDifficultyButtonsHighlight()
    {
        Color selectedColor = new Color(0.2f, 0.6f, 0.2f, 1f); // Зелёный для выбранного
        Color normalColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Серый для невыбранного
        
        if (difficultyEasyButton != null)
        {
            var colors = difficultyEasyButton.colors;
            colors.normalColor = selectedDifficulty == TableDifficulty.Easy ? selectedColor : normalColor;
            difficultyEasyButton.colors = colors;
        }
        if (difficultyMediumButton != null)
        {
            var colors = difficultyMediumButton.colors;
            colors.normalColor = selectedDifficulty == TableDifficulty.Medium ? selectedColor : normalColor;
            difficultyMediumButton.colors = colors;
        }
        if (difficultyHardButton != null)
        {
            var colors = difficultyHardButton.colors;
            colors.normalColor = selectedDifficulty == TableDifficulty.Hard ? selectedColor : normalColor;
            difficultyHardButton.colors = colors;
        }
    }

    private void EnsureBackButtonHook()
    {
        if (backToAuthButton != null)
        {
            backToAuthButton.onClick.AddListener(HandleBackToAuth);
            return;
        }

        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn == openCreateTableButton || btn == createAndPlayButton || btn == cancelCreateButton)
                continue;

            var text = btn.GetComponentInChildren<Text>(true);
            var tmpText = btn.GetComponentInChildren<TMP_Text>(true);
            string caption = text != null ? text.text : null;
            if (string.IsNullOrEmpty(caption) && tmpText != null)
                caption = tmpText.text;

            if (!string.IsNullOrEmpty(caption) && (caption.Trim().ToLowerInvariant() == "назад" || caption.Trim().ToLowerInvariant() == "вернуться к регистрации"))
            {
                backToAuthButton = btn;
                backToAuthButton.onClick.AddListener(HandleBackToAuth);
                break;
            }
        }
    }
}



