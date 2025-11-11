using WonderPokerCore;
using CoreCard = WonderPokerCore.Card;
using CoreCardSign = WonderPokerCore.CardSign;
using CoreCardValue = WonderPokerCore.CardValue;

public static class WonderCardConverter
{
    public static Card ToClientCard(CoreCard card)
    {
        Suit suit = card.Sign switch
        {
            CoreCardSign.Club => Suit.Clubs,
            CoreCardSign.Diamond => Suit.Diamonds,
            CoreCardSign.Heart => Suit.Hearts,
            CoreCardSign.Spade => Suit.Spades,
            _ => Suit.Clubs
        };

        Rank rank = (Rank)(int)card.Value;
        return new Card(suit, rank);
    }

    public static Card[] ToClientCards(CardsCollection collection)
    {
        if (collection == null || collection.Cards == null)
            return System.Array.Empty<Card>();

        Card[] result = new Card[collection.Cards.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = ToClientCard(collection.Cards[i]);
        }
        return result;
    }
}

