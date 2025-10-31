using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewBehaviourScript : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text stackText;
    [Header("TMP (опционально)")]
    [SerializeField] private TMP_Text nameTextTMP;
    [SerializeField] private TMP_Text stackTextTMP;
    [SerializeField] private GameObject betBubble;
    [SerializeField] private Text betText;
    [SerializeField] private TMP_Text betTextTMP;

    [Header("Карманные карты (опционально)")]
    [SerializeField] private Image hole1Image;
    [SerializeField] private Image hole2Image;
    [SerializeField] private Sprite holeCardBack;
    [SerializeField] private GameObject dealerButton;

    public void SetPlayer(string playerName, int stack, Sprite avatar = null)
    {
        if (nameTextTMP != null)
            nameTextTMP.text = playerName;
        else if (nameText != null)
            nameText.text = playerName;

        if (stackTextTMP != null)
            stackTextTMP.text = stack.ToString();
        else if (stackText != null)
            stackText.text = stack.ToString();

        if (avatarImage != null && avatar != null)
            avatarImage.sprite = avatar;
    }

    public void UpdateStack(int stack)
    {
        if (stackTextTMP != null)
            stackTextTMP.text = stack.ToString();
        else if (stackText != null)
            stackText.text = stack.ToString();
    }

    public void ShowBet(int chips)
    {
        bool show = chips > 0;
        if (betBubble != null)
            betBubble.SetActive(show);

        if (show)
        {
            if (betTextTMP != null)
                betTextTMP.text = chips.ToString();
            else if (betText != null)
                betText.text = chips.ToString();
        }
    }

    public void SetDealer(bool isDealer)
    {
        if (dealerButton != null)
            dealerButton.SetActive(isDealer);
    }

    public void HideHoles()
    {
        if (hole1Image != null) hole1Image.enabled = false;
        if (hole2Image != null) hole2Image.enabled = false;
    }

    public void ShowHole(Card a, Card b)
    {
        if (hole1Image != null)
        {
            var s = CardSpriteProvider.GetSprite(a);
            hole1Image.sprite = s != null ? s : holeCardBack;
            hole1Image.enabled = true;
        }
        if (hole2Image != null)
        {
            var s = CardSpriteProvider.GetSprite(b);
            hole2Image.sprite = s != null ? s : holeCardBack;
            hole2Image.enabled = true;
        }
    }

    private IEnumerator CheckRotationLater(RectTransform rt, string cardName, float expectedRotation)
    {
        yield return null; // Ждем один кадр
        float actualRotation = rt.localEulerAngles.z;
        if (actualRotation > 180f) actualRotation -= 360f; // Нормализуем угол
        
        Debug.Log($"[{name}] {cardName} через кадр: ожидаемый поворот={expectedRotation}°, фактический={actualRotation}°");
        
        if (Mathf.Abs(actualRotation - expectedRotation) > 1f)
        {
            Debug.LogWarning($"[{name}] {cardName} поворот сбросился! Принудительно устанавливаем {expectedRotation}°");
            rt.localRotation = Quaternion.Euler(0, 0, expectedRotation);
        }
    }

    public void ConfigureHoleLayout(Vector2 inwardDirection, float rotationDeg, float distance = 28f, float spacing = 20f)
    {
        // inwardDirection должен быть нормализован и указывать К ЦЕНТРУ стола
        if (inwardDirection.sqrMagnitude < 0.0001f) inwardDirection = new Vector2(0f, -1f);
        inwardDirection.Normalize();
        Vector2 tangent = new Vector2(-inwardDirection.y, inwardDirection.x); // перпендикуляр

        Vector2 pos1 = inwardDirection * distance - tangent * (spacing * 0.5f);
        Vector2 pos2 = inwardDirection * distance + tangent * (spacing * 0.5f);

        // Используем переданный поворот вместо вычисления собственного
        float correctRotation = rotationDeg;
        
        Debug.Log($"[{name}] ConfigureHoleLayout: используем переданный поворот={correctRotation}°");

        if (hole1Image != null)
        {
            var rt = hole1Image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos1;
            rt.localRotation = Quaternion.Euler(0, 0, correctRotation);
            Debug.Log($"[{name}] Hole1: позиция={pos1}, поворот={correctRotation}°, фактический поворот={rt.localEulerAngles.z}°");
            
            // Проверим поворот через кадр
            StartCoroutine(CheckRotationLater(rt, "Hole1", correctRotation));
        }
        if (hole2Image != null)
        {
            var rt = hole2Image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos2;
            rt.localRotation = Quaternion.Euler(0, 0, correctRotation);
            Debug.Log($"[{name}] Hole2: позиция={pos2}, поворот={correctRotation}°, фактический поворот={rt.localEulerAngles.z}°");
            
            // Проверим поворот через кадр
            StartCoroutine(CheckRotationLater(rt, "Hole2", correctRotation));
        }
    }
}
