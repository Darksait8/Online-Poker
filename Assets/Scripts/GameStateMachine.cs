using UnityEngine;

public enum PlayerActionType { Fold, CheckOrCall, BetOrRaise }

public class GameStateMachine : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private BoardController board;
    [SerializeField] private TableManager tableManager;

    private Deck deck;
    [SerializeField, Range(2, 9)] private int minPlayersToStart = 2;
    private GamePhase phase = GamePhase.WaitingToStart;

    private void Awake()
    {
        deck = new Deck();
    }

    private void Start()
    {
        // если есть SeatsLayoutRadial — подпишемся на изменения занятости
        var seats = FindObjectOfType<SeatsLayoutRadial>();
        if (seats != null)
        {
            seats.OnOccupiedChanged += HandleOccupiedChanged;
            HandleOccupiedChanged(seats.OccupiedCount);
        }
    }

    public void StartHand()
    {
        phase = GamePhase.PreFlop;
        
        // Проверяем, что deck не null
        if (deck == null)
        {
            deck = new Deck();
            Debug.Log("GameStateMachine: Создан новый deck");
        }
        
        deck.Reset(); // Это уже включает перетасовку
        deck.Shuffle(); // Дополнительная перетасовка для большей случайности
        
        // Проверяем, что board не null и очищаем его
        if (board == null)
        {
            board = FindObjectOfType<BoardController>();
            if (board == null)
            {
                Debug.LogWarning("GameStateMachine: BoardController не найден, пропускаем очистку борда");
            }
        }
        if (board != null)
        {
            board.ResetBoard();
        }
        // Раздаём карманные карты всем занятым местам (если есть раскладка)
        var seats = FindObjectOfType<SeatsLayoutRadial>();
        if (seats != null)
        {
            var occupiedSeats = seats.GetOccupiedSeats();
            if (occupiedSeats != null)
            {
                foreach (var ui in occupiedSeats)
                {
                    if (ui == null) continue;
                    // гарантированно включаем спрайты карт (если не назначены — поставится рубашка)
                    if (deck.CanDraw(2))
                    {
                        var c1 = deck.Draw();
                        var c2 = deck.Draw();
                        ui.ShowHole(c1, c2);
                    }
                }
            }
        }
    // Теперь RevealFlop вызывается вручную после завершения betting round
    }

    public void RevealFlop()
    {
        if (!deck.CanDraw(3)) { deck.Reset(); }
        var c1 = deck.Draw(); var c2 = deck.Draw(); var c3 = deck.Draw();
        board.RevealFlop(c1, c2, c3);
        phase = GamePhase.Flop;
    }

    public void RevealTurn()
    {
        if (!deck.CanDraw(1)) { deck.Reset(); }
        var c = deck.Draw();
        board.RevealTurn(c);
        phase = GamePhase.Turn;
    }

    public void RevealRiver()
    {
        if (!deck.CanDraw(1)) { deck.Reset(); }
        var c = deck.Draw();
        board.RevealRiver(c);
        phase = GamePhase.River;
    }

    private void Showdown()
    {
        phase = GamePhase.Showdown;
        // Подсчёт победителя добавим позже
        
        // После показа победителя завершаем раунд
        Invoke(nameof(EndHand), 2f);
    }

    private void EndHand()
    {
        // Очищаем стол
        if (board != null)
        {
            board.ResetBoard();
        }
        
        // Очищаем карты игроков
        var seats = FindObjectOfType<SeatsLayoutRadial>();
        if (seats != null)
        {
            var occupiedSeats = seats.GetOccupiedSeats();
            foreach (var ui in occupiedSeats)
            {
                if (ui != null)
                {
                    ui.HideHoles(); // Скрываем карты игрока
                }
            }
        }
        
        // Возвращаемся в состояние ожидания
        phase = GamePhase.WaitingToStart;
        
        // Проверяем количество игроков для начала новой раздачи
        if (seats != null && seats.OccupiedCount >= minPlayersToStart)
        {
            // Начинаем новую раздачу через небольшую задержку
            Invoke(nameof(StartHand), 1f);
        }
    }

    private void HandleOccupiedChanged(int occupied)
    {
        if (phase == GamePhase.WaitingToStart && occupied >= minPlayersToStart)
        {
            StartHand();
        }
    }

    // Упрощённый обработчик действий (MVP)
    public void PlayerAction(PlayerActionType action, int amount)
    {
        Debug.Log($"PlayerAction: {action} amount={amount} phase={phase}");
        
        // В зависимости от фазы и действия переходим к следующей фазе
        // Здесь должна быть более сложная логика проверки всех игроков,
        // но для демонстрации сделаем упрощенно
        switch (phase)
        {
            case GamePhase.PreFlop:
                Invoke(nameof(RevealFlop), 0.5f);
                break;
            case GamePhase.Flop:
                Invoke(nameof(RevealTurn), 0.5f);
                break;
            case GamePhase.Turn:
                Invoke(nameof(RevealRiver), 0.5f);
                break;
            case GamePhase.River:
                Invoke(nameof(Showdown), 0.5f);
                break;
        }
    }
}


