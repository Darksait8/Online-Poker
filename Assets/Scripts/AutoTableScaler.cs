using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// Автоматически подстраивает все элементы стола под его размер при старте
/// </summary>
public class AutoTableScaler : MonoBehaviour
{
    [Header("Базовые размеры стола (для расчета масштаба)")]
    [SerializeField] private float baseTableWidth = 645f;
    [SerializeField] private float baseTableHeight = 312f;
    
    [Header("Автоматическое масштабирование")]
    [SerializeField] private bool autoScaleOnStart = true;
    [SerializeField] private bool autoScaleOnEnable = false;
    
    private RectTransform tableRect;
    private SeatsLayoutRadial seatsLayout;
    private BoardController boardController;
    private float lastMultiplier = 1f;
    
    private void Awake()
    {
        FindTableAndComponents();
    }
    
    private void Start()
    {
        if (autoScaleOnStart)
        {
            ScaleAllElementsToTable();
        }
    }
    
    private void OnEnable()
    {
        if (autoScaleOnEnable)
        {
            ScaleAllElementsToTable();
        }
    }
    
    private void FindTableAndComponents()
    {
        // Находим Table Panel
        GameObject tablePanelGO = GameObject.Find("Table Panel");
        if (tablePanelGO != null)
        {
            tableRect = tablePanelGO.GetComponent<RectTransform>();
        }
        
        // Находим SeatsLayoutRadial
        seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        
        // Находим BoardController
        boardController = FindObjectOfType<BoardController>();
        
        if (tableRect == null)
        {
            Debug.LogWarning("AutoTableScaler: Table Panel не найден!");
        }
    }
    
    [ContextMenu("Масштабировать все элементы под размер стола")]
    public void ScaleAllElementsToTable()
    {
        if (tableRect == null)
        {
            FindTableAndComponents();
        }
        
        if (tableRect == null)
        {
            Debug.LogError("AutoTableScaler: Не могу найти Table Panel!");
            return;
        }
        
        // Вычисляем текущий размер стола
        Vector2 currentSize = tableRect.sizeDelta;
        
        // Вычисляем множитель масштабирования
        float scaleX = currentSize.x / baseTableWidth;
        float scaleY = currentSize.y / baseTableHeight;
        float multiplier = (scaleX + scaleY) / 2f; // Средний множитель для пропорционального масштабирования
        
        if (Mathf.Abs(multiplier - lastMultiplier) < 0.001f)
        {
            // Масштаб не изменился, ничего не делаем
            return;
        }
        
        lastMultiplier = multiplier;
        
        Debug.Log($"AutoTableScaler: Размер стола {currentSize.x:F0}x{currentSize.y:F0}, множитель: {multiplier:F2}x");
        
        // 1. Масштабируем SeatsLayoutRadial
        if (seatsLayout != null)
        {
            ScaleSeatsLayout(multiplier);
        }
        
        // 2. Масштабируем BoardController
        if (boardController != null)
        {
            ScaleBoardCards(multiplier);
        }
        
        // 3. Масштабируем карманные карты и фишки у всех мест
        ScaleSeatCardsAndChips(multiplier);
        
        Debug.Log($"AutoTableScaler: Все элементы масштабированы до {multiplier:F2}x");
    }
    
