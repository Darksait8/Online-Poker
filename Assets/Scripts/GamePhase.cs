/// <summary>
/// Фазы покерной игры
/// </summary>
public enum GamePhase
{
    /// <summary>
    /// Ожидание начала игры
    /// </summary>
    WaitingToStart,
    
    /// <summary>
    /// Раздача карт и постановка блайндов
    /// </summary>
    PreFlop,
    
    /// <summary>
    /// Ставки после раздачи карт
    /// </summary>
    PreFlopBetting,
    
    /// <summary>
    /// Открытие флопа (3 карты)
    /// </summary>
    Flop,
    
    /// <summary>
    /// Ставки после флопа
    /// </summary>
    FlopBetting,
    
    /// <summary>
    /// Открытие терна (4-я карта)
    /// </summary>
    Turn,
    
    /// <summary>
    /// Ставки после терна
    /// </summary>
    TurnBetting,
    
    /// <summary>
    /// Открытие ривера (5-я карта)
    /// </summary>
    River,
    
    /// <summary>
    /// Ставки после ривера
    /// </summary>
    RiverBetting,
    
    /// <summary>
    /// Вскрытие карт и определение победителя
    /// </summary>
    Showdown,
    
    /// <summary>
    /// Завершение раздачи
    /// </summary>
    HandComplete
}