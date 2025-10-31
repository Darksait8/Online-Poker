using UnityEngine;

// Маппер карт на спрайты
public static class CardSpriteProvider
{
    private static CardSpritesData spritesData;

    public static void Initialize(CardSpritesData data)
    {
        spritesData = data;
    }

    public static Sprite GetSprite(Card card)
    {
        if (spritesData == null)
        {
            // Пробуем найти CardSpritesData в проекте
            spritesData = Resources.Load<CardSpritesData>("CardSpritesData");
            
            if (spritesData == null)
            {
                Debug.LogWarning("CardSpriteProvider: CardSpritesData не найден. Создайте его через Create > Poker > Card Sprites Data");
                return null;
            }
        }

        return spritesData.GetSprite(card);
    }

    public static Sprite GetCardBack()
    {
        if (spritesData == null)
        {
            spritesData = Resources.Load<CardSpritesData>("CardSpritesData");
        }

        return spritesData?.cardBack;
    }
}


