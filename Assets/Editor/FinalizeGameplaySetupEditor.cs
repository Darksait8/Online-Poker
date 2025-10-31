using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public static class FinalizeGameplaySetupEditor
{
#if UNITY_EDITOR
    [MenuItem("Tools/Poker/Finalize Gameplay Setup")]
    public static void FinalizeSetup()
    {
        // 1) Найти/подготовить Seats и раскладку
        var seatsGO = GameObject.Find("Seats");
        if (seatsGO == null)
        {
            Debug.LogError("Seats container not found. Create 'Seats' under Canvas.");
            return;
        }
        var seatsLayout = seatsGO.GetComponent<SeatsLayoutRadial>();
        if (seatsLayout == null) seatsLayout = seatsGO.AddComponent<SeatsLayoutRadial>();

        // 2) Найти/выбрать актуальный префаб Seat в Assets/Prefabs
        var seatPrefab = EnsureSeatPrefab();
        if (seatPrefab == null) return;

        // 3) Проверить/привязать обязательные поля NewBehaviourScript в самом префабе (по именам children)
        var seatInstance = (GameObject)PrefabUtility.InstantiatePrefab(seatPrefab);
        seatInstance.name = "Seat_TEMP_VALIDATE";
        var ctrl = seatInstance.GetComponent<NewBehaviourScript>();
        if (ctrl == null)
        {
            ctrl = seatInstance.AddComponent<NewBehaviourScript>();
        }
        // попытаться найти стандартные элементы по именам
        TryAssign<Image>(ctrl, "avatarImage", seatInstance.transform.Find("Avatar"));
        TryAssign<Text>(ctrl, "nameText", seatInstance.transform.Find("NameText"));
        TryAssign<Text>(ctrl, "stackText", seatInstance.transform.Find("StackText"));
        if (seatInstance.transform.Find("BetBubble") != null)
        {
            ctrl.GetType().GetField("betBubble", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, seatInstance.transform.Find("BetBubble").gameObject);
            TryAssign<Text>(ctrl, "betText", seatInstance.transform.Find("BetBubble/BetText"));
        }
        // Карманные карты (если нет — создадим)
        var h1 = seatInstance.transform.Find("Hole1");
        var h2 = seatInstance.transform.Find("Hole2");
        if (h1 == null)
        {
            var go = new GameObject("Hole1", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(seatInstance.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40, 56);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-24, 24);
            h1 = go.transform;
        }
        if (h2 == null)
        {
            var go = new GameObject("Hole2", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(seatInstance.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40, 56);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(24, 24);
            h2 = go.transform;
        }
        TryAssign<Image>(ctrl, "hole1Image", h1);
        TryAssign<Image>(ctrl, "hole2Image", h2);

        PrefabUtility.SaveAsPrefabAsset(seatInstance, AssetDatabase.GetAssetPath(seatPrefab));
        Object.DestroyImmediate(seatInstance);

        // 4) Присвоить префаб в SeatsLayoutRadial и пересоздать места
        seatsLayout.GetType().GetField("seatPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(seatsLayout, seatPrefab.GetComponent<RectTransform>());
        // удалить всех текущих детей, затем пересоздать
        seatsLayout.SpawnSeats();

        // 5) Добавить AutoSeatFiller (2 игрока по умолчанию)
        var filler = seatsGO.GetComponent<AutoSeatFiller>();
        if (filler == null) filler = seatsGO.AddComponent<AutoSeatFiller>();
        var fType = typeof(AutoSeatFiller);
        fType.GetField("playersToJoin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(filler, 2);
        fType.GetField("defaultStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(filler, 1000);

        // 6) Найти/связать BoardController и GameStateMachine
        var board = Object.FindObjectOfType<BoardController>();
        var game = Object.FindObjectOfType<GameStateMachine>();
        if (game == null)
        {
            var go = new GameObject("GameManager");
            game = go.AddComponent<GameStateMachine>();
        }
        if (board == null)
        {
            var bgo = GameObject.Find("Board");
            if (bgo != null) board = bgo.GetComponent<BoardController>();
        }
        if (board != null)
        {
            var gType = typeof(GameStateMachine);
            gType.GetField("board", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(game, board);
        }

        // 7) Привязать панель действий (если есть)
        var action = Object.FindObjectOfType<ActionPanelController>();
        if (action != null)
        {
            var aType = typeof(ActionPanelController);
            aType.GetField("game", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(action, game);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Finalize Gameplay: Seat prefab validated, seats respawned, auto-fill set to 2, game wired.");
    }

    private static void TryAssign<T>(object target, string fieldName, Transform tr) where T : Component
    {
        if (tr == null) return;
        var comp = tr.GetComponent<T>();
        if (comp == null) return;
        target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(target, comp);
    }

    private static GameObject EnsureSeatPrefab()
    {
        // Удалим временные сценные экземпляры Seat_*/Seat_TEMP_CONVERT, если они не под контейнером Seats
        var seatsContainer = GameObject.Find("Seats");
        foreach (var go in Object.FindObjectsOfType<RectTransform>())
        {
            if (go.name.StartsWith("Seat_TEMP_CONVERT"))
            {
                Object.DestroyImmediate(go.gameObject);
            }
        }

        // Попробуем найти корректный asset-префаб в папке Prefabs
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Если есть Seat.prefab — берём его
        var seatPath = "Assets/Prefabs/Seat.prefab";
        var seatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(seatPath);
        if (seatPrefab == null)
        {
            // Иначе найдём любой prefab в папке Prefabs, который содержит NewBehaviourScript и детей Hole1/Hole2
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                var ctrl = go.GetComponent<NewBehaviourScript>();
                if (ctrl == null) continue;
                // проверим, есть ли дети с именами Hole1/Hole2
                var hasHoles = go.transform.Find("Hole1") != null && go.transform.Find("Hole2") != null;
                if (hasHoles)
                {
                    // Переименуем в Seat.prefab
                    AssetDatabase.RenameAsset(path, "Seat");
                    seatPrefab = go;
                    AssetDatabase.SaveAssets();
                    break;
                }
            }
        }

        if (seatPrefab == null)
        {
            Debug.LogError("Seat prefab not found. Create it: drag a configured Seat (with Hole1/Hole2) from Hierarchy into Assets/Prefabs and run this command again.");
            return null;
        }

        // Удалим временные префабы Seat_TEMP_CONVERT*
        var tempGuids = AssetDatabase.FindAssets("Seat_TEMP_CONVERT t:Prefab", new[] { "Assets/Prefabs" });
        foreach (var guid in tempGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }

        return seatPrefab;
    }
#endif
}


