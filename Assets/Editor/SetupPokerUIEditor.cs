using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using TMPro;

// Этот скрипт добавляет пункт меню Tools/Poker/Auto-Setup
// Он создаёт префаб Seat, контейнер Seats и настраивает SeatsLayoutRadial.
public static class SetupPokerUIEditor
{
#if UNITY_EDITOR
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string SeatPrefabPath = PrefabsFolder + "/Seat.prefab";

    [MenuItem("Tools/Poker/Auto-Setup")] 
    public static void AutoSetup()
    {
        // 1) Убедиться, что есть Canvas и стол
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        RectTransform tableRect = null;
        var tablePanel = GameObject.Find("Table Panel");
        if (tablePanel != null)
        {
            tableRect = tablePanel.GetComponent<RectTransform>();
        }

        // 2) Создать префаб Seat если его нет
        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject seatRoot = CreateSeatGO();

        // Создать/обновить префаб
        GameObject seatPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SeatPrefabPath);
        if (seatPrefabAsset == null)
        {
            PrefabUtility.SaveAsPrefabAsset(seatRoot, SeatPrefabPath);
        }
        else
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(seatRoot, SeatPrefabPath, InteractionMode.AutomatedAction);
        }

        // Удаляем временный объект из сцены
        Object.DestroyImmediate(seatRoot);

        // 3) Создать контейнер Seats и настроить раскладку
        GameObject seatsContainer = GameObject.Find("Seats");
        if (seatsContainer == null)
        {
            seatsContainer = new GameObject("Seats", typeof(RectTransform));
            seatsContainer.transform.SetParent(canvas.transform, false);
            var rt = seatsContainer.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        var layout = seatsContainer.GetComponent<SeatsLayoutRadial>();
        if (layout == null)
        {
            layout = seatsContainer.AddComponent<SeatsLayoutRadial>();
        }

        // Проставляем ссылки
        layout.GetType().GetField("seatPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(layout, AssetDatabase.LoadAssetAtPath<GameObject>(SeatPrefabPath)?.GetComponent<RectTransform>());

        if (tableRect != null)
        {
            layout.GetType().GetField("tableRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(layout, tableRect);
        }

        // Сохраняем сцену
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // Попробуем сразу разложить места
        layout.SendMessage("SpawnSeats", SendMessageOptions.DontRequireReceiver);

        Debug.Log("Poker Auto-Setup: создан префаб Seat, добавлен контейнер Seats и разложены места.");
    }

    [MenuItem("Tools/Poker/Fix Blurry UI")] 
    public static void FixBlurryUI()
    {
        // Настройка Canvas Scaler и включение Pixel Perfect
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("Canvas не найден. Создаю новый.");
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

#if UNITY_2021_3_OR_NEWER
        var canvasComp = canvas.GetComponent<Canvas>();
        canvasComp.pixelPerfect = true;
#endif

        // Переимпорт всех спрайтов в папке Art с настройками для UI
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 100;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Point; // максимально чётко для UI
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
        }

        // Приводим Table Panel к нативному размеру и масштабу 1
        var tablePanel = GameObject.Find("Table Panel");
        if (tablePanel != null)
        {
            var img = tablePanel.GetComponent<Image>();
            var rt = tablePanel.GetComponent<RectTransform>();
            if (img != null)
            {
                img.preserveAspect = true;
                img.SetNativeSize();
            }
            if (rt != null)
            {
                rt.localScale = Vector3.one;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Poker Fix Blurry UI: настроен Canvas Scaler, включён Pixel Perfect и переимпортированы спрайты.");
    }

    [MenuItem("Tools/Poker/Convert Text to TMP")] 
    public static void ConvertTextToTMP()
    {
        // Проверим наличие префаба Seat
        GameObject seatPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SeatPrefabPath);
        if (seatPrefabAsset == null)
        {
            Debug.LogWarning("Seat.prefab не найден. Сначала запусти Tools/Poker/Auto-Setup или создай префаб вручную.");
            return;
        }

        // Создаём временную копию префаба в сцене, конвертируем, сохраняем обратно
        GameObject seatInstance = (GameObject)PrefabUtility.InstantiatePrefab(seatPrefabAsset);
        seatInstance.name = "Seat_TEMP_CONVERT";

        ConvertSeatTexts(seatInstance);

        PrefabUtility.SaveAsPrefabAsset(seatInstance, SeatPrefabPath);
        Object.DestroyImmediate(seatInstance);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Poker Convert Text to TMP: тексты в Seat.prefab заменены на TextMeshProUGUI и привязаны в скрипте.");
    }

    private static void ConvertSeatTexts(GameObject seatRoot)
    {
        if (seatRoot == null) return;

        NewBehaviourScript ctrl = seatRoot.GetComponent<NewBehaviourScript>();
        if (ctrl == null)
        {
            Debug.LogWarning("На Seat нет NewBehaviourScript — пропускаю привязку TMP полей.");
        }

        // Ищем дочерние элементы по ожидаемым именам
        Transform tName = seatRoot.transform.Find("NameText");
        Transform tStack = seatRoot.transform.Find("StackText");
        Transform tBetBubble = seatRoot.transform.Find("BetBubble");
        Transform tBetText = tBetBubble != null ? tBetBubble.Find("BetText") : null;

        // Конвертация помощник
        System.Func<Transform, TMP_Text> EnsureTMP = (tr) =>
        {
            if (tr == null) return null;
            var tmp = tr.GetComponent<TMP_Text>();
            if (tmp == null)
            {
                string cached = null;
                var legacy = tr.GetComponent<Text>();
                if (legacy != null)
                {
                    cached = legacy.text;
                }
                tmp = tr.gameObject.AddComponent<TextMeshProUGUI>();
                if (!string.IsNullOrEmpty(cached)) tmp.text = cached;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = legacy != null ? legacy.color : Color.white;
                if (legacy != null) Object.DestroyImmediate(legacy);
            }
            return tmp;
        };

        TMP_Text nameTMP = EnsureTMP(tName);
        TMP_Text stackTMP = EnsureTMP(tStack);
        TMP_Text betTMP = EnsureTMP(tBetText);

        // Привязка к приватным полям контроллера через Reflection
        if (ctrl != null)
        {
            var type = typeof(NewBehaviourScript);
            type.GetField("nameTextTMP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, nameTMP);
            type.GetField("stackTextTMP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, stackTMP);
            type.GetField("betTextTMP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, betTMP);

            // ВАЖНО: пометить как изменённый, чтобы сериализация сохранила ссылки
            EditorUtility.SetDirty(ctrl);
        }

        // На всякий случай пометим dirty и сам корневой объект
        EditorUtility.SetDirty(seatRoot);
    }

    private static GameObject CreateSeatGO()
    {
        // Корневой объект
        GameObject seat = new GameObject("Seat", typeof(RectTransform));
        RectTransform seatRT = seat.GetComponent<RectTransform>();
        seatRT.sizeDelta = new Vector2(160, 110);
        seatRT.anchorMin = seatRT.anchorMax = new Vector2(0.5f, 0.5f);
        seatRT.pivot = new Vector2(0.5f, 0.5f);

        // Avatar (Image)
        GameObject avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
        avatar.transform.SetParent(seat.transform, false);
        var avatarRT = avatar.GetComponent<RectTransform>();
        avatarRT.sizeDelta = new Vector2(64, 64);
        avatarRT.anchoredPosition = new Vector2(0, 10);

        // NameText (Text)
        GameObject nameText = new GameObject("NameText", typeof(RectTransform), typeof(Text));
        nameText.transform.SetParent(seat.transform, false);
        var nameRT = nameText.GetComponent<RectTransform>();
        nameRT.sizeDelta = new Vector2(160, 20);
        nameRT.anchoredPosition = new Vector2(0, 50);
        var nameLabel = nameText.GetComponent<Text>();
        nameLabel.text = "Имя игрока";
        nameLabel.alignment = TextAnchor.MiddleCenter;
        nameLabel.color = Color.white;

        // StackText (Text)
        GameObject stackText = new GameObject("StackText", typeof(RectTransform), typeof(Text));
        stackText.transform.SetParent(seat.transform, false);
        var stackRT = stackText.GetComponent<RectTransform>();
        stackRT.sizeDelta = new Vector2(160, 18);
        stackRT.anchoredPosition = new Vector2(0, -42);
        var stackLabel = stackText.GetComponent<Text>();
        stackLabel.text = "1000";
        stackLabel.alignment = TextAnchor.MiddleCenter;
        stackLabel.color = new Color(1f, 0.9f, 0.4f);

        // BetBubble (Image) + BetText
        GameObject betBubble = new GameObject("BetBubble", typeof(RectTransform), typeof(Image));
        betBubble.transform.SetParent(seat.transform, false);
        var betRT = betBubble.GetComponent<RectTransform>();
        betRT.sizeDelta = new Vector2(70, 26);
        betRT.anchoredPosition = new Vector2(80, -10);
        betBubble.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        GameObject betText = new GameObject("BetText", typeof(RectTransform), typeof(Text));
        betText.transform.SetParent(betBubble.transform, false);
        var betTextRT = betText.GetComponent<RectTransform>();
        betTextRT.sizeDelta = new Vector2(70, 26);
        var betLabel = betText.GetComponent<Text>();
        betLabel.text = "0";
        betLabel.alignment = TextAnchor.MiddleCenter;
        betLabel.color = Color.white;
        betBubble.SetActive(false);

        // DealerButton (Image)
        GameObject dealer = new GameObject("DealerButton", typeof(RectTransform), typeof(Image));
        dealer.transform.SetParent(seat.transform, false);
        var dealerRT = dealer.GetComponent<RectTransform>();
        dealerRT.sizeDelta = new Vector2(22, 22);
        dealerRT.anchoredPosition = new Vector2(-70, -35);
        dealer.SetActive(false);

        // Контроллер NewBehaviourScript
        var controller = seat.AddComponent<NewBehaviourScript>();
        controller.GetType().GetField("avatarImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(controller, avatar.GetComponent<Image>());
        controller.GetType().GetField("nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(controller, nameText.GetComponent<Text>());
        controller.GetType().GetField("stackText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(controller, stackText.GetComponent<Text>());
        controller.GetType().GetField("betBubble", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(controller, betBubble);
        controller.GetType().GetField("betText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(controller, betText.GetComponent<Text>());
        controller.GetType().GetField("dealerButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(controller, dealer);

        return seat;
    }
#endif

#if UNITY_EDITOR
    [MenuItem("Tools/Poker/Seat - Set Name Y = 50")] 
    public static void SeatSetNameY50()
    {
        GameObject seatPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SeatPrefabPath);
        if (seatPrefabAsset == null)
        {
            Debug.LogWarning("Seat.prefab не найден. Сначала создайте его через Auto-Setup.");
            return;
        }

        GameObject seatInstance = (GameObject)PrefabUtility.InstantiatePrefab(seatPrefabAsset);
        seatInstance.name = "Seat_TEMP_ADJUST";

        var nameTr = seatInstance.transform.Find("NameText");
        if (nameTr != null)
        {
            var rt = nameTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                var p = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(p.x, 50f);
                EditorUtility.SetDirty(rt);
            }
        }

        PrefabUtility.SaveAsPrefabAsset(seatInstance, SeatPrefabPath);
        Object.DestroyImmediate(seatInstance);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Seat: позиция NameText по Y установлена в 50 и сохранена в префаб.");
    }

    [MenuItem("Tools/Poker/Add TableManager")] 
    public static void AddTableManager()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("Canvas не найден. Сначала выполните Tools/Poker/Auto-Setup.");
            return;
        }

        var seatsGO = GameObject.Find("Seats");
        if (seatsGO == null)
        {
            Debug.LogWarning("Seats контейнер не найден. Выполните Auto-Setup или создайте Seats вручную.");
            return;
        }

        var seatsLayout = seatsGO.GetComponent<SeatsLayoutRadial>();
        if (seatsLayout == null)
        {
            seatsLayout = seatsGO.AddComponent<SeatsLayoutRadial>();
        }

        GameObject tableRoot = GameObject.Find("Table");
        if (tableRoot == null)
        {
            tableRoot = new GameObject("Table");
            tableRoot.transform.SetParent(canvas.transform, false);
        }

        var tm = tableRoot.GetComponent<TableManager>();
        if (tm == null) tm = tableRoot.AddComponent<TableManager>();

        var tmType = typeof(TableManager);
        tmType.GetField("seatsLayout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(tm, seatsLayout);
        tmType.GetField("minPlayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(tm, 2);
        var slType = typeof(SeatsLayoutRadial);
        var maxField = slType.GetField("maxSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int maxSeats = maxField != null ? (int)maxField.GetValue(seatsLayout) : 9;
        tmType.GetField("maxPlayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(tm, Mathf.Clamp(maxSeats, 2, 9));
        tmType.GetField("autoFillOnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(tm, true);

        EditorUtility.SetDirty(tm);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("TableManager добавлен и настроен. При старте он заполнит стол до минимума игроков.");
    }
#endif
}


