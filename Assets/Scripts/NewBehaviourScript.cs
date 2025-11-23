using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

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
    [SerializeField] private float betBubbleOutwardOffset = 40f;

    [Header("Карманные карты (опционально)")]
    [SerializeField] private Image hole1Image;
    [SerializeField] private Image hole2Image;
    [SerializeField] private Sprite holeCardBack;
    [SerializeField] private GameObject dealerButton;

    [Header("Размеры карт")]
    [SerializeField] private Vector2 holeCardSize = new Vector2(65f, 95f); // Размер hole карт

    [Header("Фишки ставки")]
    [SerializeField] private BetChipDisplay chipDisplay;
    [Header("Расстояния до центра")]
    [SerializeField, Tooltip("Положительное значение отодвигает карманные карты от центра (в пикселях)")]
    private float holeDistanceOffset = 40f;
    [SerializeField, Tooltip("Отрицательное значение тянет фишки ближе к игроку, положительное — к центру")]
    private float chipDistanceOffset = -70f;
    [SerializeField, Tooltip("Дополнительный поворот карманных карт в градусах")]
    private float holeRotationOffset;
    private Vector2 cachedInwardDirection = Vector2.down;
    private float cachedHoleDistance = 55f; // Увеличено для размещения карт ближе к центру стола

    private void Awake()
    {
        EnsureHoleCardBack();
        EnsureChipDisplay();
        SetHoleCardSizes();
        ShowChips(false);
    }

    private void SetHoleCardSizes()
    {
        SetImageSize(hole1Image);
        SetImageSize(hole2Image);
    }

    private void SetImageSize(Image img)
    {
        if (img == null) return;
        
        // Настраиваем Image для правильного отображения карт
        img.type = Image.Type.Simple;
        img.preserveAspect = true; // Сохраняем пропорции карт
        
        RectTransform rt = img.rectTransform;
        if (rt != null)
        {
            rt.sizeDelta = holeCardSize;
        }
    }

    private void EnsureHoleCardBack()
    {
        if (holeCardBack == null)
        {
            holeCardBack = CardSpriteProvider.GetCardBack();
        }
    }

    private void EnsureChipDisplay()
    {
        if (chipDisplay == null)
            chipDisplay = GetComponentInChildren<BetChipDisplay>(true);
        if (chipDisplay == null)
            chipDisplay = GetComponent<BetChipDisplay>();

        if (chipDisplay == null)
        {
            var anchor = BetBubbleRect;
            var parent = anchor != null ? anchor : transform as RectTransform;
            if (parent == null)
                parent = transform as RectTransform;

            var go = new GameObject("ChipDisplay", typeof(RectTransform), typeof(BetChipDisplay));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent != null ? parent : transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            chipDisplay = go.GetComponent<BetChipDisplay>();
        }

        if (chipDisplay == null)
        {
            Debug.LogWarning($"[{name}] BetChipDisplay not assigned and auto-creation failed");
            return;
        }

        if (chipDisplay != null)
        {
            if (BetBubbleRect != null)
                chipDisplay.SetAnchorTarget(BetBubbleRect);
            chipDisplay.EnsureChipSet();
            chipDisplay.InitializeRuntime();
            var seatRect = transform as RectTransform;
            if (seatRect != null)
            {
                float chipDistance = Mathf.Max(0f, cachedHoleDistance + chipDistanceOffset);
                chipDisplay.ConfigureSeatAnchor(seatRect, cachedInwardDirection, chipDistance);
            }
        }

        if (BetBubbleRect != null)
            UpdateBetBubblePosition();
    }

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

        if (avatarImage != null)
        {
            Sprite spriteToUse = avatar ?? avatarImage.sprite ?? AvatarLibrary.GetAvatarSprite("default");
            avatarImage.sprite = spriteToUse;
            avatarImage.color = Color.white;
            avatarImage.enabled = spriteToUse != null;
        }
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
        if (chipDisplay == null)
            chipDisplay = GetComponentInChildren<BetChipDisplay>(true);
        Debug.Log($"[{name}] ShowBet() chips={chips}, hasChipDisplay={(chipDisplay != null)}");
        bool show = chips > 0;
        if (betBubble != null)
            betBubble.SetActive(show);

        string valueText = chips.ToString();
            if (betTextTMP != null)
            betTextTMP.text = valueText;
            else if (betText != null)
            betText.text = valueText;

        chipDisplay?.SetAmount(chips);
        if (BetBubbleRect != null)
            UpdateBetBubblePosition();
    }

    public void ShowChips(bool show)
    {
        if (chipDisplay == null)
            chipDisplay = GetComponentInChildren<BetChipDisplay>(true);
        chipDisplay?.Show(show);
        }

    public void RepositionChipsRelativeToSeat()
    {
        chipDisplay?.Reposition();
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
        chipDisplay?.Show(false);
    }

    public RectTransform BetBubbleRect => betBubble != null ? betBubble.transform as RectTransform : null;

    public void ShowHoleBacks()
    {
        EnsureHoleCardBack();
        if (holeCardBack == null)
        {
            HideHoles();
            return;
        }

        if (hole1Image != null)
        {
            hole1Image.sprite = holeCardBack;
            hole1Image.type = Image.Type.Simple;
            hole1Image.preserveAspect = true; // Сохраняем пропорции спрайта
            hole1Image.enabled = true;
        }

        if (hole2Image != null)
        {
            hole2Image.sprite = holeCardBack;
            hole2Image.type = Image.Type.Simple;
            hole2Image.preserveAspect = true; // Сохраняем пропорции спрайта
            hole2Image.enabled = true;
        }
    }

    public void ShowHole(Card a, Card b)
    {
        EnsureHoleCardBack();
        if (hole1Image != null)
        {
            var s = CardSpriteProvider.GetSprite(a);
            hole1Image.sprite = s != null ? s : holeCardBack;
            hole1Image.type = Image.Type.Simple;
            hole1Image.preserveAspect = true; // Сохраняем пропорции спрайта
            hole1Image.enabled = true;
        }
        if (hole2Image != null)
        {
            var s = CardSpriteProvider.GetSprite(b);
            hole2Image.sprite = s != null ? s : holeCardBack;
            hole2Image.type = Image.Type.Simple;
            hole2Image.preserveAspect = true; // Сохраняем пропорции спрайта
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

    public void ConfigureHoleLayout(Vector2 inwardDirection, float rotationDeg, float distance = 55f, float spacing = 22f)
    {
        // inwardDirection должен быть нормализован и указывать К ЦЕНТРУ стола
        if (inwardDirection.sqrMagnitude < 0.0001f) inwardDirection = new Vector2(0f, -1f);
        inwardDirection.Normalize();
        cachedInwardDirection = inwardDirection;
        float adjustedDistance = Mathf.Max(0f, distance + holeDistanceOffset);
        cachedHoleDistance = adjustedDistance;
        if (chipDisplay != null)
        {
            var seatRect = transform as RectTransform;
            float chipDistance = Mathf.Max(0f, cachedHoleDistance + spacing + chipDistanceOffset);
            chipDisplay.ConfigureSeatAnchor(seatRect, cachedInwardDirection, chipDistance);
        }
        Vector2 tangent = new Vector2(-inwardDirection.y, inwardDirection.x); // перпендикуляр
        Vector2 pos1 = inwardDirection * adjustedDistance - tangent * (spacing * 0.5f);
        Vector2 pos2 = inwardDirection * adjustedDistance + tangent * (spacing * 0.5f);

        // Используем переданный поворот вместо вычисления собственного
        float correctRotation = rotationDeg + holeRotationOffset;
        
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

        UpdateBetBubblePosition();
    }

    public void SetHoleRotationOffset(float offset)
    {
        holeRotationOffset = offset;
    }

    private void UpdateBetBubblePosition()
    {
        var bubble = BetBubbleRect;
        if (bubble == null)
            return;

        bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
        bubble.pivot = new Vector2(0.5f, 0.5f);
        bubble.SetAsLastSibling();

        Vector2 outward = -cachedInwardDirection;
        Vector2 offset = outward * Mathf.Max(0f, cachedHoleDistance + betBubbleOutwardOffset);
        bubble.anchoredPosition = offset;
    }
}
