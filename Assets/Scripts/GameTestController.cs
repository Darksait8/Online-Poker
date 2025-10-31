using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Контроллер для тестирования покерной игры
/// </summary>
public class GameTestController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button addPlayerButton;
    [SerializeField] private Button startHandButton;
    [SerializeField] private Button setBlindsButton;
    [SerializeField] private InputField playerNameInput;
    [SerializeField] private InputField stackInput;
    [SerializeField] private InputField smallBlindInput;
    [SerializeField] private InputField bigBlindInput;
    [SerializeField] private Text statusText;

    [Header("Game References")]
    [SerializeField] private GameManager gameManager;

    private int playerCounter = 1;

    private void Awake()
    {
        // Находим GameManager если не назначен
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        // Подключаем кнопки
        if (addPlayerButton != null)
            addPlayerButton.onClick.AddListener(AddTestPlayer);
        
        if (startHandButton != null)
            startHandButton.onClick.AddListener(StartHand);
            
        if (setBlindsButton != null)
            setBlindsButton.onClick.AddListener(SetBlinds);

        // Устанавливаем значения по умолчанию
        if (playerNameInput != null)
            playerNameInput.text = $"Player {playerCounter}";
        
        if (stackInput != null)
            stackInput.text = "1000";
            
        if (smallBlindInput != null)
            smallBlindInput.text = "10";
            
        if (bigBlindInput != null)
            bigBlindInput.text = "20";

        UpdateStatus();
    }

    private void OnDestroy()
    {
        if (addPlayerButton != null)
            addPlayerButton.onClick.RemoveListener(AddTestPlayer);
        
        if (startHandButton != null)
            startHandButton.onClick.RemoveListener(StartHand);
            
        if (setBlindsButton != null)
            setBlindsButton.onClick.RemoveListener(SetBlinds);
    }

    /// <summary>
    /// Добавляет тестового игрока
    /// </summary>
    public void AddTestPlayer()
    {
        if (gameManager == null)
        {
            UpdateStatus("GameManager not found!");
            return;
        }

        string playerName = playerNameInput != null ? playerNameInput.text : $"Player {playerCounter}";
        int stack = stackInput != null ? int.Parse(stackInput.text) : 1000;

        if (string.IsNullOrEmpty(playerName))
            playerName = $"Player {playerCounter}";

        bool success = gameManager.AddPlayer(playerName, stack);
        
        if (success)
        {
            playerCounter++;
            if (playerNameInput != null)
                playerNameInput.text = $"Player {playerCounter}";
            
            UpdateStatus($"Added {playerName} with {stack} chips");
        }
        else
        {
            UpdateStatus("Failed to add player (table full?)");
        }
    }

    /// <summary>
    /// Начинает новую раздачу
    /// </summary>
    public void StartHand()
    {
        if (gameManager == null)
        {
            UpdateStatus("GameManager not found!");
            return;
        }

        gameManager.StartNewHand();
        UpdateStatus("Hand started!");
    }

    /// <summary>
    /// Устанавливает размеры блайндов
    /// </summary>
    public void SetBlinds()
    {
        if (gameManager == null)
        {
            UpdateStatus("GameManager not found!");
            return;
        }

        int smallBlind = smallBlindInput != null ? int.Parse(smallBlindInput.text) : 10;
        int bigBlind = bigBlindInput != null ? int.Parse(bigBlindInput.text) : 20;

        gameManager.SetBlindLevels(smallBlind, bigBlind);
        UpdateStatus($"Blinds set to {smallBlind}/{bigBlind}");
    }

    /// <summary>
    /// Добавляет несколько тестовых игроков сразу
    /// </summary>
    [ContextMenu("Add Test Players")]
    public void AddTestPlayers()
    {
        if (gameManager == null) return;

        string[] names = { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank" };
        int[] stacks = { 1000, 1500, 800, 1200, 900, 1100 };

        for (int i = 0; i < names.Length && i < 6; i++)
        {
            gameManager.AddPlayer(names[i], stacks[i]);
        }

        UpdateStatus($"Added {names.Length} test players");
    }

    /// <summary>
    /// Обновляет текст статуса
    /// </summary>
    private void UpdateStatus(string message = "")
    {
        if (statusText == null) return;

        if (string.IsNullOrEmpty(message))
        {
            if (gameManager != null)
            {
                var (sb, bb) = gameManager.GetBlindLevels();
                statusText.text = $"Players: {gameManager.Players.Count}, Phase: {gameManager.CurrentPhase}, Blinds: {sb}/{bb}";
            }
            else
            {
                statusText.text = "GameManager not found";
            }
        }
        else
        {
            statusText.text = message;
        }
    }

    private void Update()
    {
        // Обновляем статус каждую секунду
        if (Time.frameCount % 60 == 0)
        {
            UpdateStatus();
        }
    }
}