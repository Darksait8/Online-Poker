using WonderPokerCore;
using HoldemCard = HoldemPlayerContract.Card;
using HoldemRank = HoldemPlayerContract.ERankType;
using HoldemSuit = HoldemPlayerContract.ESuitType;

public static class HoldemCardAdapter
{
    public static HoldemCard ToHoldemCard(WonderPokerCore.Card card)
    {
        if (card == null)
            return null;

        return new HoldemCard(ToRank(card.Value), ToSuit(card.Sign));
    }

    public static HoldemRank ToRank(CardValue value)
    {
        return value switch
        {
            CardValue.Two => HoldemRank.RankTwo,
            CardValue.Three => HoldemRank.RankThree,
            CardValue.Four => HoldemRank.RankFour,
            CardValue.Five => HoldemRank.RankFive,
            CardValue.Six => HoldemRank.RankSix,
            CardValue.Seven => HoldemRank.RankSeven,
            CardValue.Eight => HoldemRank.RankEight,
            CardValue.Nine => HoldemRank.RankNine,
            CardValue.Ten => HoldemRank.RankTen,
            CardValue.Jack => HoldemRank.RankJack,
            CardValue.Queen => HoldemRank.RankQueen,
            CardValue.King => HoldemRank.RankKing,
            CardValue.Ace => HoldemRank.RankAce,
            _ => HoldemRank.RankUnknown
        };
    }

    public static HoldemSuit ToSuit(CardSign sign)
    {
        return sign switch
        {
            CardSign.Club => HoldemSuit.SuitClubs,
            CardSign.Heart => HoldemSuit.SuitHearts,
            CardSign.Spade => HoldemSuit.SuitSpades,
            CardSign.Diamond => HoldemSuit.SuitDiamonds,
            _ => HoldemSuit.SuitUnknown
        };
    }
}
