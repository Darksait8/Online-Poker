using UnityEngine;
using UnityEngine.UI;

public class SeatsLayoutRadial : MonoBehaviour
{
    [Header("Настройки раскладки")]
    [SerializeField, Range(2, 9)] private int maxSeats = 9; // максимум мест на столе
    [SerializeField] private RectTransform tableRect;   // перетащи сюда RectTransform твоего стола (Table Panel)
    [SerializeField] private RectTransform seatPrefab;  // перетащи сюда префаб места (объект с NewBehaviourScript)
    [SerializeField] private float radiusX = 520f;      // радиус по X вокруг стола
    [SerializeField] private float radiusY = 300f;      // радиус по Y вокруг стола
    [SerializeField] private float startAngleDeg = 90f; // стартовый угол (90 = сверху по центру)
    [SerializeField] private bool clockwise = true;     // направление обхода по часовой
    [SerializeField] private bool snapToPixels = true;  // выравнивать позиции к пиксельной сетке

    [Header("Тестовые данные")]
    [SerializeField] private bool spawnOnStart = true;  // создать места при старте

    [Header("Карманные карты — раскладка")]
    [SerializeField] private bool applyHoleLayout = true;
    [SerializeField] private float holeDistance = 55f;     // расстояние от центра стула к центру пары карт (увеличено для размещения ближе к центру стола)
    [SerializeField] private float holeSpacing = 22f;      // расстояние между картами
    
    [Header("Автоматическая настройка размера стола")]
    [SerializeField] private bool autoConfigureTableSize = false;  // Автоматически настроить размер стола при старте
    [SerializeField] private float tableSizeMultiplier = 1.5f;     // Множитель размера стола (1.0 = базовый размер)
    [SerializeField] private float baseTableWidth = 645f;          // Базовая ширина стола
    [SerializeField] private float baseTableHeight = 312f;         // Базовая высота стола
    
    [Header("Дополнительное увеличение карт")]
    [SerializeField] private float cardSizeMultiplier = 1.5f;      // Дополнительный множитель размера карт (увеличивает ТОЛЬКО карты, стол не меняется)
    [SerializeField] private float botCardSizeMultiplier = 0.7f;   // Множитель размера карт для ботов (уменьшает карты ботов, например 0.7 = 70% от обычного размера)


    // Рантайм: текущие занятые места
    private readonly System.Collections.Generic.List<NewBehaviourScript> occupied = new System.Collections.Generic.List<NewBehaviourScript>();
    public int OccupiedCount => occupied.Count;
    public int MaxSeats => maxSeats;
    public System.Action<int> OnOccupiedChanged;

    private void Start()
    {
        Debug.Log($"SeatsLayoutRadial: TableRuntimeConfig.HasConfig = {TableRuntimeConfig.HasConfig}");
        Debug.Log($"SeatsLayoutRadial: maxSeats из инспектора = {maxSeats}");

        // Если есть конфиг из TableRuntimeConfig, используем его
        if (TableRuntimeConfig.HasConfig)
        {
            int configMaxSeats = TableRuntimeConfig.MaxSeats;
            Debug.Log($"SeatsLayoutRadial: TableRuntimeConfig.MaxSeats = {configMaxSeats}");
            
            // Проверяем, что значение из конфига разумное
            if (configMaxSeats >= 2 && configMaxSeats <= 9)
            {
                maxSeats = configMaxSeats;
            Debug.Log($"SeatsLayoutRadial: Используем maxSeats из TableRuntimeConfig = {maxSeats}");
            }
            else
            {
                Debug.LogWarning($"SeatsLayoutRadial: TableRuntimeConfig.MaxSeats = {configMaxSeats} вне диапазона [2,9], используем значение из инспектора = {maxSeats}");
            }
        }
        else
        {
            Debug.Log($"SeatsLayoutRadial: TableRuntimeConfig не настроен, используем значение из инспектора = {maxSeats}");
        }
        
        // Автоматическая настройка размера стола
        if (autoConfigureTableSize && tableSizeMultiplier != 1.0f)
        {
            // Используем базовые значения для расчета, а не текущие из инспектора
            float baseRadiusX = 520f;
            float baseRadiusY = 300f;
            float baseHoleDistance = 60f;
            float baseHoleSpacing = 80f;
            
            SetTableSizeMultiplier(tableSizeMultiplier, baseTableWidth, baseTableHeight, 
                baseRadiusX, baseRadiusY, baseHoleDistance, baseHoleSpacing, false);
        }
        if (spawnOnStart)
            SpawnSeats();
        
        // АВТОМАТИЧЕСКОЕ определение размера стола и подстройка всех элементов (после создания мест)
        if (!autoConfigureTableSize || tableSizeMultiplier == 1.0f)
        {
            AutoScaleToTableSize();
        }
        
        // ДОПОЛНИТЕЛЬНОЕ увеличение размера карт (независимо от стола)
        if (cardSizeMultiplier > 1.0f)
        {
            StartCoroutine(DelayedScaleCardsOnly(cardSizeMultiplier));
        }
    }

