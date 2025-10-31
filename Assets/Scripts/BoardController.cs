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
    }

    private void Hide(Image img)
    {
        if (img == null) return;
        
        // Вместо отключения компонента Image, отключаем весь GameObject
        img.gameObject.SetActive(false);
    }
}


