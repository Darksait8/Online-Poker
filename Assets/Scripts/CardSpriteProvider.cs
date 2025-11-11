using UnityEngine;

// Маппер карт на спрайты
public static class CardSpriteProvider
{
    private static readonly string DefaultResourcePath = "CardSpritesData";
    private static CardSpritesData spritesData;

    public static string CurrentThemeId { get; private set; } = "default";

    public static void Initialize(CardSpritesData data)
    {
        SetTheme(data, data != null ? data.name : "default");
    }

    public static void SetTheme(CardSpritesData data, string themeId)
    {
        if (data == null)
        {
            Debug.LogWarning("CardSpriteProvider: попытка установить пустую тему карт");
            return;
        }

        spritesData = data;
        CurrentThemeId = string.IsNullOrWhiteSpace(themeId) ? "default" : themeId;
    }

    public static Sprite GetSprite(Card card)
    {
        EnsureDataLoaded();
        return spritesData != null ? spritesData.GetSprite(card) : null;
    }

    public static Sprite GetCardBack()
    {
        EnsureDataLoaded();
        return spritesData?.cardBack;
    }

    private static void EnsureDataLoaded()
    {
        if (spritesData != null) return;

        var data = Resources.Load<CardSpritesData>(DefaultResourcePath);
        if (data != null)
        {
            SetTheme(data, CurrentThemeId);
        }
        else
        {
            Debug.LogWarning("CardSpriteProvider: CardSpritesData не найден. Создайте его через Create > Poker > Card Sprites Data");
        }
    }
}


