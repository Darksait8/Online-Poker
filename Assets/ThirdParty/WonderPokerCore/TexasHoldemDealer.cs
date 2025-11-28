using System;
using System.Collections.Generic;

namespace WonderPokerCore
{
    public class TexasHoldemDealer : ICardsDealer
    {
        public CardsCollection Deck { get; set; }
        public int Position { get; set; } = -1;

        private Random random;

        public TexasHoldemDealer()
        {
            // Инициализируем Random с надежным seed
            int seed = GenerateSecureSeed();
            random = new Random(seed);
        }

        private int GenerateSecureSeed()
        {
            // Комбинируем несколько источников энтропии для максимальной случайности
            unchecked
            {
                int seed = (int)DateTime.Now.Ticks;
                seed ^= Environment.TickCount;
                seed ^= Guid.NewGuid().GetHashCode();
                return seed;
            }
        }

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
            if (Deck?.Cards == null || Deck.Cards.Count == 0) return;
            
            // Правильный алгоритм Fisher–Yates shuffle вместо OrderBy
            // OrderBy с random.Next() дает неравномерное распределение!
            var cards = Deck.Cards;
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }
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