    [ContextMenu("Spawn Seats")]
    [UnityEngine.ContextMenu("Recreate Seats")]
    public void SpawnSeats()
    {
        if (seatPrefab == null)
        {
            Debug.LogWarning("SeatsLayoutRadial: не назначен seatPrefab");
            return;
        }

        // Удаляем старых детей, если были
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (Application.isEditor)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
        }
        occupied.Clear();
        OnOccupiedChanged?.Invoke(occupied.Count);

        float dir = clockwise ? -1f : 1f;
        for (int i = 0; i < maxSeats; i++)
        {
            float t = i / (float)maxSeats;
            float angleRad = Mathf.Deg2Rad * (startAngleDeg + dir * t * 360f);
            Vector2 p = new Vector2(Mathf.Cos(angleRad) * radiusX, Mathf.Sin(angleRad) * radiusY);

            RectTransform seat = Instantiate(seatPrefab, transform);
            seat.name = $"Seat_{i + 1}";
            seat.anchorMin = seat.anchorMax = new Vector2(0.5f, 0.5f);
            seat.pivot = new Vector2(0.5f, 0.5f);
            if (snapToPixels)
            {
                p = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
            }
            seat.anchoredPosition = p;

            // Инициализация места как свободного
            var ui = seat.GetComponent<NewBehaviourScript>();
            if (ui != null)
            {
                ui.SetPlayer("Свободно", 0);
                ui.ShowBet(0);
                ui.SetDealer(false);
                ui.HideHoles();

                // Настроим расположение и поворот карманных карт в зависимости от сектора
                if (applyHoleLayout)
                {
                    float angleDeg = startAngleDeg + dir * t * 360f;
                    float rad = Mathf.Deg2Rad * angleDeg;
                    // направление К центру стола (вектор от места к центру)
                    Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
                    
                    // Поворот карт: карты должны быть ПЕРПЕНДИКУЛЯРНЫ игроку
                    // Это означает, что карты должны быть повернуты на 90° относительно направления к центру
                    float rot = angleDeg + 90f; // Добавляем 90° чтобы карты были перпендикулярны
                    
                    // Нормализуем угол поворота
                    rot = Mathf.Repeat(rot, 360f);
                    
                    bool invertForTop = angleDeg > 45f && angleDeg < 135f;
                    ui.SetHoleRotationOffset(invertForTop ? 180f : 0f);
                    Debug.Log($"Seat_{i + 1}: angleDeg = {angleDeg:F1}°, rot = {rot:F1}°, extra={(invertForTop ? 180f : 0f)}");
                    ui.ConfigureHoleLayout(inward, rot, holeDistance, holeSpacing);
                }
            }
        }
    }

    // Найти первое свободное место (по индексу ребёнка)
    private int FindFirstFreeIndex()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var ui = transform.GetChild(i).GetComponent<NewBehaviourScript>();
            if (ui != null && !occupied.Contains(ui))
                return i;
        }
        return -1;
    }

    public bool TryJoin(string playerName, int stack)
    {
        if (occupied.Count >= maxSeats) return false;
        int idx = FindFirstFreeIndex();
        if (idx < 0) return false;
        var ui = transform.GetChild(idx).GetComponent<NewBehaviourScript>();
        if (ui == null) return false;
        ui.SetPlayer(playerName, stack);
        occupied.Add(ui);
        OnOccupiedChanged?.Invoke(occupied.Count);
        return true;
    }

    public bool Leave(string playerName)
    {
        for (int i = 0; i < occupied.Count; i++)
        {
            var ui = occupied[i];
            // сравним по имени
            // если используешь уникальные ID — заменим логикой позже
            // здесь просто очищаем место
            // (имя берём из TMP или Text)
            bool match = false;
            var nameField = ui.GetType().GetField("nameTextTMP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(ui) as TMPro.TMP_Text;
            if (nameField != null) match = nameField.text == playerName;
            if (!match)
            {
                var legacy = ui.GetType().GetField("nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(ui) as UnityEngine.UI.Text;
                if (legacy != null) match = legacy.text == playerName;
            }
            if (match)
            {
                ui.SetPlayer("Свободно", 0);
                ui.ShowBet(0);
                ui.SetDealer(false);
                occupied.RemoveAt(i);
                OnOccupiedChanged?.Invoke(occupied.Count);
                return true;
            }
        }
        return false;
    }

    // Перечень занятых мест (UI контроллеров)
    public System.Collections.Generic.IReadOnlyList<NewBehaviourScript> GetOccupiedSeats()
    {
        return occupied;
    }
    
    /// <summary>
    /// Обновляет UI конкретного игрока
    /// </summary>
    public void UpdatePlayerUI(object player)
    {
        // Простая заглушка - в реальной игре здесь должна быть логика обновления UI
        Debug.Log($"UpdatePlayerUI called for player: {player}");
    }
    
    /// <summary>
    /// Обновляет UI всех игроков
    /// </summary>
    public void UpdateAllPlayersUI()
    {
        // Простая заглушка - в реальной игре здесь должна быть логика обновления UI всех игроков
        Debug.Log("UpdateAllPlayersUI called");
    }
    
    // ========== Методы для управления размером стола и радиусами ==========
    
    /// <summary>
    /// Устанавливает размер стола
    /// </summary>
    public void SetTableSize(float width, float height)
    {
        if (tableRect == null)
        {
            Debug.LogWarning("SeatsLayoutRadial: tableRect не назначен!");
            return;
        }
        
        tableRect.sizeDelta = new Vector2(width, height);
        Debug.Log($"SeatsLayoutRadial: Установлен размер стола: {width}x{height}");
    }
    
    /// <summary>
    /// Устанавливает радиусы размещения мест
    /// </summary>
    public void SetSeatRadii(float newRadiusX, float newRadiusY)
    {
        radiusX = newRadiusX;
        radiusY = newRadiusY;
        Debug.Log($"SeatsLayoutRadial: Установлены радиусы: X={radiusX}, Y={radiusY}");
    }
    
    /// <summary>
    /// Устанавливает расстояния для карманных карт
    /// </summary>
    public void SetHoleDistances(float newHoleDistance, float newHoleSpacing)
    {
        holeDistance = newHoleDistance;
        holeSpacing = newHoleSpacing;
        Debug.Log($"SeatsLayoutRadial: Установлены расстояния для карт: distance={holeDistance}, spacing={holeSpacing}");
    }
    
    /// <summary>
    /// Устанавливает размер стола и автоматически обновляет радиусы пропорционально
    /// </summary>
    /// <param name="width">Ширина стола</param>
    /// <param name="height">Высота стола</param>
    /// <param name="baseWidth">Базовая ширина (для расчета пропорций, по умолчанию 645)</param>
    /// <param name="baseHeight">Базовая высота (для расчета пропорций, по умолчанию 312)</param>
    /// <param name="baseRadiusX">Базовый радиус X (по умолчанию 520)</param>
    /// <param name="baseRadiusY">Базовый радиус Y (по умолчанию 300)</param>
    /// <param name="baseHoleDistance">Базовое расстояние для карт (по умолчанию 60)</param>
    /// <param name="baseHoleSpacing">Базовое расстояние между картами (по умолчанию 80)</param>
    /// <param name="respawnSeats">Пересоздать места после изменения (по умолчанию true)</param>
    public void ConfigureTableSize(
        float width, 
        float height,
        float baseWidth = 645f,
        float baseHeight = 312f,
        float baseRadiusX = 520f,
        float baseRadiusY = 300f,
        float baseHoleDistance = 60f,
        float baseHoleSpacing = 80f,
        bool respawnSeats = true)
    {
        // Устанавливаем размер стола
        SetTableSize(width, height);
        
        // Вычисляем множитель масштаба
        float scaleX = width / baseWidth;
        float scaleY = height / baseHeight;
        float avgScale = (scaleX + scaleY) / 2f; // Средний множитель для пропорционального масштабирования
        
        // Обновляем радиусы пропорционально
        float newRadiusX = baseRadiusX * avgScale;
        float newRadiusY = baseRadiusY * avgScale;
        SetSeatRadii(newRadiusX, newRadiusY);
        
        // Обновляем расстояния для карт
        float newHoleDistance = baseHoleDistance * avgScale;
        float newHoleSpacing = baseHoleSpacing * avgScale;
        SetHoleDistances(newHoleDistance, newHoleSpacing);
        
        // Пересоздаем места с новыми параметрами
        if (respawnSeats)
        {
            SpawnSeats();
        }
        
        Debug.Log($"SeatsLayoutRadial: Стол настроен. Размер: {width}x{height}, Множитель: {avgScale:F2}x, Радиусы: {newRadiusX:F0}x{newRadiusY:F0}");
    }
    
    /// <summary>
    /// Автоматически определяет размер стола и подстраивает все элементы под него
    /// </summary>
    private void AutoScaleToTableSize()
    {
        if (tableRect == null) return;
        
        Vector2 currentSize = tableRect.sizeDelta;
        
        // Базовые размеры
        float baseWidth = 645f;
        float baseHeight = 312f;
        
        // Вычисляем множитель
        float scaleX = currentSize.x / baseWidth;
        float scaleY = currentSize.y / baseHeight;
        float multiplier = (scaleX + scaleY) / 2f;
        
        // Если размер отличается от базового более чем на 1%, применяем масштабирование
        if (Mathf.Abs(multiplier - 1.0f) > 0.01f)
        {
            Debug.Log($"SeatsLayoutRadial: Автоматическое масштабирование. Размер стола: {currentSize.x:F0}x{currentSize.y:F0}, множитель: {multiplier:F2}x");
            
            // Подстраиваем все элементы под размер стола
            float baseRadiusX = 520f;
            float baseRadiusY = 300f;
            float baseHoleDistance = 60f;
            float baseHoleSpacing = 80f;
            
            SetTableSizeMultiplier(multiplier, baseWidth, baseHeight, 
                baseRadiusX, baseRadiusY, baseHoleDistance, baseHoleSpacing, false);
            
            // После масштабирования подстраиваем карты и фишки
            StartCoroutine(DelayedScaleCardsAndChips(multiplier));
        }
    }
    
    private System.Collections.IEnumerator DelayedScaleCardsAndChips(float multiplier)
    {
        // Ждем кадр, чтобы все объекты успели создаться
        yield return null;
        
        ScaleAllCardsAndChips(multiplier);
    }
    
    private void ScaleAllCardsAndChips(float multiplier)
    {
        // Масштабируем карты борда
        BoardController boardController = FindObjectOfType<BoardController>();
        if (boardController != null)
        {
            var cardSizeField = typeof(BoardController).GetField("cardSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cardSpacingField = typeof(BoardController).GetField("cardSpacing", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Vector2 baseCardSize = new Vector2(80f, 112f);
            float baseCardSpacing = 90f;
            
            if (cardSizeField != null)
                cardSizeField.SetValue(boardController, baseCardSize * multiplier);
            if (cardSpacingField != null)
                cardSpacingField.SetValue(boardController, baseCardSpacing * multiplier);
            
            var setSizeMethod = typeof(BoardController).GetMethod("SetCardSizes", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            setSizeMethod?.Invoke(boardController, null);
            
            var alignMethod = typeof(BoardController).GetMethod("AlignBoardCards", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            alignMethod?.Invoke(boardController, null);
        }
        
        // Масштабируем карманные карты и фишки у всех мест
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        Vector2 baseHoleSize = new Vector2(65f, 95f);
        Vector2 scaledHoleSize = baseHoleSize * multiplier;
        
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            // Масштабируем размеры карманных карт
            var holeCardSizeField = typeof(NewBehaviourScript).GetField("holeCardSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (holeCardSizeField != null)
            {
                holeCardSizeField.SetValue(seat, scaledHoleSize);
                
                var setSizeMethod = typeof(NewBehaviourScript).GetMethod("SetHoleCardSizes",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                setSizeMethod?.Invoke(seat, null);
            }
            
            // Прямое масштабирование RectTransform
            Transform hole1Transform = seat.transform.Find("Hole1");
            Transform hole2Transform = seat.transform.Find("Hole2");
            
            if (hole1Transform != null)
            {
                RectTransform hole1Rect = hole1Transform.GetComponent<RectTransform>();
                if (hole1Rect != null) hole1Rect.sizeDelta = scaledHoleSize;
            }
            if (hole2Transform != null)
            {
                RectTransform hole2Rect = hole2Transform.GetComponent<RectTransform>();
                if (hole2Rect != null) hole2Rect.sizeDelta = scaledHoleSize;
            }
            
            // Перепозиционируем карты с новыми параметрами
            float t = 0f;
            string seatName = seat.name;
            if (seatName.StartsWith("Seat_"))
            {
                string indexStr = seatName.Substring(5);
                if (int.TryParse(indexStr, out int idx) && idx >= 1 && idx <= maxSeats)
                {
                    t = (idx - 1) / (float)maxSeats;
                }
            }
            
            float angleDeg = startAngleDeg + (clockwise ? -1f : 1f) * t * 360f;
            float rad = Mathf.Deg2Rad * angleDeg;
            Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
            float rot = angleDeg + 90f;
            rot = Mathf.Repeat(rot, 360f);
            
            bool invertForTop = angleDeg > 45f && angleDeg < 135f;
            seat.SetHoleRotationOffset(invertForTop ? 180f : 0f);
            seat.ConfigureHoleLayout(inward, rot, holeDistance, holeSpacing);
            
            // Масштабируем фишки
            var chipDisplayField = typeof(NewBehaviourScript).GetField("chipDisplay",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            BetChipDisplay chipDisplay = chipDisplayField != null ? (BetChipDisplay)chipDisplayField.GetValue(seat) : null;
            
            if (chipDisplay != null)
            {
                var chipSizeField = typeof(BetChipDisplay).GetField("chipSize",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var spacingField = typeof(BetChipDisplay).GetField("spacing",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Vector2 baseChipSize = new Vector2(44f, 44f);
                Vector2 baseSpacing = new Vector2(6f, 6f);
                
                if (chipSizeField != null)
                    chipSizeField.SetValue(chipDisplay, baseChipSize * multiplier);
                if (spacingField != null)
                    spacingField.SetValue(chipDisplay, baseSpacing * multiplier);
                
                seat.RepositionChipsRelativeToSeat();
                chipDisplay.Reposition();
            }
        }
        
        Debug.Log($"SeatsLayoutRadial: Все элементы масштабированы под размер стола (множитель: {multiplier:F2}x)");
    }
    
    /// <summary>
    /// Устанавливает размер стола с множителем (например, 1.5 для увеличения на 50%)
    /// </summary>
    public void SetTableSizeMultiplier(
        float multiplier,
        float baseWidth = 645f,
        float baseHeight = 312f,
        float baseRadiusX = 520f,
        float baseRadiusY = 300f,
        float baseHoleDistance = 60f,
        float baseHoleSpacing = 80f,
        bool respawnSeats = true)
    {
        float newWidth = baseWidth * multiplier;
        float newHeight = baseHeight * multiplier;
        
        ConfigureTableSize(
            newWidth, 
            newHeight,
            baseWidth,
            baseHeight,
            baseRadiusX,
            baseRadiusY,
            baseHoleDistance,
            baseHoleSpacing,
            respawnSeats
        );
    }
    
    private System.Collections.IEnumerator DelayedScaleCardsOnly(float multiplier)
    {
        // Ждем несколько кадров, чтобы все объекты успели создаться и настроиться
        yield return null;
        yield return null;
        
        ScaleCardsOnly(multiplier);
    }
    
    private void ScaleCardsOnly(float multiplier)
    {
        // Увеличиваем карты борда
        BoardController boardController = FindObjectOfType<BoardController>();
        if (boardController != null)
        {
            var cardSizeField = typeof(BoardController).GetField("cardSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cardSpacingField = typeof(BoardController).GetField("cardSpacing",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (cardSizeField != null && cardSpacingField != null)
            {
                Vector2 currentSize = (Vector2)cardSizeField.GetValue(boardController);
                float currentSpacing = (float)cardSpacingField.GetValue(boardController);
                
                Vector2 newSize = currentSize * multiplier;
                float newSpacing = currentSpacing * multiplier;
                
                cardSizeField.SetValue(boardController, newSize);
                cardSpacingField.SetValue(boardController, newSpacing);
                
                var setSizeMethod = typeof(BoardController).GetMethod("SetCardSizes",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                setSizeMethod?.Invoke(boardController, null);
                
                var alignMethod = typeof(BoardController).GetMethod("AlignBoardCards",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                alignMethod?.Invoke(boardController, null);
                
                Debug.Log($"SeatsLayoutRadial: Карты борда дополнительно увеличены до {newSize.x:F0}x{newSize.y:F0}");
            }
        }
        
        // Увеличиваем карманные карты (с учетом ботов)
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        int scaledCount = 0;
        int botScaledCount = 0;
        
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            // Определяем, является ли игрок ботом
            bool isBot = IsBotPlayer(seat);
            float finalMultiplier = isBot ? multiplier * botCardSizeMultiplier : multiplier;
            
            var holeCardSizeField = typeof(NewBehaviourScript).GetField("holeCardSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (holeCardSizeField != null)
            {
                Vector2 currentSize = (Vector2)holeCardSizeField.GetValue(seat);
                Vector2 newSize = currentSize * finalMultiplier;
                
                holeCardSizeField.SetValue(seat, newSize);
                
                var setSizeMethod = typeof(NewBehaviourScript).GetMethod("SetHoleCardSizes",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                setSizeMethod?.Invoke(seat, null);
                
                // Прямо обновляем RectTransform
                Transform hole1Transform = seat.transform.Find("Hole1");
                Transform hole2Transform = seat.transform.Find("Hole2");
                
                if (hole1Transform != null)
                {
                    RectTransform hole1Rect = hole1Transform.GetComponent<RectTransform>();
                    if (hole1Rect != null) hole1Rect.sizeDelta = newSize;
                    
                    var hole1ImageField = typeof(NewBehaviourScript).GetField("hole1Image",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Image hole1 = hole1ImageField != null ? (Image)hole1ImageField.GetValue(seat) : null;
                    if (hole1 != null && hole1.rectTransform != null)
                        hole1.rectTransform.sizeDelta = newSize;
                }
                
                if (hole2Transform != null)
                {
                    RectTransform hole2Rect = hole2Transform.GetComponent<RectTransform>();
                    if (hole2Rect != null) hole2Rect.sizeDelta = newSize;
                    
                    var hole2ImageField = typeof(NewBehaviourScript).GetField("hole2Image",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Image hole2 = hole2ImageField != null ? (Image)hole2ImageField.GetValue(seat) : null;
                    if (hole2 != null && hole2.rectTransform != null)
                        hole2.rectTransform.sizeDelta = newSize;
                }
                
                if (isBot)
                    botScaledCount++;
                else
                    scaledCount++;
            }
        }
        
        Debug.Log($"SeatsLayoutRadial: Карманные карты масштабированы - {scaledCount} игроков (множитель: {multiplier:F2}x), {botScaledCount} ботов (множитель: {multiplier * botCardSizeMultiplier:F2}x)");
    }
    
    /// <summary>
    /// Определяет, является ли место ботом (по имени игрока)
    /// </summary>
    private bool IsBotPlayer(NewBehaviourScript seat)
    {
        if (seat == null) return false;
        
        // Получаем имя игрока через рефлексию
        var nameTextTMPField = typeof(NewBehaviourScript).GetField("nameTextTMP",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameTextField = typeof(NewBehaviourScript).GetField("nameText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        string playerName = "";
        
        // Пытаемся получить имя из TMP_Text
        if (nameTextTMPField != null)
        {
            TMPro.TMP_Text nameTMP = nameTextTMPField.GetValue(seat) as TMPro.TMP_Text;
            if (nameTMP != null && !string.IsNullOrEmpty(nameTMP.text))
            {
                playerName = nameTMP.text;
            }
        }
        
        // Если не получилось, пытаемся из обычного Text
        if (string.IsNullOrEmpty(playerName) && nameTextField != null)
        {
            UnityEngine.UI.Text nameText = nameTextField.GetValue(seat) as UnityEngine.UI.Text;
            if (nameText != null && !string.IsNullOrEmpty(nameText.text))
            {
                playerName = nameText.text;
            }
        }
        
        // Проверяем, начинается ли имя с "Бот" (для русской локализации)
        if (!string.IsNullOrEmpty(playerName))
        {
            return playerName.StartsWith("Бот", System.StringComparison.OrdinalIgnoreCase) ||
                   playerName.StartsWith("Bot", System.StringComparison.OrdinalIgnoreCase);
        }
        
        return false;
    }
}


