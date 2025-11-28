using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Инструмент для автоматического масштабирования всех элементов стола
/// Tools > Auto Scale Table - автоматически подстраивает все под размер стола
/// </summary>
public class AutoScaleTableTool : EditorWindow
{
    private class SeatFullState
    {
        public bool hole1Visible;
        public bool hole2Visible;
        public Sprite hole1Sprite;
        public Sprite hole2Sprite;
        public string playerName;
        public string stackText;
        public Sprite avatarSprite;
        public bool betBubbleActive;
        public int currentBetAmount;
    }
    private float tableSizeMultiplier = 1.5f;
    private float baseTableWidth = 645f;
    private float baseTableHeight = 312f;
    
    [MenuItem("Tools/Auto Scale Table")]
    public static void ShowWindow()
    {
        GetWindow<AutoScaleTableTool>("Auto Scale Table");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Автоматическое масштабирование элементов стола", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "Этот инструмент автоматически подстраивает все элементы под масштаб стола:\n" +
            "• Размер стола\n" +
            "• Радиусы размещения мест\n" +
            "• Размеры карманных карт\n" +
            "• Размеры общих карт (флоп, терн, ривер)\n" +
            "• Размеры фишек\n" +
            "• Расстояния между элементами",
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        tableSizeMultiplier = EditorGUILayout.Slider("Множитель размера", tableSizeMultiplier, 0.5f, 3.0f);
        baseTableWidth = EditorGUILayout.FloatField("Базовая ширина стола", baseTableWidth);
        baseTableHeight = EditorGUILayout.FloatField("Базовая высота стола", baseTableHeight);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Применить масштабирование", GUILayout.Height(40)))
        {
            ApplyScaling();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Сбросить к базовым размерам"))
        {
            ApplyScaling(1.0f);
        }
    }
    
    private void ApplyScaling(float? customMultiplier = null)
    {
        float multiplier = customMultiplier ?? tableSizeMultiplier;
        
        if (EditorUtility.DisplayDialog(
            "Подтверждение",
            $"Применить масштабирование {multiplier:F2}x ко всем элементам стола?",
            "Да", "Отмена"))
        {
            ScaleAllElements(multiplier);
        }
    }
    
    private void ScaleAllElements(float multiplier)
    {
        Undo.SetCurrentGroupName($"Auto Scale Table {multiplier:F2}x");
        int undoGroup = Undo.GetCurrentGroup();
        
        try
        {
            // 1. Находим стол и масштабируем его
            RectTransform tablePanel = FindTablePanel();
            if (tablePanel != null)
            {
                float newWidth = baseTableWidth * multiplier;
                float newHeight = baseTableHeight * multiplier;
                
                Undo.RecordObject(tablePanel, "Scale Table Panel");
                tablePanel.sizeDelta = new Vector2(newWidth, newHeight);
                EditorUtility.SetDirty(tablePanel);
                Debug.Log($"✓ Стол масштабирован: {newWidth:F0}x{newHeight:F0}");
            }
            
            // 2. Сохраняем полное состояние всех мест (игроки, карты, фишки) перед изменением
            NewBehaviourScript[] allExistingSeats = FindObjectsOfType<NewBehaviourScript>();
            Dictionary<string, SeatFullState> fullSeatStates = new Dictionary<string, SeatFullState>();
            
            foreach (NewBehaviourScript seat in allExistingSeats)
            {
                if (seat == null) continue;
                
                string seatName = seat.name;
                SeatFullState state = new SeatFullState();
                
                // Сохраняем состояние карт
                var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
                Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
                
                state.hole1Visible = hole1 != null && hole1.enabled;
                state.hole2Visible = hole2 != null && hole2.enabled;
                state.hole1Sprite = hole1 != null ? hole1.sprite : null;
                state.hole2Sprite = hole2 != null ? hole2.sprite : null;
                
                // Сохраняем состояние игрока
                var nameTextField = typeof(NewBehaviourScript).GetField("nameText", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var nameTextTMPField = typeof(NewBehaviourScript).GetField("nameTextTMP", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var stackTextField = typeof(NewBehaviourScript).GetField("stackText", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var stackTextTMPField = typeof(NewBehaviourScript).GetField("stackTextTMP", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var avatarField = typeof(NewBehaviourScript).GetField("avatarImage", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Text nameText = nameTextField != null ? (Text)nameTextField.GetValue(seat) : null;
                TMPro.TMP_Text nameTextTMP = nameTextTMPField != null ? (TMPro.TMP_Text)nameTextTMPField.GetValue(seat) : null;
                Text stackText = stackTextField != null ? (Text)stackTextField.GetValue(seat) : null;
                TMPro.TMP_Text stackTextTMP = stackTextTMPField != null ? (TMPro.TMP_Text)stackTextTMPField.GetValue(seat) : null;
                Image avatarImg = avatarField != null ? (Image)avatarField.GetValue(seat) : null;
                
                state.playerName = nameTextTMP != null ? nameTextTMP.text : (nameText != null ? nameText.text : "");
                state.stackText = stackTextTMP != null ? stackTextTMP.text : (stackText != null ? stackText.text : "");
                state.avatarSprite = avatarImg != null ? avatarImg.sprite : null;
                
                // Сохраняем состояние фишек и ставок
                var betBubbleField = typeof(NewBehaviourScript).GetField("betBubble", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var betTextField = typeof(NewBehaviourScript).GetField("betText", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var betTextTMPField = typeof(NewBehaviourScript).GetField("betTextTMP", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var chipDisplayField = typeof(NewBehaviourScript).GetField("chipDisplay", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                GameObject betBubble = betBubbleField != null ? (GameObject)betBubbleField.GetValue(seat) : null;
                BetChipDisplay chipDisplay = chipDisplayField != null ? (BetChipDisplay)chipDisplayField.GetValue(seat) : null;
                
                state.betBubbleActive = betBubble != null && betBubble.activeSelf;
                // Получаем currentAmount через рефлексию
                if (chipDisplay != null)
                {
                    var currentAmountField = typeof(BetChipDisplay).GetField("currentAmount", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    state.currentBetAmount = currentAmountField != null ? (int)currentAmountField.GetValue(chipDisplay) : 0;
                }
                else
                {
                    state.currentBetAmount = 0;
                }
                
                fullSeatStates[seatName] = state;
            }
            
            // 4. Масштабируем радиусы, но НЕ пересоздаем места
            SeatsLayoutRadial seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
            if (seatsLayout != null)
            {
                Undo.RecordObject(seatsLayout, "Scale Seats Layout");
                
                float baseRadiusX = 520f;
                float baseRadiusY = 300f;
                float baseHoleDistance = 60f;
                float baseHoleSpacing = 80f;
                
                seatsLayout.SetSeatRadii(baseRadiusX * multiplier, baseRadiusY * multiplier);
                seatsLayout.SetHoleDistances(baseHoleDistance * multiplier, baseHoleSpacing * multiplier);
                
                EditorUtility.SetDirty(seatsLayout);
                
                Debug.Log($"✓ Радиусы мест обновлены: X={baseRadiusX * multiplier:F0}, Y={baseRadiusY * multiplier:F0}");
                Debug.Log($"✓ Расстояния карт обновлены: distance={baseHoleDistance * multiplier:F0}, spacing={baseHoleSpacing * multiplier:F0}");
            }
            
            // 3. Сохраняем состояние общих карт перед масштабированием
            BoardController boardController = FindObjectOfType<BoardController>();
            bool[] boardCardsVisible = new bool[5];
            Sprite[] boardCardSprites = new Sprite[5];
            
            if (boardController != null)
            {
                var flop1Field = typeof(BoardController).GetField("flop1", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var flop2Field = typeof(BoardController).GetField("flop2", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var flop3Field = typeof(BoardController).GetField("flop3", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var turnField = typeof(BoardController).GetField("turn", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var riverField = typeof(BoardController).GetField("river", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Image[] boardImages = new Image[5];
                if (flop1Field != null) boardImages[0] = (Image)flop1Field.GetValue(boardController);
                if (flop2Field != null) boardImages[1] = (Image)flop2Field.GetValue(boardController);
                if (flop3Field != null) boardImages[2] = (Image)flop3Field.GetValue(boardController);
                if (turnField != null) boardImages[3] = (Image)turnField.GetValue(boardController);
                if (riverField != null) boardImages[4] = (Image)riverField.GetValue(boardController);
                
                for (int i = 0; i < 5; i++)
                {
                    if (boardImages[i] != null)
                    {
                        boardCardsVisible[i] = boardImages[i].enabled;
                        boardCardSprites[i] = boardImages[i].sprite;
                    }
                }
                
                Undo.RecordObject(boardController, "Scale Board Cards");
                
                float baseCardSize = 80f;
                float baseCardSpacing = 90f;
                
                // Доступ к полям через рефлексию (они SerializeField)
                var cardSizeField = typeof(BoardController).GetField("cardSize", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cardSpacingField = typeof(BoardController).GetField("cardSpacing", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Vector2 newCardSize = new Vector2(baseCardSize, baseCardSize * 1.4f) * multiplier;
                float newCardSpacing = baseCardSpacing * multiplier;
                
                if (cardSizeField != null)
                {
                    cardSizeField.SetValue(boardController, newCardSize);
                }
                
                if (cardSpacingField != null)
                {
                    cardSpacingField.SetValue(boardController, newCardSpacing);
                }
                
                // Вызываем методы для обновления карт
                var setSizeMethod = typeof(BoardController).GetMethod("SetCardSizes", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                setSizeMethod?.Invoke(boardController, null);
                
                var alignMethod = typeof(BoardController).GetMethod("AlignBoardCards", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                alignMethod?.Invoke(boardController, null);
                
                // Восстанавливаем видимость и спрайты карт
                for (int i = 0; i < 5; i++)
                {
                    if (boardImages[i] != null)
                    {
                        Undo.RecordObject(boardImages[i], "Restore Board Card");
                        boardImages[i].enabled = boardCardsVisible[i];
                        if (boardCardSprites[i] != null)
                        {
                            boardImages[i].sprite = boardCardSprites[i];
                        }
                        EditorUtility.SetDirty(boardImages[i]);
                    }
                }
                
                EditorUtility.SetDirty(boardController);
                
                Debug.Log($"✓ Общие карты масштабированы: размер={newCardSize.x:F0}x{newCardSize.y:F0}, расстояние={newCardSpacing:F0}");
            }
            
            // 4. Обновляем позиции существующих мест
            UpdateExistingSeatPositions(seatsLayout, multiplier);
            
            // 5. СНАЧАЛА убеждаемся, что Hole1 и Hole2 существуют во всех местах
            EnsureHoleCardsExist();
            
            // 6. Масштабируем размеры мест (префабы) и карманные карты
            ScaleSeatPrefabs(multiplier);
            
            // 6. Масштабируем фишки
            ScaleChips(multiplier);
            
            // 7. Восстанавливаем полное состояние всех мест (включая карты и фишки)
            RestoreFullSeatStates(fullSeatStates, seatsLayout, multiplier);
            
            // 8. Финальная проверка и исправление всех элементов
            FinalizeSeatRestoration(fullSeatStates, seatsLayout, multiplier);
            
            // 9. РАДИКАЛЬНАЯ ФИНАЛЬНАЯ ПРОВЕРКА: Принудительно показываем карты у ВСЕХ игроков
            ForceShowCardsForAllPlayers(fullSeatStates, seatsLayout, multiplier);
            
            // 10. ФИНАЛЬНОЕ МАСШТАБИРОВАНИЕ КАРТ: Убеждаемся, что все карты масштабированы правильно
            FinalizeCardScaling(multiplier);
            
            // 11. ФИНАЛЬНОЕ ПЕРЕПОЗИЦИОНИРОВАНИЕ КАРТ: Убеждаемся, что все карты правильно расположены
            FinalizeCardPositions(seatsLayout, multiplier);
            
            Undo.CollapseUndoOperations(undoGroup);
            
            EditorUtility.DisplayDialog("Готово", 
                $"Масштабирование {multiplier:F2}x применено ко всем элементам стола!", 
                "ОК");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при масштабировании: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Ошибка", 
                $"Произошла ошибка при масштабировании:\n{e.Message}", 
                "ОК");
        }
    }
    
    private RectTransform FindTablePanel()
    {
        GameObject tablePanelGO = GameObject.Find("Table Panel");
        if (tablePanelGO != null)
        {
            return tablePanelGO.GetComponent<RectTransform>();
        }
        
        // Ищем в сцене
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>();
            foreach (RectTransform rect in rects)
            {
                if (rect.name == "Table Panel")
                {
                    return rect;
                }
            }
        }
        
        return null;
    }
    
    private void ScaleSeatPrefabs(float multiplier)
    {
        // Масштабируем все существующие места в сцене
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        int scaledCount = 0;
        
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            RectTransform seatRect = seat.GetComponent<RectTransform>();
            if (seatRect != null)
            {
                Undo.RecordObject(seatRect, "Scale Seat");
                
                float baseSeatWidth = 160f;
                float baseSeatHeight = 110f;
                
                seatRect.sizeDelta = new Vector2(
                    baseSeatWidth * multiplier,
                    baseSeatHeight * multiplier);
                
                // Масштабируем размеры карманных карт
                var holeCardSizeField = typeof(NewBehaviourScript).GetField("holeCardSize", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Vector2 baseHoleSize = new Vector2(65f, 95f);
                Vector2 newHoleSize = baseHoleSize * multiplier;
                
                if (holeCardSizeField != null)
                {
                    holeCardSizeField.SetValue(seat, newHoleSize);
                    
                    // Вызываем SetHoleCardSizes для применения изменений
                    var setSizeMethod = typeof(NewBehaviourScript).GetMethod("SetHoleCardSizes", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    setSizeMethod?.Invoke(seat, null);
                }
                
                // ПРЯМО масштабируем RectTransform карт
                var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
                Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
                
                // Если Image не привязаны, ищем в иерархии
                if (hole1 == null)
                {
                    Transform hole1Transform = seat.transform.Find("Hole1");
                    if (hole1Transform != null)
                        hole1 = hole1Transform.GetComponent<Image>();
                }
                if (hole2 == null)
                {
                    Transform hole2Transform = seat.transform.Find("Hole2");
                    if (hole2Transform != null)
                        hole2 = hole2Transform.GetComponent<Image>();
                }
                
                // Прямо масштабируем размеры карт
                if (hole1 != null)
                {
                    RectTransform hole1Rect = hole1.rectTransform;
                    if (hole1Rect != null)
                    {
                        Undo.RecordObject(hole1Rect, "Scale Hole1 Size");
                        hole1Rect.sizeDelta = newHoleSize;
                        EditorUtility.SetDirty(hole1Rect);
                    }
                }
                
                if (hole2 != null)
                {
                    RectTransform hole2Rect = hole2.rectTransform;
                    if (hole2Rect != null)
                    {
                        Undo.RecordObject(hole2Rect, "Scale Hole2 Size");
                        hole2Rect.sizeDelta = newHoleSize;
                        EditorUtility.SetDirty(hole2Rect);
                    }
                }
                
                // Масштабируем дочерние элементы (кроме карт, они уже обработаны)
                ScaleRectTransformChildren(seatRect, multiplier, skipHoleCards: true);
                
                EditorUtility.SetDirty(seat);
                scaledCount++;
            }
        }
        
        Debug.Log($"✓ Места и карманные карты масштабированы (обработано {scaledCount})");
    }
    
    private void ScaleRectTransformChildren(Transform parent, float multiplier, bool skipHoleCards = false)
    {
        foreach (Transform child in parent)
        {
            // Пропускаем карманные карты, если указано
            if (skipHoleCards && (child.name == "Hole1" || child.name == "Hole2"))
            {
                continue;
            }
            
            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                Undo.RecordObject(childRect, "Scale Child");
                
                // Масштабируем размер
                if (childRect.sizeDelta != Vector2.zero)
                {
                    childRect.sizeDelta = childRect.sizeDelta * multiplier;
                }
                
                // Масштабируем позицию только если не используется anchors
                if (childRect.anchorMin == childRect.anchorMax)
                {
                    childRect.anchoredPosition = childRect.anchoredPosition * multiplier;
                }
            }
            
            // Рекурсивно обрабатываем детей
            if (child.childCount > 0)
            {
                ScaleRectTransformChildren(child, multiplier, skipHoleCards);
            }
        }
    }
    
    private void ScaleChips(float multiplier)
    {
        BetChipDisplay[] chipDisplays = FindObjectsOfType<BetChipDisplay>();
        int scaledCount = 0;
        
        foreach (BetChipDisplay chipDisplay in chipDisplays)
        {
            if (chipDisplay == null) continue;
            
            Undo.RecordObject(chipDisplay, "Scale Chip Display");
            
            var chipSizeField = typeof(BetChipDisplay).GetField("chipSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spacingField = typeof(BetChipDisplay).GetField("spacing", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var offsetField = typeof(BetChipDisplay).GetField("offset", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (chipSizeField != null)
            {
                Vector2 currentSize = (Vector2)chipSizeField.GetValue(chipDisplay);
                chipSizeField.SetValue(chipDisplay, currentSize * multiplier);
            }
            
            if (spacingField != null)
            {
                Vector2 currentSpacing = (Vector2)spacingField.GetValue(chipDisplay);
                spacingField.SetValue(chipDisplay, currentSpacing * multiplier);
            }
            
            if (offsetField != null)
            {
                Vector2 currentOffset = (Vector2)offsetField.GetValue(chipDisplay);
                offsetField.SetValue(chipDisplay, currentOffset * multiplier);
            }
            
            EditorUtility.SetDirty(chipDisplay);
            scaledCount++;
        }
        
        Debug.Log($"✓ Фишки масштабированы (обработано {scaledCount})");
    }
    
    private void UpdateChipPositions()
    {
        // Обновляем позиции фишек после масштабирования
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        SeatsLayoutRadial seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        int updatedCount = 0;
        
        // Получаем параметры для вычисления направлений
        float startAngle = 90f;
        bool clockwise = true;
        
        if (seatsLayout != null)
        {
            var startAngleField = typeof(SeatsLayoutRadial).GetField("startAngleDeg", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var clockwiseField = typeof(SeatsLayoutRadial).GetField("clockwise", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (startAngleField != null) startAngle = (float)startAngleField.GetValue(seatsLayout);
            if (clockwiseField != null) clockwise = (bool)clockwiseField.GetValue(seatsLayout);
        }
        
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            // Вычисляем направление к центру для этого места
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
            
            if (seatIndex >= 0 && seatsLayout != null && seatIndex < seatsLayout.MaxSeats)
            {
                float dir = clockwise ? -1f : 1f;
                float t = seatIndex / (float)seatsLayout.MaxSeats;
                float angleDeg = startAngle + dir * t * 360f;
                float rad = Mathf.Deg2Rad * angleDeg;
                Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
                
                // Обновляем позиции фишек через ConfigureHoleLayout (он также обновляет фишки)
                var holeDistanceField = typeof(SeatsLayoutRadial).GetField("holeDistance", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var holeSpacingField = typeof(SeatsLayoutRadial).GetField("holeSpacing", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                float holeDist = holeDistanceField != null ? (float)holeDistanceField.GetValue(seatsLayout) : 60f;
                float holeSpacing = holeSpacingField != null ? (float)holeSpacingField.GetValue(seatsLayout) : 80f;
                float rot = angleDeg + 90f;
                rot = Mathf.Repeat(rot, 360f);
                
                // ConfigureHoleLayout также обновляет позиции фишек
                seat.ConfigureHoleLayout(inward, rot, holeDist, holeSpacing);
            }
            
            // Дополнительно вызываем RepositionChipsRelativeToSeat
            seat.RepositionChipsRelativeToSeat();
            updatedCount++;
        }
        
        Debug.Log($"✓ Позиции фишек обновлены (обработано {updatedCount})");
    }
    
    private void EnsureCardsVisible()
    {
        // Убеждаемся, что общие карты видны (если они были показаны)
        BoardController boardController = FindObjectOfType<BoardController>();
        if (boardController != null)
        {
            // Проверяем, есть ли карты на столе
            var flop1Field = typeof(BoardController).GetField("flop1", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var flop2Field = typeof(BoardController).GetField("flop2", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var flop3Field = typeof(BoardController).GetField("flop3", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var turnField = typeof(BoardController).GetField("turn", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var riverField = typeof(BoardController).GetField("river", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Image[] boardImages = new Image[5];
            if (flop1Field != null) boardImages[0] = (Image)flop1Field.GetValue(boardController);
            if (flop2Field != null) boardImages[1] = (Image)flop2Field.GetValue(boardController);
            if (flop3Field != null) boardImages[2] = (Image)flop3Field.GetValue(boardController);
            if (turnField != null) boardImages[3] = (Image)turnField.GetValue(boardController);
            if (riverField != null) boardImages[4] = (Image)riverField.GetValue(boardController);
            
            // Если карта имеет спрайт, убеждаемся что она включена
            foreach (Image img in boardImages)
            {
                if (img != null && img.sprite != null)
                {
                    Undo.RecordObject(img, "Ensure Card Visible");
                    img.enabled = true;
                    EditorUtility.SetDirty(img);
                }
            }
        }
        
        // Убеждаемся, что карманные карты видны (если они были показаны)
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        int checkedCount = 0;
        
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
            Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
            
            if (hole1 != null && hole1.sprite != null)
            {
                Undo.RecordObject(hole1, "Ensure Hole1 Visible");
                hole1.enabled = true;
                EditorUtility.SetDirty(hole1);
            }
            
            if (hole2 != null && hole2.sprite != null)
            {
                Undo.RecordObject(hole2, "Ensure Hole2 Visible");
                hole2.enabled = true;
                EditorUtility.SetDirty(hole2);
            }
            
            checkedCount++;
        }
        
        Debug.Log($"✓ Проверено {checkedCount} мест на видимость карт");
        
        Debug.Log($"✓ Видимость карт проверена");
    }
    
    private void UpdateExistingSeatPositions(SeatsLayoutRadial seatsLayout, float multiplier)
    {
        if (seatsLayout == null) return;
        
        // Получаем параметры через рефлексию
        var radiusXField = typeof(SeatsLayoutRadial).GetField("radiusX", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var radiusYField = typeof(SeatsLayoutRadial).GetField("radiusY", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var startAngleField = typeof(SeatsLayoutRadial).GetField("startAngleDeg", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var clockwiseField = typeof(SeatsLayoutRadial).GetField("clockwise", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var snapToPixelsField = typeof(SeatsLayoutRadial).GetField("snapToPixels", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        float radiusX = radiusXField != null ? (float)radiusXField.GetValue(seatsLayout) : 520f * multiplier;
        float radiusY = radiusYField != null ? (float)radiusYField.GetValue(seatsLayout) : 300f * multiplier;
        float startAngle = startAngleField != null ? (float)startAngleField.GetValue(seatsLayout) : 90f;
        bool clockwise = clockwiseField != null ? (bool)clockwiseField.GetValue(seatsLayout) : true;
        bool snapToPixels = snapToPixelsField != null ? (bool)snapToPixelsField.GetValue(seatsLayout) : true;
        
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        float dir = clockwise ? -1f : 1f;
        
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
            
            if (seatIndex >= 0 && seatIndex < seatsLayout.MaxSeats)
            {
                RectTransform seatRect = seat.GetComponent<RectTransform>();
                if (seatRect != null)
                {
                    float t = seatIndex / (float)seatsLayout.MaxSeats;
                    float angleRad = Mathf.Deg2Rad * (startAngle + dir * t * 360f);
                    Vector2 p = new Vector2(Mathf.Cos(angleRad) * radiusX, Mathf.Sin(angleRad) * radiusY);
                    
                    if (snapToPixels)
                    {
                        p = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
                    }
                    
                    Undo.RecordObject(seatRect, "Update Seat Position");
                    seatRect.anchoredPosition = p;
                    EditorUtility.SetDirty(seatRect);
                }
            }
        }
        
        Debug.Log($"✓ Позиции мест обновлены (обработано {seats.Length})");
    }
    
    private void RestoreFullSeatStates(Dictionary<string, SeatFullState> fullSeatStates, SeatsLayoutRadial seatsLayout, float multiplier)
    {
        if (seatsLayout == null) return;
        
        // Получаем параметры для ConfigureHoleLayout
        var startAngleField = typeof(SeatsLayoutRadial).GetField("startAngleDeg", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var clockwiseField = typeof(SeatsLayoutRadial).GetField("clockwise", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var holeDistanceField = typeof(SeatsLayoutRadial).GetField("holeDistance", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var holeSpacingField = typeof(SeatsLayoutRadial).GetField("holeSpacing", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        float startAngle = startAngleField != null ? (float)startAngleField.GetValue(seatsLayout) : 90f;
        bool clockwise = clockwiseField != null ? (bool)clockwiseField.GetValue(seatsLayout) : true;
        float holeDist = holeDistanceField != null ? (float)holeDistanceField.GetValue(seatsLayout) : 60f * multiplier;
        float holeSpacing = holeSpacingField != null ? (float)holeSpacingField.GetValue(seatsLayout) : 80f * multiplier;
        
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        float dir = clockwise ? -1f : 1f;
        int restoredCount = 0;
        
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            string seatName = seat.name;
            if (!fullSeatStates.TryGetValue(seatName, out SeatFullState state)) continue;
            
            // Вычисляем параметры для ConfigureHoleLayout
            int seatIndex = -1;
            if (seatName.StartsWith("Seat_"))
            {
                string indexStr = seatName.Substring(5);
                if (int.TryParse(indexStr, out int idx))
                {
                    seatIndex = idx - 1;
                }
            }
            
            if (seatIndex >= 0 && seatIndex < seatsLayout.MaxSeats)
            {
                float t = seatIndex / (float)seatsLayout.MaxSeats;
                float angleDeg = startAngle + dir * t * 360f;
                float rad = Mathf.Deg2Rad * angleDeg;
                Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
                float rot = angleDeg + 90f;
                rot = Mathf.Repeat(rot, 360f);
                
                bool invertForTop = angleDeg > 45f && angleDeg < 135f;
                seat.SetHoleRotationOffset(invertForTop ? 180f : 0f);
                
                Undo.RecordObject(seat, "Restore Seat State");
                
                // СНАЧАЛА восстанавливаем карты (чтобы они были доступны)
                var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
                Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
                
                // Проверяем, есть ли игрок в этом месте
                bool hasPlayer = !string.IsNullOrEmpty(state.playerName) && state.playerName != "Свободно";
                
                // Если есть игрок, ОБЯЗАТЕЛЬНО показываем карты
                if (hasPlayer || state.hole1Visible || state.hole2Visible || state.hole1Sprite != null || state.hole2Sprite != null)
                {
                    var cardBack = CardSpriteProvider.GetCardBack();
                    
                    if (hole1 != null)
                    {
                        Undo.RecordObject(hole1, "Restore Hole1");
                        
                        // Активируем GameObject, если выключен
                        if (!hole1.gameObject.activeSelf)
                            hole1.gameObject.SetActive(true);
                        
                        if (state.hole1Sprite != null)
                        {
                            hole1.sprite = state.hole1Sprite;
                            hole1.enabled = true;
                        }
                        else if (hasPlayer || state.hole1Visible)
                        {
                            // Если есть игрок или карта была видна, показываем рубашку
                            if (cardBack != null)
                                hole1.sprite = cardBack;
                            hole1.enabled = true;
                        }
                        
                        hole1.type = Image.Type.Simple;
                        hole1.preserveAspect = true;
                        hole1.color = Color.white;
                        EditorUtility.SetDirty(hole1);
                    }
                    
                    if (hole2 != null)
                    {
                        Undo.RecordObject(hole2, "Restore Hole2");
                        
                        // Активируем GameObject, если выключен
                        if (!hole2.gameObject.activeSelf)
                            hole2.gameObject.SetActive(true);
                        
                        if (state.hole2Sprite != null)
                        {
                            hole2.sprite = state.hole2Sprite;
                            hole2.enabled = true;
                        }
                        else if (hasPlayer || state.hole2Visible || state.hole1Visible)
                        {
                            // Если есть игрок или карта была видна, показываем рубашку
                            if (cardBack != null)
                                hole2.sprite = cardBack;
                            hole2.enabled = true;
                        }
                        
                        hole2.type = Image.Type.Simple;
                        hole2.preserveAspect = true;
                        hole2.color = Color.white;
                        EditorUtility.SetDirty(hole2);
                    }
                    
                    // Дополнительно вызываем ShowHoleBacks для гарантии
                    if (hasPlayer)
                        seat.ShowHoleBacks();
                }
                else
                {
                    // Если нет игрока, скрываем карты
                    if (hole1 != null)
                    {
                        Undo.RecordObject(hole1, "Hide Hole1");
                        hole1.enabled = false;
                        EditorUtility.SetDirty(hole1);
                    }
                    if (hole2 != null)
                    {
                        Undo.RecordObject(hole2, "Hide Hole2");
                        hole2.enabled = false;
                        EditorUtility.SetDirty(hole2);
                    }
                }
                
                // Восстанавливаем игрока
                seat.SetPlayer(state.playerName, int.TryParse(state.stackText, out int stack) ? stack : 0, state.avatarSprite);
                
                // ПОТОМ обновляем позиции карт и фишек (ConfigureHoleLayout)
                seat.ConfigureHoleLayout(inward, rot, holeDist, holeSpacing);
                
                // Восстанавливаем фишки и ставки ПОСЛЕ обновления позиций
                if (state.currentBetAmount > 0)
                {
                    seat.ShowBet(state.currentBetAmount);
                }
                else
                {
                    seat.ShowBet(0);
                }
                
                // Принудительно обновляем позиции фишек
                seat.RepositionChipsRelativeToSeat();
                
                // Дополнительно принудительно обновляем чип дисплей
                var chipDisplayField = typeof(NewBehaviourScript).GetField("chipDisplay", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                BetChipDisplay chipDisplay = chipDisplayField != null ? (BetChipDisplay)chipDisplayField.GetValue(seat) : null;
                
                if (chipDisplay != null)
                {
                    Undo.RecordObject(chipDisplay, "Update Chip Display");
                    
                    // Получаем seatRect для правильной настройки
                    RectTransform seatRect = seat.GetComponent<RectTransform>();
                    if (seatRect != null)
                    {
                        float chipDistance = holeDist + holeSpacing;
                        chipDisplay.ConfigureSeatAnchor(seatRect, inward, chipDistance);
                    }
                    
                    // Принудительно пересоздаем фишки, если была ставка
                    if (state.currentBetAmount > 0)
                    {
                        chipDisplay.SetAmount(state.currentBetAmount);
                    }
                    
                    chipDisplay.Reposition();
                    EditorUtility.SetDirty(chipDisplay);
                }
                
                EditorUtility.SetDirty(seat);
            }
            restoredCount++;
        }
        
        Debug.Log($"✓ Состояние мест восстановлено (обработано {restoredCount})");
    }
    
    private void ForceUpdateChipPositions()
    {
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        int updatedCount = 0;
        
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            // Принудительно обновляем позиции фишек
            seat.RepositionChipsRelativeToSeat();
            
            // Получаем BetChipDisplay и принудительно обновляем
            var chipDisplayField = typeof(NewBehaviourScript).GetField("chipDisplay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            BetChipDisplay chipDisplay = chipDisplayField != null ? (BetChipDisplay)chipDisplayField.GetValue(seat) : null;
            
            if (chipDisplay != null)
            {
                Undo.RecordObject(chipDisplay, "Force Update Chip Display");
                chipDisplay.Reposition();
                
                // Принудительно вызываем PositionContainer через рефлексию
                var positionMethod = typeof(BetChipDisplay).GetMethod("PositionContainer", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                positionMethod?.Invoke(chipDisplay, null);
                
                EditorUtility.SetDirty(chipDisplay);
            }
            
            updatedCount++;
        }
        
        Debug.Log($"✓ Фишки принудительно обновлены (обработано {updatedCount})");
    }
    
    private void FinalizeSeatRestoration(Dictionary<string, SeatFullState> fullSeatStates, SeatsLayoutRadial seatsLayout, float multiplier)
    {
        if (seatsLayout == null) return;
        
        NewBehaviourScript[] seats = FindObjectsOfType<NewBehaviourScript>();
        int finalizedCount = 0;
        
        // Получаем CardSpriteProvider для получения рубашки
        var cardBackSprite = CardSpriteProvider.GetCardBack();
        
        foreach (NewBehaviourScript seat in seats)
        {
            if (seat == null) continue;
            
            Undo.RecordObject(seat, "Finalize Seat Restoration");
            
            string seatName = seat.name;
            SeatFullState state = default(SeatFullState);
            bool hasSavedState = fullSeatStates != null && fullSeatStates.TryGetValue(seatName, out state);
            
            // Проверяем, есть ли игрок в этом месте (по имени или стеку)
            bool hasPlayer = hasSavedState && !string.IsNullOrEmpty(state.playerName) && state.playerName != "Свободно";
            
            // Финальная проверка и восстановление карт
            var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var holeCardBackField = typeof(NewBehaviourScript).GetField("holeCardBack", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
            Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
            Sprite cardBack = holeCardBackField != null ? (Sprite)holeCardBackField.GetValue(seat) : cardBackSprite;
            if (cardBack == null) cardBack = cardBackSprite;
            
            // РАДИКАЛЬНО: Если есть игрок ИЛИ было сохраненное состояние карт - ПРИНУДИТЕЛЬНО показываем
            bool shouldShowCards = hasPlayer || (hasSavedState && (state.hole1Visible || state.hole2Visible || state.hole1Sprite != null || state.hole2Sprite != null));
            
            // Если есть игрок, ОБЯЗАТЕЛЬНО показываем карты
            if (hasPlayer)
            {
                shouldShowCards = true;
            }
            
            if (shouldShowCards)
            {
                // ПРИНУДИТЕЛЬНО показываем карты - сначала пробуем восстановить спрайты, если нет - показываем рубашки
                if (hole1 != null)
                {
                    Undo.RecordObject(hole1, "Force Show Hole1");
                    
                    // Активируем GameObject карты, если он выключен
                    if (!hole1.gameObject.activeSelf)
                    {
                        hole1.gameObject.SetActive(true);
                    }
                    
                    // Восстанавливаем спрайт из сохраненного состояния, или устанавливаем рубашку
                    if (hasSavedState && state.hole1Sprite != null)
                    {
                        hole1.sprite = state.hole1Sprite;
                    }
                    else if (hole1.sprite == null && cardBack != null)
                    {
                        hole1.sprite = cardBack;
                    }
                    
                    // ПРИНУДИТЕЛЬНО включаем и настраиваем
                    hole1.enabled = true;
                    hole1.type = Image.Type.Simple;
                    hole1.preserveAspect = true;
                    hole1.color = Color.white;
                    
                    // Убеждаемся, что RectTransform настроен правильно
                    RectTransform hole1Rect = hole1.rectTransform;
                    if (hole1Rect != null)
                    {
                        hole1Rect.gameObject.SetActive(true);
                        var holeCardSizeField = typeof(NewBehaviourScript).GetField("holeCardSize", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (holeCardSizeField != null)
                        {
                            Vector2 cardSize = (Vector2)holeCardSizeField.GetValue(seat);
                            if (cardSize == Vector2.zero)
                            {
                                cardSize = new Vector2(65f, 95f) * multiplier;
                            }
                            hole1Rect.sizeDelta = cardSize;
                        }
                    }
                    
                    EditorUtility.SetDirty(hole1);
                }
                
                if (hole2 != null)
                {
                    Undo.RecordObject(hole2, "Force Show Hole2");
                    
                    // Активируем GameObject карты, если он выключен
                    if (!hole2.gameObject.activeSelf)
                    {
                        hole2.gameObject.SetActive(true);
                    }
                    
                    // Восстанавливаем спрайт из сохраненного состояния, или устанавливаем рубашку
                    if (hasSavedState && state.hole2Sprite != null)
                    {
                        hole2.sprite = state.hole2Sprite;
                    }
                    else if (hole2.sprite == null && cardBack != null)
                    {
                        hole2.sprite = cardBack;
                    }
                    
                    // ПРИНУДИТЕЛЬНО включаем и настраиваем
                    hole2.enabled = true;
                    hole2.type = Image.Type.Simple;
                    hole2.preserveAspect = true;
                    hole2.color = Color.white;
                    
                    // Убеждаемся, что RectTransform настроен правильно
                    RectTransform hole2Rect = hole2.rectTransform;
                    if (hole2Rect != null)
                    {
                        hole2Rect.gameObject.SetActive(true);
                        var holeCardSizeField = typeof(NewBehaviourScript).GetField("holeCardSize", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (holeCardSizeField != null)
                        {
                            Vector2 cardSize = (Vector2)holeCardSizeField.GetValue(seat);
                            if (cardSize == Vector2.zero)
                            {
                                cardSize = new Vector2(65f, 95f) * multiplier;
                            }
                            hole2Rect.sizeDelta = cardSize;
                        }
                    }
                    
                    EditorUtility.SetDirty(hole2);
                }
                
                // Дополнительно вызываем ShowHoleBacks для гарантии
                seat.ShowHoleBacks();
            }
            
            // Получаем параметры для реконфигурации позиций карт
            int seatIndex = -1;
            if (seatName.StartsWith("Seat_"))
            {
                string indexStr = seatName.Substring(5);
                if (int.TryParse(indexStr, out int idx))
                {
                    seatIndex = idx - 1;
                }
            }
            
            if (seatIndex >= 0 && seatIndex < seatsLayout.MaxSeats && shouldShowCards)
            {
                // Переконфигурируем позиции карт
                var startAngleField = typeof(SeatsLayoutRadial).GetField("startAngleDeg", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var clockwiseField = typeof(SeatsLayoutRadial).GetField("clockwise", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var holeDistanceField = typeof(SeatsLayoutRadial).GetField("holeDistance", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var holeSpacingField = typeof(SeatsLayoutRadial).GetField("holeSpacing", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                float startAngle = startAngleField != null ? (float)startAngleField.GetValue(seatsLayout) : 90f;
                bool clockwise = clockwiseField != null ? (bool)clockwiseField.GetValue(seatsLayout) : true;
                float holeDist = holeDistanceField != null ? (float)holeDistanceField.GetValue(seatsLayout) : 60f * multiplier;
                float holeSpacing = holeSpacingField != null ? (float)holeSpacingField.GetValue(seatsLayout) : 80f * multiplier;
                
                float dir = clockwise ? -1f : 1f;
                float t = seatIndex / (float)seatsLayout.MaxSeats;
                float angleDeg = startAngle + dir * t * 360f;
                float rad = Mathf.Deg2Rad * angleDeg;
                Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
                float rot = angleDeg + 90f;
                rot = Mathf.Repeat(rot, 360f);
                
                bool invertForTop = angleDeg > 45f && angleDeg < 135f;
                seat.SetHoleRotationOffset(invertForTop ? 180f : 0f);
                
                // Принудительно реконфигурируем позиции карт
                seat.ConfigureHoleLayout(inward, rot, holeDist, holeSpacing);
            }
            
            // Финально обновляем позиции фишек
            seat.RepositionChipsRelativeToSeat();
            
            var chipDisplayField = typeof(NewBehaviourScript).GetField("chipDisplay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            BetChipDisplay chipDisplay = chipDisplayField != null ? (BetChipDisplay)chipDisplayField.GetValue(seat) : null;
            
            if (chipDisplay != null)
            {
                Undo.RecordObject(chipDisplay, "Finalize Chip Display");
                chipDisplay.Reposition();
                
                // Принудительно вызываем PositionContainer
                var positionMethod = typeof(BetChipDisplay).GetMethod("PositionContainer", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                positionMethod?.Invoke(chipDisplay, null);
                
                EditorUtility.SetDirty(chipDisplay);
            }
            
            EditorUtility.SetDirty(seat);
            finalizedCount++;
        }
        
        Debug.Log($"✓ ФИНАЛЬНАЯ ПРОВЕРКА: обработано {finalizedCount} мест, карты ПРИНУДИТЕЛЬНО восстановлены");
    }
    
    private void ForceFixAllSeats(SeatsLayoutRadial seatsLayout, float multiplier, Dictionary<string, SeatFullState> savedStates)
    {
        if (seatsLayout == null)
        {
            Debug.LogWarning("ForceFixAllSeats: seatsLayout == null");
            return;
        }
        
        NewBehaviourScript[] allSeats = FindObjectsOfType<NewBehaviourScript>();
        Debug.Log($"ForceFixAllSeats: Найдено {allSeats.Length} мест");
        
        // Получаем параметры
        var startAngleField = typeof(SeatsLayoutRadial).GetField("startAngleDeg", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var clockwiseField = typeof(SeatsLayoutRadial).GetField("clockwise", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var holeDistanceField = typeof(SeatsLayoutRadial).GetField("holeDistance", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var holeSpacingField = typeof(SeatsLayoutRadial).GetField("holeSpacing", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        float startAngle = startAngleField != null ? (float)startAngleField.GetValue(seatsLayout) : 90f;
        bool clockwise = clockwiseField != null ? (bool)clockwiseField.GetValue(seatsLayout) : true;
        float holeDist = holeDistanceField != null ? (float)holeDistanceField.GetValue(seatsLayout) : 60f * multiplier;
        float holeSpacing = holeSpacingField != null ? (float)holeSpacingField.GetValue(seatsLayout) : 80f * multiplier;
        
        float dir = clockwise ? -1f : 1f;
        int fixedCount = 0;
        
        foreach (NewBehaviourScript seat in allSeats)
        {
            if (seat == null) continue;
            
            Undo.RecordObject(seat, "Force Fix Seat");
            
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
            
            if (seatIndex < 0 || seatIndex >= seatsLayout.MaxSeats) continue;
            
            // Вычисляем параметры позиционирования
            float t = seatIndex / (float)seatsLayout.MaxSeats;
            float angleDeg = startAngle + dir * t * 360f;
            float rad = Mathf.Deg2Rad * angleDeg;
            Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
            float rot = angleDeg + 90f;
            rot = Mathf.Repeat(rot, 360f);
            
            bool invertForTop = angleDeg > 45f && angleDeg < 135f;
            seat.SetHoleRotationOffset(invertForTop ? 180f : 0f);
            
            // 1. ПРИНУДИТЕЛЬНО показываем карты
            var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
            Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
            
            // Показываем карты - сначала пробуем восстановить из сохраненного состояния
            if (savedStates != null && savedStates.TryGetValue(seatName, out SeatFullState savedState))
            {
                if (hole1 != null)
                {
                    if (savedState.hole1Visible && savedState.hole1Sprite != null)
                    {
                        hole1.sprite = savedState.hole1Sprite;
                        hole1.enabled = true;
                    }
                    else
                    {
                        // Если не было сохранено, показываем рубашку
                        seat.ShowHoleBacks();
                    }
                }
                
                if (hole2 != null)
                {
                    if (savedState.hole2Visible && savedState.hole2Sprite != null)
                    {
                        hole2.sprite = savedState.hole2Sprite;
                        hole2.enabled = true;
                    }
                    else if (hole1 != null && hole1.enabled)
                    {
                        // Если первая карта показывается, показываем и вторую
                        seat.ShowHoleBacks();
                    }
                }
            }
            else
            {
                // Если нет сохраненного состояния, показываем рубашки
                seat.ShowHoleBacks();
            }
            
            // Принудительно включаем карты, если они есть
            if (hole1 != null)
            {
                Undo.RecordObject(hole1, "Force Enable Hole1");
                if (hole1.sprite == null)
                {
                    seat.ShowHoleBacks(); // Показываем рубашку, если нет спрайта
                }
                hole1.enabled = true;
                hole1.type = Image.Type.Simple;
                hole1.preserveAspect = true;
                EditorUtility.SetDirty(hole1);
            }
            
            if (hole2 != null)
            {
                Undo.RecordObject(hole2, "Force Enable Hole2");
                if (hole2.sprite == null)
                {
                    seat.ShowHoleBacks(); // Показываем рубашку, если нет спрайта
                }
                hole2.enabled = true;
                hole2.type = Image.Type.Simple;
                hole2.preserveAspect = true;
                EditorUtility.SetDirty(hole2);
            }
            
            // 2. ОБНОВЛЯЕМ позиции карт через ConfigureHoleLayout
            seat.ConfigureHoleLayout(inward, rot, holeDist, holeSpacing);
            
            // 3. ПРИНУДИТЕЛЬНО обновляем позиции фишек
            var chipDisplayField = typeof(NewBehaviourScript).GetField("chipDisplay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            BetChipDisplay chipDisplay = chipDisplayField != null ? (BetChipDisplay)chipDisplayField.GetValue(seat) : null;
            
            if (chipDisplay != null)
            {
                Undo.RecordObject(chipDisplay, "Force Fix Chip Display");
                
                // Получаем seatRect
                RectTransform seatRect = seat.GetComponent<RectTransform>();
                if (seatRect != null)
                {
                    // Вычисляем расстояние для фишек (после карт)
                    float chipDistance = holeDist + holeSpacing;
                    
                    // Принудительно настраиваем якорь
                    chipDisplay.ConfigureSeatAnchor(seatRect, inward, chipDistance);
                    
                    // Принудительно обновляем позицию
                    chipDisplay.Reposition();
                    
                    // Принудительно вызываем PositionContainer через рефлексию
                    var positionMethod = typeof(BetChipDisplay).GetMethod("PositionContainer", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    positionMethod?.Invoke(chipDisplay, null);
                    
                    // Если была ставка, восстанавливаем её
                    if (savedStates != null && savedStates.TryGetValue(seatName, out SeatFullState state))
                    {
                        if (state.currentBetAmount > 0)
                        {
                            chipDisplay.SetAmount(state.currentBetAmount);
                            chipDisplay.Show(true);
                        }
                    }
                }
                
                EditorUtility.SetDirty(chipDisplay);
            }
            
            // 4. Дополнительно вызываем RepositionChipsRelativeToSeat
            seat.RepositionChipsRelativeToSeat();
            
            EditorUtility.SetDirty(seat);
            fixedCount++;
        }
        
        Debug.Log($"✓ РАДИКАЛЬНОЕ ИСПРАВЛЕНИЕ: обработано {fixedCount} мест");
    }
    
    private void ForceShowCardsForAllPlayers(Dictionary<string, SeatFullState> fullSeatStates, SeatsLayoutRadial seatsLayout, float multiplier)
    {
        NewBehaviourScript[] allSeats = FindObjectsOfType<NewBehaviourScript>();
        var cardBack = CardSpriteProvider.GetCardBack();
        int forcedCount = 0;
        
        foreach (NewBehaviourScript seat in allSeats)
        {
            if (seat == null) continue;
            
            string seatName = seat.name;
            SeatFullState state = default(SeatFullState);
            bool hasSavedState = fullSeatStates != null && fullSeatStates.TryGetValue(seatName, out state);
            
            // Проверяем, есть ли игрок
            var nameTextField = typeof(NewBehaviourScript).GetField("nameText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nameTextTMPField = typeof(NewBehaviourScript).GetField("nameTextTMP", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Text nameText = nameTextField != null ? (Text)nameTextField.GetValue(seat) : null;
            TMPro.TMP_Text nameTextTMP = nameTextTMPField != null ? (TMPro.TMP_Text)nameTextTMPField.GetValue(seat) : null;
            
            string currentPlayerName = nameTextTMP != null ? nameTextTMP.text : (nameText != null ? nameText.text : "");
            bool hasPlayer = !string.IsNullOrEmpty(currentPlayerName) && currentPlayerName != "Свободно";
            
            // Если есть игрок, ПРИНУДИТЕЛЬНО показываем карты
            if (hasPlayer)
            {
                var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
                Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
                
                if (hole1 != null)
                {
                    Undo.RecordObject(hole1, "Force Show Hole1 For Player");
                    
                    // Активируем GameObject
                    if (!hole1.gameObject.activeSelf)
                        hole1.gameObject.SetActive(true);
                    
                    // Если нет спрайта, устанавливаем рубашку
                    if (hole1.sprite == null && cardBack != null)
                        hole1.sprite = cardBack;
                    
                    hole1.enabled = true;
                    hole1.type = Image.Type.Simple;
                    hole1.preserveAspect = true;
                    hole1.color = Color.white;
                    EditorUtility.SetDirty(hole1);
                }
                
                if (hole2 != null)
                {
                    Undo.RecordObject(hole2, "Force Show Hole2 For Player");
                    
                    // Активируем GameObject
                    if (!hole2.gameObject.activeSelf)
                        hole2.gameObject.SetActive(true);
                    
                    // Если нет спрайта, устанавливаем рубашку
                    if (hole2.sprite == null && cardBack != null)
                        hole2.sprite = cardBack;
                    
                    hole2.enabled = true;
                    hole2.type = Image.Type.Simple;
                    hole2.preserveAspect = true;
                    hole2.color = Color.white;
                    EditorUtility.SetDirty(hole2);
                }
                
                // Дополнительно вызываем ShowHoleBacks
                seat.ShowHoleBacks();
                
                forcedCount++;
            }
        }
        
        Debug.Log($"✓ ПРИНУДИТЕЛЬНО показаны карты у {forcedCount} игроков");
    }
    
    private void EnsureHoleCardsExist()
    {
        NewBehaviourScript[] allSeats = FindObjectsOfType<NewBehaviourScript>();
        int createdCount = 0;
        var cardBack = CardSpriteProvider.GetCardBack();
        
        foreach (NewBehaviourScript seat in allSeats)
        {
            if (seat == null) continue;
            
            bool needsCreation = false;
            
            // Проверяем, есть ли Hole1 и Hole2 через рефлексию
            var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
            Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
            
            // Проверяем, есть ли GameObject'ы Hole1 и Hole2 в иерархии (даже выключенные)
            Transform hole1Transform = seat.transform.Find("Hole1");
            Transform hole2Transform = seat.transform.Find("Hole2");
            
            // Если GameObject'ы есть, но Image компоненты не привязаны - находим их
            if (hole1Transform != null && hole1 == null)
            {
                hole1 = hole1Transform.GetComponent<Image>();
            }
            if (hole2Transform != null && hole2 == null)
            {
                hole2 = hole2Transform.GetComponent<Image>();
            }
            
            // Если Image компоненты отсутствуют или GameObject'ы отсутствуют - создаем
            if (hole1 == null || hole1Transform == null)
            {
                // Создаем Hole1
                GameObject hole1GO = new GameObject("Hole1");
                hole1GO.transform.SetParent(seat.transform, false);
                RectTransform hole1Rect = hole1GO.AddComponent<RectTransform>();
                hole1Rect.anchorMin = new Vector2(0.5f, 0.5f);
                hole1Rect.anchorMax = new Vector2(0.5f, 0.5f);
                hole1Rect.pivot = new Vector2(0.5f, 0.5f);
                hole1Rect.sizeDelta = new Vector2(65f, 95f);
                hole1Rect.anchoredPosition = Vector2.zero;
                
                hole1 = hole1GO.AddComponent<Image>();
                hole1.color = Color.white;
                hole1.type = Image.Type.Simple;
                hole1.preserveAspect = true;
                if (cardBack != null)
                    hole1.sprite = cardBack;
                hole1.enabled = false;
                
                // Привязываем к NewBehaviourScript
                if (hole1Field != null)
                    hole1Field.SetValue(seat, hole1);
                
                Undo.RegisterCreatedObjectUndo(hole1GO, "Create Hole1");
                needsCreation = true;
            }
            
            if (hole2 == null || hole2Transform == null)
            {
                // Создаем Hole2
                GameObject hole2GO = new GameObject("Hole2");
                hole2GO.transform.SetParent(seat.transform, false);
                RectTransform hole2Rect = hole2GO.AddComponent<RectTransform>();
                hole2Rect.anchorMin = new Vector2(0.5f, 0.5f);
                hole2Rect.anchorMax = new Vector2(0.5f, 0.5f);
                hole2Rect.pivot = new Vector2(0.5f, 0.5f);
                hole2Rect.sizeDelta = new Vector2(65f, 95f);
                hole2Rect.anchoredPosition = Vector2.zero;
                
                hole2 = hole2GO.AddComponent<Image>();
                hole2.color = Color.white;
                hole2.type = Image.Type.Simple;
                hole2.preserveAspect = true;
                if (cardBack != null)
                    hole2.sprite = cardBack;
                hole2.enabled = false;
                
                // Привязываем к NewBehaviourScript
                if (hole2Field != null)
                    hole2Field.SetValue(seat, hole2);
                
                Undo.RegisterCreatedObjectUndo(hole2GO, "Create Hole2");
                needsCreation = true;
            }
            
            // ВСЕГДА привязываем Image компоненты к NewBehaviourScript, если они есть
            if (hole1 != null && hole1Field != null)
            {
                Image currentHole1 = (Image)hole1Field.GetValue(seat);
                if (currentHole1 != hole1)
                {
                    hole1Field.SetValue(seat, hole1);
                    needsCreation = true;
                }
            }
            
            if (hole2 != null && hole2Field != null)
            {
                Image currentHole2 = (Image)hole2Field.GetValue(seat);
                if (currentHole2 != hole2)
                {
                    hole2Field.SetValue(seat, hole2);
                    needsCreation = true;
                }
            }
            
            // Если GameObject'ы выключены - включаем их
            if (hole1Transform != null && !hole1Transform.gameObject.activeSelf)
            {
                hole1Transform.gameObject.SetActive(true);
                needsCreation = true;
            }
            if (hole2Transform != null && !hole2Transform.gameObject.activeSelf)
            {
                hole2Transform.gameObject.SetActive(true);
                needsCreation = true;
            }
            
            if (needsCreation)
            {
                EditorUtility.SetDirty(seat);
                createdCount++;
            }
        }
        
        if (createdCount > 0)
        {
            Debug.Log($"✓ Создано/исправлено {createdCount} мест: добавлены отсутствующие Hole1/Hole2");
        }
        else
        {
            Debug.Log($"✓ Все места имеют Hole1 и Hole2");
        }
    }
    
    private void MakeCardsPersistentlyVisible()
    {
        NewBehaviourScript[] allSeats = FindObjectsOfType<NewBehaviourScript>();
        int processedCount = 0;
        var cardBack = CardSpriteProvider.GetCardBack();
        
        foreach (NewBehaviourScript seat in allSeats)
        {
            if (seat == null) continue;
            
            // Проверяем, есть ли игрок
            var nameTextField = typeof(NewBehaviourScript).GetField("nameText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nameTextTMPField = typeof(NewBehaviourScript).GetField("nameTextTMP", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Text nameText = nameTextField != null ? (Text)nameTextField.GetValue(seat) : null;
            TMPro.TMP_Text nameTextTMP = nameTextTMPField != null ? (TMPro.TMP_Text)nameTextTMPField.GetValue(seat) : null;
            
            string currentPlayerName = nameTextTMP != null ? nameTextTMP.text : (nameText != null ? nameText.text : "");
            bool hasPlayer = !string.IsNullOrEmpty(currentPlayerName) && currentPlayerName != "Свободно";
            
            if (!hasPlayer) continue;
            
            var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
            Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
            
            if (hole1 != null)
            {
                Undo.RecordObject(hole1, "Make Hole1 Persistently Visible");
                
                // Убеждаемся, что GameObject активен
                if (!hole1.gameObject.activeSelf)
                    hole1.gameObject.SetActive(true);
                
                // Убеждаемся, что есть спрайт
                if (hole1.sprite == null && cardBack != null)
                    hole1.sprite = cardBack;
                
                // ПРИНУДИТЕЛЬНО включаем
                hole1.enabled = true;
                hole1.type = Image.Type.Simple;
                hole1.preserveAspect = true;
                hole1.color = Color.white;
                
                EditorUtility.SetDirty(hole1);
            }
            
            if (hole2 != null)
            {
                Undo.RecordObject(hole2, "Make Hole2 Persistently Visible");
                
                // Убеждаемся, что GameObject активен
                if (!hole2.gameObject.activeSelf)
                    hole2.gameObject.SetActive(true);
                
                // Убеждаемся, что есть спрайт
                if (hole2.sprite == null && cardBack != null)
                    hole2.sprite = cardBack;
                
                // ПРИНУДИТЕЛЬНО включаем
                hole2.enabled = true;
                hole2.type = Image.Type.Simple;
                hole2.preserveAspect = true;
                hole2.color = Color.white;
                
                EditorUtility.SetDirty(hole2);
            }
            
            // Вызываем ShowHoleBacks для гарантии
            seat.ShowHoleBacks();
            
            processedCount++;
        }
        
        Debug.Log($"✓ Карты сделаны постоянно видимыми для {processedCount} игроков");
    }
    
    private void FinalizeCardScaling(float multiplier)
    {
        NewBehaviourScript[] allSeats = FindObjectsOfType<NewBehaviourScript>();
        Vector2 baseHoleSize = new Vector2(65f, 95f);
        Vector2 expectedSize = baseHoleSize * multiplier;
        int scaledCount = 0;
        
        foreach (NewBehaviourScript seat in allSeats)
        {
            if (seat == null) continue;
            
            var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
            Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
            
            // Если Image не привязаны, ищем в иерархии
            if (hole1 == null)
            {
                Transform hole1Transform = seat.transform.Find("Hole1");
                if (hole1Transform != null)
                    hole1 = hole1Transform.GetComponent<Image>();
            }
            if (hole2 == null)
            {
                Transform hole2Transform = seat.transform.Find("Hole2");
                if (hole2Transform != null)
                    hole2 = hole2Transform.GetComponent<Image>();
            }
            
            // Убеждаемся, что размеры карт правильные
            if (hole1 != null)
            {
                RectTransform hole1Rect = hole1.rectTransform;
                if (hole1Rect != null)
                {
                    Undo.RecordObject(hole1Rect, "Finalize Hole1 Scale");
                    hole1Rect.sizeDelta = expectedSize;
                    EditorUtility.SetDirty(hole1Rect);
                }
            }
            
            if (hole2 != null)
            {
                RectTransform hole2Rect = hole2.rectTransform;
                if (hole2Rect != null)
                {
                    Undo.RecordObject(hole2Rect, "Finalize Hole2 Scale");
                    hole2Rect.sizeDelta = expectedSize;
                    EditorUtility.SetDirty(hole2Rect);
                }
            }
            
            // Обновляем поле holeCardSize в скрипте
            var holeCardSizeField = typeof(NewBehaviourScript).GetField("holeCardSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (holeCardSizeField != null)
            {
                holeCardSizeField.SetValue(seat, expectedSize);
                
                // Вызываем SetHoleCardSizes для применения
                var setSizeMethod = typeof(NewBehaviourScript).GetMethod("SetHoleCardSizes", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                setSizeMethod?.Invoke(seat, null);
                
                EditorUtility.SetDirty(seat);
            }
            
            scaledCount++;
        }
        
        Debug.Log($"✓ ФИНАЛЬНОЕ МАСШТАБИРОВАНИЕ: карты масштабированы до {expectedSize.x:F0}x{expectedSize.y:F0} для {scaledCount} мест");
    }
    
    private void FinalizeCardPositions(SeatsLayoutRadial seatsLayout, float multiplier)
    {
        if (seatsLayout == null) return;
        
        NewBehaviourScript[] allSeats = FindObjectsOfType<NewBehaviourScript>();
        int repositionedCount = 0;
        
        // Получаем параметры из SeatsLayoutRadial
        var startAngleField = typeof(SeatsLayoutRadial).GetField("startAngleDeg", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var clockwiseField = typeof(SeatsLayoutRadial).GetField("clockwise", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var holeDistanceField = typeof(SeatsLayoutRadial).GetField("holeDistance", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var holeSpacingField = typeof(SeatsLayoutRadial).GetField("holeSpacing", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        float startAngle = startAngleField != null ? (float)startAngleField.GetValue(seatsLayout) : 90f;
        bool clockwise = clockwiseField != null ? (bool)clockwiseField.GetValue(seatsLayout) : true;
        float holeDist = holeDistanceField != null ? (float)holeDistanceField.GetValue(seatsLayout) : 60f * multiplier;
        float holeSpacing = holeSpacingField != null ? (float)holeSpacingField.GetValue(seatsLayout) : 80f * multiplier;
        
        float dir = clockwise ? -1f : 1f;
        
        foreach (NewBehaviourScript seat in allSeats)
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
            
            if (seatIndex >= 0 && seatIndex < seatsLayout.MaxSeats)
            {
                // Вычисляем правильные параметры позиционирования
                float t = seatIndex / (float)seatsLayout.MaxSeats;
                float angleDeg = startAngle + dir * t * 360f;
                float rad = Mathf.Deg2Rad * angleDeg;
                Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
                float rot = angleDeg + 90f;
                rot = Mathf.Repeat(rot, 360f);
                
                bool invertForTop = angleDeg > 45f && angleDeg < 135f;
                seat.SetHoleRotationOffset(invertForTop ? 180f : 0f);
                
                Undo.RecordObject(seat, "Finalize Card Positions");
                
                // ПРИНУДИТЕЛЬНО перепозиционируем карты
                seat.ConfigureHoleLayout(inward, rot, holeDist, holeSpacing);
                
                // Убеждаемся, что карты имеют правильные размеры
                var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Image hole1 = hole1Field != null ? (Image)hole1Field.GetValue(seat) : null;
                Image hole2 = hole2Field != null ? (Image)hole2Field.GetValue(seat) : null;
                
                if (hole1 == null)
                {
                    Transform hole1Transform = seat.transform.Find("Hole1");
                    if (hole1Transform != null)
                        hole1 = hole1Transform.GetComponent<Image>();
                }
                if (hole2 == null)
                {
                    Transform hole2Transform = seat.transform.Find("Hole2");
                    if (hole2Transform != null)
                        hole2 = hole2Transform.GetComponent<Image>();
                }
                
                // Убеждаемся, что размеры карт правильные
                Vector2 expectedSize = new Vector2(65f, 95f) * multiplier;
                if (hole1 != null && hole1.rectTransform != null)
                {
                    Undo.RecordObject(hole1.rectTransform, "Finalize Hole1 Position and Size");
                    hole1.rectTransform.sizeDelta = expectedSize;
                    EditorUtility.SetDirty(hole1.rectTransform);
                }
                if (hole2 != null && hole2.rectTransform != null)
                {
                    Undo.RecordObject(hole2.rectTransform, "Finalize Hole2 Position and Size");
                    hole2.rectTransform.sizeDelta = expectedSize;
                    EditorUtility.SetDirty(hole2.rectTransform);
                }
                
                // Обновляем позиции фишек
                seat.RepositionChipsRelativeToSeat();
                
                var chipDisplayField = typeof(NewBehaviourScript).GetField("chipDisplay", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                BetChipDisplay chipDisplay = chipDisplayField != null ? (BetChipDisplay)chipDisplayField.GetValue(seat) : null;
                
                if (chipDisplay != null)
                {
                    Undo.RecordObject(chipDisplay, "Finalize Chip Position");
                    RectTransform seatRect = seat.GetComponent<RectTransform>();
                    if (seatRect != null)
                    {
                        float chipDistance = holeDist + holeSpacing;
                        chipDisplay.ConfigureSeatAnchor(seatRect, inward, chipDistance);
                    }
                    chipDisplay.Reposition();
                    EditorUtility.SetDirty(chipDisplay);
                }
                
                EditorUtility.SetDirty(seat);
                repositionedCount++;
            }
        }
        
        Debug.Log($"✓ ФИНАЛЬНОЕ ПЕРЕПОЗИЦИОНИРОВАНИЕ: карты правильно расположены для {repositionedCount} мест");
    }
}

