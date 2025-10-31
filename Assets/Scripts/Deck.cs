using System;
using System.Collections.Generic;

public class Deck
{
    private readonly List<Card> cards = new List<Card>(52);
    private int index;
    private Random rng;

    public Deck()
    {
        // Используем время для более случайного seed
        rng = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));
        Reset();
    }

    public void Reset()
    {
        cards.Clear();
        for (int s = 0; s < 4; s++)
        {
            for (int r = 2; r <= 14; r++)
            {
                cards.Add(new Card((Suit)s, (Rank)r));
            }
        }
        Shuffle();
    }

    public Card DrawCard()
    {
        if (index >= cards.Count)
        {
            throw new InvalidOperationException("No cards left in deck");
        }
        return cards[index++];
    }

    public void Shuffle()
    {
        // Создаем новый Random для каждой перетасовки с новым seed
        rng = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF) + UnityEngine.Random.Range(0, 10000));
        
        // Улучшенный Fisher–Yates с несколькими проходами
        for (int pass = 0; pass < 3; pass++) // 3 прохода для лучшей случайности
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }
        index = 0;
        
        UnityEngine.Debug.Log($"Колода перетасована. Первые 5 карт: {cards[0]}, {cards[1]}, {cards[2]}, {cards[3]}, {cards[4]}");
    }

    public bool CanDraw(int count = 1) => index + count <= cards.Count;

    public Card Draw()
    {
        if (!CanDraw()) throw new InvalidOperationException("Deck is empty");
        return cards[index++];
    }
}