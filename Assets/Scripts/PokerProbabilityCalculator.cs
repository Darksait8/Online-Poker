using System.Collections.Generic;

/// <summary>
/// Калькулятор вероятностей покерных комбинаций для Texas Hold'em
/// </summary>
public static class PokerProbabilityCalculator
{
    /// <summary>
    /// Информация о комбинации и её вероятности
    /// </summary>
    public class CombinationInfo
    {
        public string Name { get; set; }
        public string RussianName { get; set; }
        public double Probability { get; set; } // Вероятность в процентах
        public string Odds { get; set; } // Шансы в формате "1 из X"
        public int Rank { get; set; } // Ранг комбинации (1 = лучшая)
        public Card[] ExampleCards { get; set; } // Пример карт для визуализации (5 карт)
    }

    /// <summary>
    /// Возвращает все покерные комбинации с их вероятностями для Texas Hold'em (7 карт)
    /// Вероятности основаны на математических расчетах для случайной раздачи
    /// </summary>
    public static List<CombinationInfo> GetAllCombinations()
    {
        return new List<CombinationInfo>
        {
            new CombinationInfo
            {
                Name = "Royal Flush",
                RussianName = "Роял-флэш",
                Probability = 0.000154,
                Odds = "1 из 649,740",
                Rank = 1,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.Ace),
                    new Card(Suit.Spades, Rank.King),
                    new Card(Suit.Spades, Rank.Queen),
                    new Card(Suit.Spades, Rank.Jack),
                    new Card(Suit.Spades, Rank.Ten)
                }
            },
            new CombinationInfo
            {
                Name = "Straight Flush",
                RussianName = "Стрит-флэш",
                Probability = 0.00139,
                Odds = "1 из 72,193",
                Rank = 2,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.Nine),
                    new Card(Suit.Spades, Rank.Eight),
                    new Card(Suit.Spades, Rank.Seven),
                    new Card(Suit.Spades, Rank.Six),
                    new Card(Suit.Spades, Rank.Five)
                }
            },
            new CombinationInfo
            {
                Name = "Four of a Kind",
                RussianName = "Каре",
                Probability = 0.0240,
                Odds = "1 из 4,165",
                Rank = 3,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.Ace),
                    new Card(Suit.Hearts, Rank.Ace),
                    new Card(Suit.Diamonds, Rank.Ace),
                    new Card(Suit.Clubs, Rank.Ace),
                    new Card(Suit.Spades, Rank.King)
                }
            },
            new CombinationInfo
            {
                Name = "Full House",
                RussianName = "Фул-хаус",
                Probability = 0.1441,
                Odds = "1 из 694",
                Rank = 4,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.King),
                    new Card(Suit.Hearts, Rank.King),
                    new Card(Suit.Diamonds, Rank.King),
                    new Card(Suit.Spades, Rank.Nine),
                    new Card(Suit.Hearts, Rank.Nine)
                }
            },
            new CombinationInfo
            {
                Name = "Flush",
                RussianName = "Флэш",
                Probability = 0.1965,
                Odds = "1 из 509",
                Rank = 5,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.Ace),
                    new Card(Suit.Spades, Rank.King),
                    new Card(Suit.Spades, Rank.Queen),
                    new Card(Suit.Spades, Rank.Jack),
                    new Card(Suit.Spades, Rank.Nine)
                }
            },
            new CombinationInfo
            {
                Name = "Straight",
                RussianName = "Стрит",
                Probability = 0.3925,
                Odds = "1 из 255",
                Rank = 6,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.Nine),
                    new Card(Suit.Hearts, Rank.Eight),
                    new Card(Suit.Diamonds, Rank.Seven),
                    new Card(Suit.Clubs, Rank.Six),
                    new Card(Suit.Spades, Rank.Five)
                }
            },
            new CombinationInfo
            {
                Name = "Three of a Kind",
                RussianName = "Тройка",
                Probability = 2.1128,
                Odds = "1 из 47",
                Rank = 7,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.Ace),
                    new Card(Suit.Hearts, Rank.Ace),
                    new Card(Suit.Diamonds, Rank.Ace),
                    new Card(Suit.Spades, Rank.King),
                    new Card(Suit.Spades, Rank.Queen)
                }
            },
            new CombinationInfo
            {
                Name = "Two Pair",
                RussianName = "Две пары",
                Probability = 4.7539,
                Odds = "1 из 21",
                Rank = 8,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.King),
                    new Card(Suit.Hearts, Rank.King),
                    new Card(Suit.Diamonds, Rank.Nine),
                    new Card(Suit.Spades, Rank.Nine),
                    new Card(Suit.Spades, Rank.Ace)
                }
            },
            new CombinationInfo
            {
                Name = "One Pair",
                RussianName = "Пара",
                Probability = 42.2569,
                Odds = "1 из 2.4",
                Rank = 9,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.Ace),
                    new Card(Suit.Hearts, Rank.Ace),
                    new Card(Suit.Spades, Rank.King),
                    new Card(Suit.Spades, Rank.Queen),
                    new Card(Suit.Spades, Rank.Jack)
                }
            },
            new CombinationInfo
            {
                Name = "High Card",
                RussianName = "Старшая карта",
                Probability = 50.1177,
                Odds = "1 из 2",
                Rank = 10,
                ExampleCards = new Card[]
                {
                    new Card(Suit.Spades, Rank.Ace),
                    new Card(Suit.Hearts, Rank.King),
                    new Card(Suit.Diamonds, Rank.Queen),
                    new Card(Suit.Clubs, Rank.Jack),
                    new Card(Suit.Spades, Rank.Nine)
                }
            }
        };
    }

    /// <summary>
    /// Форматирует вероятность для отображения
    /// </summary>
    public static string FormatProbability(double probability)
    {
        // Если вероятность очень маленькая (меньше 0.01%), показываем как "< 0.01%"
        if (probability < 0.01)
        {
            return "< 0.01%";
        }
        // Если вероятность меньше 1%, показываем 2 знака после запятой
        else if (probability < 1.0)
        {
            // Убираем лишние нули в конце
            string formatted = probability.ToString("F2");
            formatted = formatted.TrimEnd('0').TrimEnd(',', '.');
            return formatted + "%";
        }
        // Если вероятность больше или равна 1%, показываем 2 знака после запятой
        else if (probability < 100.0)
        {
            // Убираем лишние нули в конце
            string formatted = probability.ToString("F2");
            formatted = formatted.TrimEnd('0').TrimEnd(',', '.');
            return formatted + "%";
        }
        // Если 100% или больше, показываем без десятичных
        else
        {
            return "100%";
        }
    }
}

