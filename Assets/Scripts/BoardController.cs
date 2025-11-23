using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class BoardController : MonoBehaviour
{
    [Header("UI слоты борда (флоп, тёрн, ривер)")]
    [SerializeField] private Image flop1;
    [SerializeField] private Image flop2;
    [SerializeField] private Image flop3;
    [SerializeField] private Image turn;
    [SerializeField] private Image river;

    [Header("Рубашка/пустая карта")]
    [SerializeField] private Sprite cardBack;

    [Header("Размеры карт")]
    [SerializeField] private Vector2 cardSize = new Vector2(80f, 112f); // Нормальные пропорции общих карт (пропорции 5:7 как у реальных карт)
    
    [Header("Позиционирование карт")]
    [SerializeField] private float cardSpacing = 90f; // Расстояние между картами (от центра к центру)
    [SerializeField] private float verticalOffset = 0f; // Смещение по Y (0 = центр стола)

    private void Awake()
    {
        SetCardSizes();
        AlignBoardCards();
    }

    private void SetCardSizes()
    {
        SetImageSize(flop1);
        SetImageSize(flop2);
        SetImageSize(flop3);
        SetImageSize(turn);
        SetImageSize(river);
    }
    
    /// <summary>
    /// Выравнивает карты флопа, терна и ривера по горизонтальной линии
    /// </summary>
    private void AlignBoardCards()
    {
        float baseY = verticalOffset; // Используем настраиваемое смещение по Y
        
        // Флоп: три карты слева от центра
        if (flop1 != null && flop1.rectTransform != null)
        {
            flop1.rectTransform.anchoredPosition = new Vector2(-cardSpacing * 2, baseY);
            flop1.rectTransform.localScale = Vector3.one; // Убираем искажение
        }
        if (flop2 != null && flop2.rectTransform != null)
        {
            flop2.rectTransform.anchoredPosition = new Vector2(-cardSpacing, baseY);
            flop2.rectTransform.localScale = Vector3.one;
        }
        if (flop3 != null && flop3.rectTransform != null)
        {
            flop3.rectTransform.anchoredPosition = new Vector2(0f, baseY);
            flop3.rectTransform.localScale = Vector3.one;
        }
        
        // Терн: справа от флопа
        if (turn != null && turn.rectTransform != null)
        {
            turn.rectTransform.anchoredPosition = new Vector2(cardSpacing, baseY);
            turn.rectTransform.localScale = Vector3.one;
        }
        
        // Ривер: справа от терна
        if (river != null && river.rectTransform != null)
        {
            river.rectTransform.anchoredPosition = new Vector2(cardSpacing * 2, baseY);
            river.rectTransform.localScale = Vector3.one;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            SetCardSizes();
            AlignBoardCards();
        }
    }
#endif

    private void SetImageSize(Image img)
    {
        if (img == null) return;
        
        // Настраиваем Image для правильного отображения карт
        img.type = Image.Type.Simple;
        img.preserveAspect = true; // Сохраняем пропорции карт
        
        RectTransform rt = img.rectTransform;
        if (rt != null)
        {
            rt.sizeDelta = cardSize;
            // Убираем искажающее масштабирование
            rt.localScale = Vector3.one;
        }
    }

    public void Clear()
    {
        // Скрываем все карты
        Hide(flop1);
        Hide(flop2);
        Hide(flop3);
        Hide(turn);
        Hide(river);
    }
    
    public void ResetBoard()
    {
        Clear();
    }

    public void SetFlopCards(Card[] cards)
    {
        if (cards.Length != 3) return;
        SetImage(flop1, CardSpriteProvider.GetSprite(cards[0]));
        SetImage(flop2, CardSpriteProvider.GetSprite(cards[1]));
        SetImage(flop3, CardSpriteProvider.GetSprite(cards[2]));
    }

    // Совместимость: RevealFlop используется в GameStateMachine
    public void RevealFlop(Card c1, Card c2, Card c3)
    {
        SetFlopCards(new[] { c1, c2, c3 });
    }

    public void SetTurnCard(Card card)
    {
        SetImage(turn, CardSpriteProvider.GetSprite(card));
    }

    public void SetRiverCard(Card card)
    {
        SetImage(river, CardSpriteProvider.GetSprite(card));
    }

    public void RevealTurn(Card c)
    {
        SetImage(turn, CardSpriteProvider.GetSprite(c));
    }

    public void RevealRiver(Card c)
    {
        SetImage(river, CardSpriteProvider.GetSprite(c));
    }

    private void SetImage(Image img, Sprite s)
    {
        if (img == null) return;
        
        if (s == null)
        {
            Hide(img);
            return;
        }

        img.gameObject.SetActive(true);
        img.sprite = s;
        img.color = Color.white;
        img.type = Image.Type.Simple;
        img.preserveAspect = true; // Сохраняем пропорции спрайта
        
        // Устанавливаем размер после установки спрайта
        if (img.rectTransform != null)
        {
            img.rectTransform.sizeDelta = cardSize;
            // Убираем масштабирование, которое может искажать карты
            img.rectTransform.localScale = Vector3.one;
        }
    }

    private void Hide(Image img)
    {
        if (img == null) return;
        
        // Вместо отключения компонента Image, отключаем весь GameObject
        img.gameObject.SetActive(false);
    }
}


