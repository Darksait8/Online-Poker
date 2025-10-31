# Дизайн основного геймплея покера

## Обзор

Данный документ описывает архитектуру и дизайн системы для реализации полноценного покерного геймплея. Основная цель - интегрировать логику игры с существующим UI и создать работающую покерную игру.

## Архитектура

### Основные компоненты

```
GameManager (новый)
├── PlayerManager (новый) 
├── BettingManager (новый)
├── HandEvaluator (новый)
└── TurnManager (новый)

Существующие компоненты:
├── GameStateMachine (расширить)
├── ActionPanelController (интегрировать)
├── BoardController (использовать как есть)
└── SeatsLayoutRadial (интегрировать)
```

## Компоненты и интерфейсы

### 1. CardSpriteProvider (обновить)
```csharp
public static class CardSpriteProvider
{
    private static Dictionary<Card, Sprite> cardSprites;
    
    public static Sprite GetSprite(Card card)
    {
        // Возвращает реальный спрайт карты
    }
    
    public static void LoadCardSprites()
    {
        // Загружает спрайты из Resources/Cards
    }
}
```

### 2. Player (новая модель)
```csharp
public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Stack { get; set; }
    public int CurrentBet { get; set; }
    public Card[] HoleCards { get; set; }
    public PlayerStatus Status { get; set; } // Active, Folded, AllIn
    public int SeatIndex { get; set; }
}
```

### 3. GameManager (новый основной контроллер)
```csharp
public class GameManager : MonoBehaviour
{
    public List<Player> Players { get; private set; }
    public int DealerIndex { get; private set; }
    public int CurrentPlayerIndex { get; private set; }
    public int Pot { get; private set; }
    public int CurrentBet { get; private set; }
    public GamePhase Phase { get; private set; }
    
    public void StartNewHand()
    public void ProcessPlayerAction(PlayerActionType action, int amount)
    public void NextPlayer()
    public void NextPhase()
}
```

### 4. BettingManager (новый)
```csharp
public class BettingManager
{
    public bool CanCheck(Player player)
    public bool CanCall(Player player)
    public bool CanRaise(Player player, int amount)
    public int GetCallAmount(Player player)
    public void ProcessBet(Player player, int amount)
    public bool IsBettingRoundComplete()
}
```

### 5. HandEvaluator (новый)
```csharp
public class HandEvaluator
{
    public HandRank EvaluateHand(Card[] holeCards, Card[] boardCards)
    public Player DetermineWinner(List<Player> players, Card[] boardCards)
    public List<Player> GetWinners(List<Player> players, Card[] boardCards)
}
```

### 6. TurnManager (новый)
```csharp
public class TurnManager
{
    public float TurnTimeLimit = 30f;
    public Player CurrentPlayer { get; private set; }
    
    public void StartTurn(Player player)
    public void EndTurn()
    public void OnTurnTimeout()
}
```

## Модели данных

### HandRank (перечисление)
```csharp
public enum HandRank
{
    HighCard = 1,
    Pair = 2,
    TwoPair = 3,
    ThreeOfAKind = 4,
    Straight = 5,
    Flush = 6,
    FullHouse = 7,
    FourOfAKind = 8,
    StraightFlush = 9,
    RoyalFlush = 10
}
```

### PlayerStatus (перечисление)
```csharp
public enum PlayerStatus
{
    Active,
    Folded,
    AllIn,
    SittingOut
}
```

## Обработка ошибок

1. **Недостаточно фишек для ставки**: Автоматический All-In
2. **Таймаут хода**: Автоматический Fold
3. **Некорректная ставка**: Отклонение действия с уведомлением
4. **Отключение игрока**: Автоматический Fold и пометка как SittingOut

## Стратегия тестирования

### Модульные тесты
- HandEvaluator: тестирование всех покерных комбинаций
- BettingManager: проверка логики ставок
- TurnManager: тестирование таймеров и переходов

### Интеграционные тесты  
- Полный цикл раздачи от начала до конца
- Обработка различных сценариев (All-In, Fold, Split Pot)
- Корректность UI обновлений

## Интеграция с существующим кодом

### GameStateMachine (расширение)
- Добавить интеграцию с GameManager
- Использовать новую логику переходов между фазами
- Сохранить существующую логику раздачи карт

### ActionPanelController (интеграция)
- Подключить к TurnManager для активации/деактивации
- Интегрировать с BettingManager для валидации действий
- Добавить обновление UI при изменении стеков/ставок

### SeatsLayoutRadial (интеграция)
- Связать UI мест с моделями Player
- Добавить обновление отображения стеков, ставок, статусов
- Интегрировать с системой дилера и блайндов