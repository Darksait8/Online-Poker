using System;
using System.Collections.Generic;
using System.Linq;
using WonderPokerCore;

/// <summary>
/// Калькулятор вероятностей покерных комбинаций на основе известных карт
/// </summary>
public static class PokerOddsCalculator
{
    /// <summary>
    /// Рассчитывает вероятности всех комбинаций на основе известных карт
    /// </summary>
    /// <param name="holeCards">Карманные карты игрока (2 карты)</param>
    /// <param name="boardCards">Карты на столе (0-5 карт)</param>
    /// <returns>Словарь с вероятностями для каждой комбинации</returns>
    public static Dictionary<string, double> CalculateOdds(Card[] holeCards, Card[] boardCards)
    {
        var odds = new Dictionary<string, double>();
        
        if (holeCards == null || holeCards.Length != 2)
        {
            // Если нет карт игрока, возвращаем стандартные вероятности
            return GetDefaultOdds();
        }
        
        boardCards = boardCards ?? new Card[0];
        int boardCount = boardCards.Length;
        
        // Собираем все известные карты
        var knownCards = new HashSet<Card>();
        foreach (var card in holeCards)
        {
            knownCards.Add(card);
        }
        foreach (var card in boardCards)
        {
            knownCards.Add(card);
        }
        
        // Создаем колоду без известных карт
        var remainingDeck = CreateRemainingDeck(knownCards);
        
        // Определяем сколько карт нужно вытянуть
        int cardsToDraw = 5 - boardCount;
        
        // Если у игрока уже есть 5 или больше карт (hole + board >= 5), 
        // можем определить текущую комбинацию и показать 100% для неё
        if (cardsToDraw <= 0 || (holeCards.Length + boardCount >= 5))
        {
            // Все карты уже известны - можем точно определить комбинацию
            return CalculateFinalHandOdds(holeCards, boardCards);
        }
        
        // Рассчитываем вероятности для каждой комбинации
        var handsComparer = new HandsComparer();
        var allCombinations = PokerProbabilityCalculator.GetAllCombinations();
        
        long totalCombinations = 0;
        var combinationCounts = new Dictionary<string, long>();
        
        foreach (var combo in allCombinations)
        {
            combinationCounts[combo.Name] = 0;
        }
        
        // Генерируем все возможные комбинации оставшихся карт
        if (cardsToDraw == 1)
        {
            // Терн или ривер - одна карта
            totalCombinations = remainingDeck.Count;
            foreach (var card in remainingDeck)
            {
                var testBoard = boardCards.Concat(new[] { card }).ToArray();
                var bestHand = EvaluateBestHand(holeCards, testBoard, handsComparer);
                if (bestHand != null)
                {
                    combinationCounts[bestHand]++;
                }
            }
        }
        else if (cardsToDraw == 2)
        {
            // Флоп - две карты
            for (int i = 0; i < remainingDeck.Count; i++)
            {
                for (int j = i + 1; j < remainingDeck.Count; j++)
                {
                    totalCombinations++;
                    var testBoard = boardCards.Concat(new[] { remainingDeck[i], remainingDeck[j] }).ToArray();
                    var bestHand = EvaluateBestHand(holeCards, testBoard, handsComparer);
                    if (bestHand != null)
                    {
                        combinationCounts[bestHand]++;
                    }
                }
            }
        }
        else if (cardsToDraw == 3)
        {
            // Префлоп - три карты (флоп)
            for (int i = 0; i < remainingDeck.Count; i++)
            {
                for (int j = i + 1; j < remainingDeck.Count; j++)
                {
                    for (int k = j + 1; k < remainingDeck.Count; k++)
                    {
                        totalCombinations++;
                        var testBoard = new[] { remainingDeck[i], remainingDeck[j], remainingDeck[k] };
                        var bestHand = EvaluateBestHand(holeCards, testBoard, handsComparer);
                        if (bestHand != null)
                        {
                            combinationCounts[bestHand]++;
                        }
                    }
                }
            }
        }
        else if (cardsToDraw == 4)
        {
            // Префлоп - четыре карты (флоп + терн)
            // Используем выборку для производительности (5,000 случайных комбинаций)
            int sampleSize = 5000;
            var random = new System.Random();
            
            for (int sample = 0; sample < sampleSize; sample++)
            {
                totalCombinations++;
                var shuffled = remainingDeck.OrderBy(x => random.Next()).Take(4).ToArray();
                var bestHand = EvaluateBestHand(holeCards, shuffled, handsComparer);
                if (bestHand != null)
                {
                    combinationCounts[bestHand]++;
                }
            }
        }
        else if (cardsToDraw == 5)
        {
            // Префлоп - пять карт (полный борд)
            // Используем выборку для производительности (10,000 случайных комбинаций)
            int sampleSize = 10000;
            var random = new System.Random();
            
            for (int sample = 0; sample < sampleSize; sample++)
            {
                totalCombinations++;
                var shuffled = remainingDeck.OrderBy(x => random.Next()).Take(5).ToArray();
                var bestHand = EvaluateBestHand(holeCards, shuffled, handsComparer);
                if (bestHand != null)
                {
                    combinationCounts[bestHand]++;
                }
            }
        }
        
        // Конвертируем в проценты
        foreach (var combo in allCombinations)
        {
            if (totalCombinations > 0)
            {
                odds[combo.Name] = (combinationCounts[combo.Name] / (double)totalCombinations) * 100.0;
            }
            else
            {
                odds[combo.Name] = 0.0;
            }
        }
        
        return odds;
    }
    