    private void ScaleSeatsLayout(float multiplier)
    {
        if (seatsLayout == null) return;
        
        // Получаем базовые значения
        float baseRadiusX = 520f;
        float baseRadiusY = 300f;
        float baseHoleDistance = 60f;
        float baseHoleSpacing = 80f;
        
        // Вычисляем новые значения
        float newRadiusX = baseRadiusX * multiplier;
        float newRadiusY = baseRadiusY * multiplier;
        float newHoleDistance = baseHoleDistance * multiplier;
        float newHoleSpacing = baseHoleSpacing * multiplier;
        
        // Устанавливаем через рефлексию
        var radiusXField = typeof(SeatsLayoutRadial).GetField("radiusX", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var radiusYField = typeof(SeatsLayoutRadial).GetField("radiusY", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var holeDistanceField = typeof(SeatsLayoutRadial).GetField("holeDistance", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var holeSpacingField = typeof(SeatsLayoutRadial).GetField("holeSpacing", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (radiusXField != null) radiusXField.SetValue(seatsLayout, newRadiusX);
        if (radiusYField != null) radiusYField.SetValue(seatsLayout, newRadiusY);
        if (holeDistanceField != null) holeDistanceField.SetValue(seatsLayout, newHoleDistance);
        if (holeSpacingField != null) holeSpacingField.SetValue(seatsLayout, newHoleSpacing);
        
        // Обновляем позиции существующих мест
        UpdateExistingSeatPositions(multiplier);
        
        Debug.Log($"AutoTableScaler: SeatsLayout масштабирован. Радиусы: {newRadiusX:F0}x{newRadiusY:F0}, Расстояния: {newHoleDistance:F0}/{newHoleSpacing:F0}");
    }
    
    private void UpdateExistingSeatPositions(float multiplier)
    {
        if (seatsLayout == null) return;
        
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        
        // Получаем параметры из SeatsLayoutRadial
        var startAngleField = typeof(SeatsLayoutRadial).GetField("startAngleDeg", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var clockwiseField = typeof(SeatsLayoutRadial).GetField("clockwise", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var holeDistanceField = typeof(SeatsLayoutRadial).GetField("holeDistance", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var holeSpacingField = typeof(SeatsLayoutRadial).GetField("holeSpacing", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        float startAngle = startAngleField != null ? (float)startAngleField.GetValue(seatsLayout) : 90f;
        bool clockwise = clockwiseField != null ? (bool)clockwiseField.GetValue(seatsLayout) : true;
        float holeDist = holeDistanceField != null ? (float)holeDistanceField.GetValue(seatsLayout) : 60f * multiplier;
        float holeSpacing = holeSpacingField != null ? (float)holeSpacingField.GetValue(seatsLayout) : 80f * multiplier;
        
        float dir = clockwise ? -1f : 1f;
        
        // Получаем tableRect для позиционирования
        var tableRectField = typeof(SeatsLayoutRadial).GetField("tableRect", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        RectTransform seatsTableRect = tableRectField != null ? (RectTransform)tableRectField.GetValue(seatsLayout) : null;
        
        if (seatsTableRect == null && tableRect != null)
        {
            seatsTableRect = tableRect;
        }
        
        // Обновляем позиции всех мест
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            string seatName = seat.name;
            int seatIndex = -1;
            if (seatName.StartsWith("Seat_"))
            {
                string indexStr = seatName.Substring(5);
                if (int.TryParse(indexStr, out int idx))
                {
                    seatIndex = idx - 1;
                }
            }
            
            if (seatIndex >= 0 && seatIndex < seatsLayout.MaxSeats && seatsTableRect != null)
            {
                // Вычисляем правильную позицию
                float t = seatIndex / (float)seatsLayout.MaxSeats;
                float angleDeg = startAngle + dir * t * 360f;
                float rad = Mathf.Deg2Rad * angleDeg;
                
                // Получаем радиусы
                var radiusXField = typeof(SeatsLayoutRadial).GetField("radiusX", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var radiusYField = typeof(SeatsLayoutRadial).GetField("radiusY", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                float radiusX = radiusXField != null ? (float)radiusXField.GetValue(seatsLayout) : 520f * multiplier;
                float radiusY = radiusYField != null ? (float)radiusYField.GetValue(seatsLayout) : 300f * multiplier;
                
                float x = Mathf.Cos(rad) * radiusX;
                float y = Mathf.Sin(rad) * radiusY;
                
                RectTransform seatRect = seat.GetComponent<RectTransform>();
                if (seatRect != null)
                {
                    seatRect.anchoredPosition = new Vector2(x, y);
                }
                
                // Обновляем позиции карт
                Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
                float rot = angleDeg + 90f;
                rot = Mathf.Repeat(rot, 360f);
                
                bool invertForTop = angleDeg > 45f && angleDeg < 135f;
                seat.SetHoleRotationOffset(invertForTop ? 180f : 0f);
                seat.ConfigureHoleLayout(inward, rot, holeDist, holeSpacing);
                
                // Обновляем позиции фишек
                seat.RepositionChipsRelativeToSeat();
            }
        }
    }
    
    private void ScaleBoardCards(float multiplier)
    {
        if (boardController == null) return;
        
        float baseCardSize = 80f;
        float baseCardSpacing = 90f;
        
        Vector2 newCardSize = new Vector2(baseCardSize, baseCardSize * 1.4f) * multiplier;
        float newCardSpacing = baseCardSpacing * multiplier;
        
        // Устанавливаем через рефлексию
        var cardSizeField = typeof(BoardController).GetField("cardSize", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var cardSpacingField = typeof(BoardController).GetField("cardSpacing", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (cardSizeField != null)
        {
            cardSizeField.SetValue(boardController, newCardSize);
        }
        
        if (cardSpacingField != null)
        {
            cardSpacingField.SetValue(boardController, newCardSpacing);
        }
        
        // Вызываем методы обновления
        var setSizeMethod = typeof(BoardController).GetMethod("SetCardSizes", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        setSizeMethod?.Invoke(boardController, null);
        
        var alignMethod = typeof(BoardController).GetMethod("AlignBoardCards", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        alignMethod?.Invoke(boardController, null);
        
        Debug.Log($"AutoTableScaler: Board карты масштабированы. Размер: {newCardSize.x:F0}x{newCardSize.y:F0}, Расстояние: {newCardSpacing:F0}");
    }
    
    private void ScaleSeatCardsAndChips(float multiplier)
    {
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        
        Vector2 baseHoleSize = new Vector2(65f, 95f);
        Vector2 scaledHoleSize = baseHoleSize * multiplier;
        
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            // Масштабируем размеры карманных карт
            var holeCardSizeField = typeof(NewBehaviourScript).GetField("holeCardSize",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (holeCardSizeField != null)
            {
                holeCardSizeField.SetValue(seat, scaledHoleSize);
                
                // Вызываем SetHoleCardSizes
                var setSizeMethod = typeof(NewBehaviourScript).GetMethod("SetHoleCardSizes",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                setSizeMethod?.Invoke(seat, null);
            }
            
            // Прямое масштабирование RectTransform Hole1 и Hole2
            Transform hole1Transform = seat.transform.Find("Hole1");
            Transform hole2Transform = seat.transform.Find("Hole2");
            
            if (hole1Transform != null)
            {
                RectTransform hole1Rect = hole1Transform.GetComponent<RectTransform>();
                if (hole1Rect != null)
                {
                    hole1Rect.sizeDelta = scaledHoleSize;
                }
            }
            if (hole2Transform != null)
            {
                RectTransform hole2Rect = hole2Transform.GetComponent<RectTransform>();
                if (hole2Rect != null)
                {
                    hole2Rect.sizeDelta = scaledHoleSize;
                }
            }
            
            // Масштабируем фишки
            var chipDisplayField = typeof(NewBehaviourScript).GetField("chipDisplay",
                BindingFlags.NonPublic | BindingFlags.Instance);
            BetChipDisplay chipDisplay = chipDisplayField != null ? (BetChipDisplay)chipDisplayField.GetValue(seat) : null;
            
            if (chipDisplay != null)
            {
                // Получаем и масштабируем размеры фишек
                var chipSizeField = typeof(BetChipDisplay).GetField("chipSize",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var spacingField = typeof(BetChipDisplay).GetField("spacing",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                Vector2 baseChipSize = new Vector2(44f, 44f);
                Vector2 baseSpacing = new Vector2(6f, 6f);
                
                if (chipSizeField != null)
                {
                    chipSizeField.SetValue(chipDisplay, baseChipSize * multiplier);
                }
                if (spacingField != null)
                {
                    spacingField.SetValue(chipDisplay, baseSpacing * multiplier);
                }
                
                // Обновляем позиции фишек
                seat.RepositionChipsRelativeToSeat();
                chipDisplay.Reposition();
            }
        }
        
        Debug.Log($"AutoTableScaler: Карты и фишки масштабированы для {seats.Length} мест");
    }
}

