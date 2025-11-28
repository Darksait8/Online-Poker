using System;
using System.Collections.Generic;

public class Deck
{
    private readonly List<Card> cards = new List<Card>(52);
    private int index;
    private Random rng;
    private static int shuffleCounter = 0; // Счетчик для дополнительной энтропии

    public Deck()
    {
        // Используем комбинацию нескольких источников энтропии для более надежного seed
        int seed = GenerateSecureSeed();
        rng = new Random(seed);
        Reset();
    }

    private int GenerateSecureSeed()
    {
        // Комбинируем несколько источников энтропии для максимальной случайности
        unchecked
        {
            int seed = (int)DateTime.Now.Ticks;
            seed ^= Environment.TickCount;
            seed ^= Guid.NewGuid().GetHashCode();
            seed ^= (shuffleCounter++ << 16);
            seed ^= UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            return seed;
        }
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
        int seed = GenerateSecureSeed();
        rng = new Random(seed);
        
        // Правильный алгоритм Fisher–Yates shuffle (один проход достаточен)
        // Этот алгоритм гарантирует равномерное распределение всех перестановок
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
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