    /// <summary>
    /// Определяет лучшую комбинацию из 7 карт
    /// </summary>
    private static string EvaluateBestHand(Card[] holeCards, Card[] boardCards, HandsComparer comparer)
    {
        if (holeCards == null || boardCards == null)
            return null;
            
        var allCards = new CardsCollection();
        
        // Конвертируем hole cards
        foreach (var card in holeCards)
        {
            allCards.Cards.Add(ConvertToWonderCard(card));
        }
        
        // Конвертируем board cards
        foreach (var card in boardCards)
        {
            allCards.Cards.Add(ConvertToWonderCard(card));
        }
        
        if (allCards.Cards.Count < 5)
            return null;
        
        try
        {
            var bestHand = comparer.FindBestHand(allCards);
            if (bestHand == null || bestHand.Cards == null || bestHand.Cards.Count < 5)
                return null;
            
            int score = comparer.EvaluateHand(bestHand);
            
            // Конвертируем score в название комбинации
            return ScoreToHandName(score);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Конвертирует Card в WonderPokerCore.Card
    /// </summary>
    private static WonderPokerCore.Card ConvertToWonderCard(Card card)
    {
        var sign = ConvertSuit(card.Suit);
        var value = ConvertRank(card.Rank);
        return new WonderPokerCore.Card(sign, value);
    }
    
    private static WonderPokerCore.CardSign ConvertSuit(Suit suit)
    {
        switch (suit)
        {
            case Suit.Clubs: return WonderPokerCore.CardSign.Club;
            case Suit.Diamonds: return WonderPokerCore.CardSign.Diamond;
            case Suit.Hearts: return WonderPokerCore.CardSign.Heart;
            case Suit.Spades: return WonderPokerCore.CardSign.Spade;
            default: return WonderPokerCore.CardSign.Club;
        }
    }
    
    private static WonderPokerCore.CardValue ConvertRank(Rank rank)
    {
        switch (rank)
        {
            case Rank.Two: return WonderPokerCore.CardValue.Two;
            case Rank.Three: return WonderPokerCore.CardValue.Three;
            case Rank.Four: return WonderPokerCore.CardValue.Four;
            case Rank.Five: return WonderPokerCore.CardValue.Five;
            case Rank.Six: return WonderPokerCore.CardValue.Six;
            case Rank.Seven: return WonderPokerCore.CardValue.Seven;
            case Rank.Eight: return WonderPokerCore.CardValue.Eight;
            case Rank.Nine: return WonderPokerCore.CardValue.Nine;
            case Rank.Ten: return WonderPokerCore.CardValue.Ten;
            case Rank.Jack: return WonderPokerCore.CardValue.Jack;
            case Rank.Queen: return WonderPokerCore.CardValue.Queen;
            case Rank.King: return WonderPokerCore.CardValue.King;
            case Rank.Ace: return WonderPokerCore.CardValue.Ace;
            default: return WonderPokerCore.CardValue.Two;
        }
    }
    
    private static string ScoreToHandName(int score)
    {
        switch (score)
        {
            case 1: return "Royal Flush";
            case 2: return "Straight Flush";
            case 3: return "Four of a Kind";
            case 4: return "Full House";
            case 5: return "Flush";
            case 6: return "Straight";
            case 7: return "Three of a Kind";
            case 8: return "Two Pair";
            case 9: return "One Pair";
            case 10: return "High Card";
            default: return "High Card";
        }
    }
    
    /// <summary>
    /// Создает колоду без известных карт
    /// </summary>
    private static List<Card> CreateRemainingDeck(HashSet<Card> knownCards)
    {
        var deck = new List<Card>();
        
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                var card = new Card(suit, rank);
                if (!knownCards.Contains(card))
                {
                    deck.Add(card);
                }
            }
        }
        
        return deck;
    }
    
    /// <summary>
    /// Рассчитывает финальные вероятности когда все карты известны
    /// </summary>
    private static Dictionary<string, double> CalculateFinalHandOdds(Card[] holeCards, Card[] boardCards)
    {
        var odds = new Dictionary<string, double>();
        var allCombinations = PokerProbabilityCalculator.GetAllCombinations();
        
        // Определяем текущую комбинацию
        var handsComparer = new HandsComparer();
        var bestHand = EvaluateBestHand(holeCards, boardCards, handsComparer);
        
        // Если комбинация определена, показываем 100% для неё
        if (!string.IsNullOrEmpty(bestHand))
        {
            foreach (var combo in allCombinations)
            {
                if (combo.Name == bestHand)
                {
                    odds[combo.Name] = 100.0;
                }
                else
                {
                    odds[combo.Name] = 0.0;
                }
            }
        }
        else
        {
            // Если не удалось определить, используем стандартные вероятности
            foreach (var combo in allCombinations)
            {
                odds[combo.Name] = combo.Probability;
            }
        }
        
        return odds;
    }
    
    /// <summary>
    /// Возвращает стандартные вероятности (когда нет известных карт)
    /// </summary>
    private static Dictionary<string, double> GetDefaultOdds()
    {
        var odds = new Dictionary<string, double>();
        var allCombinations = PokerProbabilityCalculator.GetAllCombinations();
        
        foreach (var combo in allCombinations)
        {
            odds[combo.Name] = combo.Probability;
        }
        
        return odds;
    }
}

