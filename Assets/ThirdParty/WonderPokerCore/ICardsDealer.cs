namespace WonderPokerCore
{
    public interface ICardsDealer
    {
        CardsCollection Deck { get; set; }
        int Position { get; set; }

        void CreateDeck();
        void ShuffleCards();
        void DealCards(GameTable gameTable, int roundNumber);
        void TakeBackCards(GameTable gameTable);
        void ChangePosition(GameTable gameTable);
    }
}

