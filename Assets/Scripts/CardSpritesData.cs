using UnityEngine;

[CreateAssetMenu(fileName = "CardSpritesData", menuName = "Poker/Card Sprites Data")]
public class CardSpritesData : ScriptableObject
{
    [Header("Спрайты карт (52 карты в порядке: Clubs, Diamonds, Hearts, Spades)")]
    [Header("Каждая масть: 2, 3, 4, 5, 6, 7, 8, 9, 10, J, Q, K, A")]
    public Sprite[] cardSprites = new Sprite[52];
    
    [Header("Рубашка карты")]
    public Sprite cardBack;

    public Sprite GetSprite(Card card)
    {
        int suitIndex = (int)card.Suit;  // 0=Clubs, 1=Diamonds, 2=Hearts, 3=Spades
        int rankIndex = (int)card.Rank - 2;  // 0=Two, 1=Three, ..., 12=Ace
        int spriteIndex = suitIndex * 13 + rankIndex;

        if (spriteIndex >= 0 && spriteIndex < cardSprites.Length)
        {
            if (cardSprites[spriteIndex] != null)
            {
                return cardSprites[spriteIndex];
            }
            else
            {
                Debug.LogWarning($"Спрайт для карты {card} (индекс {spriteIndex}) равен NULL!");
            }
        }
        else
        {
            Debug.LogWarning($"Индекс {spriteIndex} для карты {card} выходит за границы массива (размер: {cardSprites.Length})");
        }

        return cardBack; // Возвращаем рубашку как fallback
    }
}