using System;
using UnityEngine;

/// <summary>
/// Модель игрока в покерной игре
/// </summary>
[System.Serializable]
public class Player
{
    [SerializeField] private int id;
    [SerializeField] private string playerName;
    [SerializeField] private int stack;
    [SerializeField] private int currentBet;
    [SerializeField] private Card[] holeCards;
    [SerializeField] private PlayerStatus status;
    [SerializeField] private int seatIndex;
    [SerializeField] private bool hasActed;
    [SerializeField] private bool isActive = true;
    
    public bool IsFolded => status == PlayerStatus.Folded;

    /// <summary>
    /// Уникальный идентификатор игрока
    /// </summary>
    public int Id 
    { 
        get => id; 
        set => id = value; 
    }

    /// <summary>
    /// Показывает, сделал ли игрок ход в текущем раунде
    /// </summary>
    public bool HasActed
    {
        get => hasActed;
        set => hasActed = value;
    }

    /// <summary>
    /// Показывает, активен ли игрок в игре
    /// </summary>
    public bool IsActive
    {
        get => isActive && status != PlayerStatus.Folded;
        set => isActive = value;
    }

    /// <summary>
    /// Имя игрока
    /// </summary>
    /// <summary>
    /// Установить карты игрока
    /// </summary>
    /// <param name="cards">Список из двух карт</param>
    public void SetHoleCards(System.Collections.Generic.List<Card> cards)
    {
        if (cards.Count != 2)
            throw new System.ArgumentException("Player must have exactly 2 hole cards");
            
        holeCards = cards.ToArray();
    }

    /// <summary>
    /// Имя игрока
    /// </summary>
    public string Name 
    { 
        get => playerName; 
        set => playerName = value; 
    }

    /// <summary>
    /// Количество фишек у игрока
    /// </summary>
    public int Stack 
    { 
        get => stack; 
        set => stack = Mathf.Max(0, value); 
    }

    /// <summary>
    /// Текущая ставка игрока в раунде
    /// </summary>
    public int CurrentBet 
    { 
        get => currentBet; 
        set => currentBet = Mathf.Max(0, value); 
    }

    /// <summary>
    /// Карманные карты игрока (2 карты)
    /// </summary>
    public Card[] HoleCards 
    { 
        get => holeCards; 
        set => holeCards = value; 
    }

    /// <summary>
    /// Текущий статус игрока
    /// </summary>
    public PlayerStatus Status 
    { 
        get => status; 
        set => status = value; 
    }

    /// <summary>
    /// Индекс места за столом (0-5 для 6-макс стола)
    /// </summary>
    public int SeatIndex 
    { 
        get => seatIndex; 
        set => seatIndex = value; 
    }

    /// <summary>
    /// Конструктор для создания нового игрока
    /// </summary>
    /// <param name="id">Уникальный ID игрока</param>
    /// <param name="name">Имя игрока</param>
    /// <param name="initialStack">Начальный стек фишек</param>
    /// <param name="seatIndex">Индекс места за столом</param>
    public Player(int id, string name, int initialStack, int seatIndex)
    {
        this.id = id;
        this.playerName = name;
        this.stack = initialStack;
        this.currentBet = 0;
        this.holeCards = new Card[2];
        this.status = PlayerStatus.Active;
        this.seatIndex = seatIndex;
    }

    /// <summary>
    /// Конструктор по умолчанию для сериализации Unity
    /// </summary>
    public Player()
    {
        this.holeCards = new Card[2];
        this.status = PlayerStatus.Active;
    }

    /// <summary>
    /// Проверяет, может ли игрок делать ставки
    /// </summary>
    public bool CanAct => status == PlayerStatus.Active && stack > 0;

    /// <summary>
    /// Проверяет, участвует ли игрок в текущей раздаче
    /// </summary>
    public bool IsInHand => status == PlayerStatus.Active || status == PlayerStatus.AllIn;

    /// <summary>
    /// Делает ставку, списывая фишки со стека
    /// </summary>
    /// <param name="amount">Размер ставки</param>
    /// <returns>Фактический размер ставки (может быть меньше при олл-ине)</returns>
    public int MakeBet(int amount)
    {
        if (status != PlayerStatus.Active)
            return 0;

        int actualBet = Mathf.Min(amount, stack);
        stack -= actualBet;
        currentBet += actualBet;

        // Если поставили все фишки - олл-ин
        if (stack == 0 && actualBet > 0)
        {
            status = PlayerStatus.AllIn;
        }

        return actualBet;
    }

    /// <summary>
    /// Сбрасывает карты (фолд)
    /// </summary>
    public void Fold()
    {
        status = PlayerStatus.Folded;
    }

    /// <summary>
    /// Сбрасывает ставку в начале нового раунда торговли
    /// </summary>
    public void ResetBet()
    {
        currentBet = 0;
    }

    /// <summary>
    /// Подготавливает игрока к новой раздаче
    /// </summary>
    public void PrepareForNewHand()
    {
        currentBet = 0;
        holeCards = new Card[2];
        
        // Восстанавливаем статус только если игрок не отошел от стола
        if (status != PlayerStatus.SittingOut)
        {
            status = stack > 0 ? PlayerStatus.Active : PlayerStatus.SittingOut;
        }
    }

    /// <summary>
    /// Возвращает строковое представление игрока для отладки
    /// </summary>
    public override string ToString()
    {
        return $"Player {id} ({playerName}): Stack={stack}, Bet={currentBet}, Status={status}, Seat={seatIndex}";
    }
}