using UnityEngine;

public class CardSpriteTest : MonoBehaviour
{
    [Header("Тест отображения карт")]
    public bool testOnStart = true;
    
    private void Start()
    {
        if (testOnStart)
        {
            TestCardSprites();
        }
    }
    
    [ContextMenu("Test Card Sprites")]
    public void TestCardSprites()
    {
        Debug.Log("=== Тест CardSpriteProvider ===");
        
        // Тестируем все масти и несколько рангов
        var testCards = new Card[]
        {
            // Трефы
            new Card(Suit.Clubs, Rank.Two),      // Индекс 0
            new Card(Suit.Clubs, Rank.Three),    // Индекс 1
            new Card(Suit.Clubs, Rank.Ace),      // Индекс 12
            
            // Бубны
            new Card(Suit.Diamonds, Rank.Two),   // Индекс 13
            new Card(Suit.Diamonds, Rank.King),  // Индекс 24
            
            // Червы
            new Card(Suit.Hearts, Rank.Two),     // Индекс 26
            new Card(Suit.Hearts, Rank.Queen),   // Индекс 37
            
            // Пики
            new Card(Suit.Spades, Rank.Two),     // Индекс 39
            new Card(Suit.Spades, Rank.Ace)      // Индекс 51
        };
        
        foreach (var card in testCards)
        {
            // Вычисляем ожидаемый индекс
            int suitIndex = (int)card.Suit;
            int rankIndex = (int)card.Rank - 2;
            int expectedIndex = suitIndex * 13 + rankIndex;
            
            var sprite = CardSpriteProvider.GetSprite(card);
            if (sprite != null)
            {
                Debug.Log($"✓ Карта {card} (индекс {expectedIndex}) -> спрайт {sprite.name}");
            }
            else
            {
                Debug.LogError($"✗ Карта {card} (индекс {expectedIndex}) -> спрайт не найден!");
            }
        }
        
        // Тестируем рубашку
        var cardBack = CardSpriteProvider.GetCardBack();
        if (cardBack != null)
        {
            Debug.Log($"✓ Рубашка карты -> спрайт {cardBack.name}");
        }
        else
        {
            Debug.LogWarning("✗ Рубашка карты не найдена");
        }
        
        Debug.Log("=== Конец теста ===");
    }
    
    [ContextMenu("Test Current Cards in Game")]
    public void TestCurrentCardsInGame()
    {
        Debug.Log("=== Тест текущих карт в игре ===");
        
        // Находим все места с картами
        var seats = FindObjectsOfType<NewBehaviourScript>();
        foreach (var seat in seats)
        {
            Debug.Log($"Место: {seat.name}");
        }
        
        // Тестируем несколько случайных карт из колоды
        var deck = new Deck();
        for (int i = 0; i < 5; i++)
        {
            var card = deck.Draw();
            var sprite = CardSpriteProvider.GetSprite(card);
            Debug.Log($"Карта из колоды: {card} -> {(sprite != null ? sprite.name : "NULL")}");
        }
        
        Debug.Log("=== Конец теста текущих карт ===");
    }
}