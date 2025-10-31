using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPlayLauncher : MonoBehaviour
{
    [Header("UI (выбор)")]
    [SerializeField] private Button joinDefaultTableButton; // выбрать готовый стол
    [SerializeField] private Button openCreateTableButton;  // открыть создание стола

    [Header("UI (создание стола)")]
    [SerializeField] private GameObject createPanel;        // панель редактирования
    [SerializeField] private InputField bigBlindInput;      // стартовый блайнд (BB)
    [SerializeField] private Dropdown maxSeatsDropdown;     // 2..9
    [SerializeField] private Button createAndPlayButton;    // создать и зайти
    [SerializeField] private Button cancelCreateButton;     // назад

    [Header("Что скрывать/показывать при переходах")]
    [SerializeField] private GameObject primaryButtons;     // блок главных кнопок
    [SerializeField] private GameObject menuBackground;     // фон главного меню (если есть)

    [Header("Сцена со столом")]
    [SerializeField] private string gameSceneName = "Main";

    private void Awake()
    {
        if (joinDefaultTableButton != null) joinDefaultTableButton.onClick.AddListener(JoinDefaultTable);
        if (openCreateTableButton != null) openCreateTableButton.onClick.AddListener(OpenCreatePanel);
        if (createAndPlayButton != null) createAndPlayButton.onClick.AddListener(CreateAndPlay);
        if (cancelCreateButton != null) cancelCreateButton.onClick.AddListener(CloseCreatePanel);

        if (createPanel != null) createPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (joinDefaultTableButton != null) joinDefaultTableButton.onClick.RemoveListener(JoinDefaultTable);
        if (openCreateTableButton != null) openCreateTableButton.onClick.RemoveListener(OpenCreatePanel);
        if (createAndPlayButton != null) createAndPlayButton.onClick.RemoveListener(CreateAndPlay);
        if (cancelCreateButton != null) cancelCreateButton.onClick.RemoveListener(CloseCreatePanel);
    }

    private void JoinDefaultTable()
    {
        // Проверяем авторизацию перед началом игры
        if (!AuthManager.IsLoggedIn)
        {
            Debug.LogWarning("Пользователь не авторизован. Перенаправляем к авторизации.");
            SceneTransitionManager.Instance?.LoadAuthScene();
            return;
        }
        
        TableRuntimeConfig.SetPreset(1000, 4); // BB=1000, места=4
        HideMainMenu();
        SceneTransitionManager.Instance?.LoadGameScene();
    }

    private void OpenCreatePanel()
    {
        if (createPanel != null) createPanel.SetActive(true);
        // Скрываем основной фон и кнопки, чтобы панели не накладывались
        if (primaryButtons != null) primaryButtons.SetActive(false);
        if (menuBackground != null) menuBackground.SetActive(false);
    }

    private void CloseCreatePanel()
    {
        if (createPanel != null) createPanel.SetActive(false);
        // Возвращаем главное меню
        if (primaryButtons != null) primaryButtons.SetActive(true);
        if (menuBackground != null) menuBackground.SetActive(true);
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
        
        int bb = 1000;
        int seats = 6;

        // Читаем значение Big Blind
        if (bigBlindInput != null)
        {
            string bbText = bigBlindInput.text.Trim();
            Debug.Log($"Big Blind input text: '{bbText}'");
            
            if (int.TryParse(bbText, out var parsed))
            {
                bb = Mathf.Clamp(parsed, 10, 1000000);
                Debug.Log($"Big Blind parsed: {bb}");
            }
            else
            {
                Debug.LogWarning($"Не удалось распарсить Big Blind: '{bbText}', используем значение по умолчанию: {bb}");
            }
        }
        else
        {
            Debug.LogWarning("bigBlindInput не назначен в Inspector!");
        }

        // Читаем значение Max Seats
        if (maxSeatsDropdown != null)
        {
            seats = Mathf.Clamp(maxSeatsDropdown.value + 2, 2, 9);
            Debug.Log($"Max Seats dropdown value: {maxSeatsDropdown.value}, calculated seats: {seats}");
        }
        else
        {
            Debug.LogWarning("maxSeatsDropdown не назначен в Inspector!");
        }

        // Очищаем предыдущую конфигурацию и устанавливаем новую
        Debug.Log($"Очищаем предыдущую конфигурацию и устанавливаем новую: Big Blind = {bb}, Max Seats = {seats}");
        TableRuntimeConfig.Clear();
        TableRuntimeConfig.SetPreset(bb, seats);
        
        // Проверяем, что конфигурация установлена
        Debug.Log($"Конфигурация установлена: Big Blind = {TableRuntimeConfig.BigBlind}, Max Seats = {TableRuntimeConfig.MaxSeats}, HasConfig = {TableRuntimeConfig.HasConfig}");
        
        HideMainMenu();
        SceneTransitionManager.Instance?.LoadGameScene();
    }

    private void HideMainMenu()
    {
        if (primaryButtons != null) primaryButtons.SetActive(false);
        if (menuBackground != null) menuBackground.SetActive(false);
        // саму панель можно также скрыть
        gameObject.SetActive(false);
    }
}



