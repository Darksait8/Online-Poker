using System;
using System.Collections.Generic;
using System.Linq;

namespace WonderPokerCore
{
    public class TexasHoldemDealer : ICardsDealer
    {
        public CardsCollection Deck { get; set; }
        public int Position { get; set; } = -1;

        private readonly Random random = new();

        public void CreateDeck()
        {
            var deck = new List<Card>(52);
            foreach (CardValue value in Enum.GetValues(typeof(CardValue)))
            {
                foreach (CardSign sign in Enum.GetValues(typeof(CardSign)))
                {
                    deck.Add(new Card(sign, value));
                }
            }

            Deck = new CardsCollection(deck);
        }

        public void ShuffleCards()
        {
            if (Deck?.Cards == null) return;
            Deck.Cards = Deck.Cards.OrderBy(_ => random.Next()).ToList();
        }

        public void DealCards(GameTable gameTable, int roundNumber)
        {
            switch (roundNumber)
            {
                case 0:
                    DealPreflop(gameTable);
                    break;
                case 1:
                    DealFlop(gameTable);
                    break;
                case 2:
                    DealTurn(gameTable);
                    break;
                case 3:
                    DealRiver(gameTable);
                    break;
            }
        }

        public void TakeBackCards(GameTable gameTable)
        {
            foreach (Player player in gameTable.Players)
            {
                player.PlayerHand = new CardsCollection();
            }

            gameTable.ShownHelpingCards = new CardsCollection();
            Deck = null;
        }

        public void ChangePosition(GameTable gameTable)
        {
            Position++;
            if (gameTable.Players.Count > 0)
                Position %= gameTable.Players.Count;
        }

        private void DealPreflop(GameTable gameTable)
        {
            CreateDeck();
            ShuffleCards();

            foreach (Player player in gameTable.Players)
            {
                player.PlayerHand.AddCard(Deck.TakeOutCard(0));
                player.PlayerHand.AddCard(Deck.TakeOutCard(0));
            }
        }

        private void DealFlop(GameTable gameTable)
        {
            for (int i = 0; i < 3; i++)
            {
                gameTable.ShownHelpingCards.AddCard(Deck.TakeOutCard(0));
            }
        }

        private void DealTurn(GameTable gameTable)
        {
            gameTable.ShownHelpingCards.AddCard(Deck.TakeOutCard(0));
        }

        private void DealRiver(GameTable gameTable)
        {
            gameTable.ShownHelpingCards.AddCard(Deck.TakeOutCard(0));
        }
    }
}

