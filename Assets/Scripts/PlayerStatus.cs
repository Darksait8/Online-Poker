using System;

/// <summary>
/// Статус игрока в текущей раздаче
/// </summary>
public enum PlayerStatus
{
    /// <summary>
    /// Игрок активен и может делать ходы
    /// </summary>
    Active,
    
    /// <summary>
    /// Игрок сбросил карты (фолд)
    /// </summary>
    Folded,
    
    /// <summary>
    /// Игрок поставил все свои фишки (олл-ин)
    /// </summary>
    AllIn,
    
    /// <summary>
    /// Игрок не участвует в игре (отошел от стола)
    /// </summary>
    SittingOut
}