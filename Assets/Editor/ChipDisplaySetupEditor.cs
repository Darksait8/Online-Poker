using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ChipDisplaySetupEditor
{
    private const string MenuPath = "Tools/Poker/Setup Chip Spawners";
    private const string SpriteAssetPath = "Assets/Art/PokerChipsPixel.png";

    [MenuItem(MenuPath)]
    public static void SetupChipDisplays()
    {
        var seats = UnityEngine.Object.FindObjectsOfType<NewBehaviourScript>(true);
        if (seats == null || seats.Length == 0)
        {
            EditorUtility.DisplayDialog("Chip Spawner", "Компоненты NewBehaviourScript не найдены в сцене.", "OK");
            return;
        }

        var chipSprites = LoadChipSprites();
        if (chipSprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Chip Spawner", "Не удалось загрузить спрайты из " + SpriteAssetPath + ". Проверь импорт.", "OK");
            return;
        }

        var visuals = BuildChipVisuals(chipSprites);
        if (visuals.Length == 0)
        {
            EditorUtility.DisplayDialog("Chip Spawner", "Не удалось сопоставить номиналы фишек. Исправьте имена спрайтов и повторите.", "OK");
            return;
        }

        int processed = 0;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        foreach (var seat in seats)
        {
            if (seat == null) continue;

            Undo.RecordObject(seat, "Configure Chip Display");
            var display = seat.GetComponentInChildren<BetChipDisplay>(true);
            if (display == null)
            {
                var go = new GameObject("ChipDisplay", typeof(RectTransform), typeof(CanvasRenderer), typeof(BetChipDisplay));
                Undo.RegisterCreatedObjectUndo(go, "Create Chip Display");
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(seat.transform, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = Vector2.zero;
                display = go.GetComponent<BetChipDisplay>();
            }

            var container = display.EditorEnsureContainer();
            var anchor = seat.BetBubbleRect;
            if (anchor != null)
            {
                display.EditorAssignAnchor(anchor);
                container = display.EditorEnsureContainer();
            }
            ConfigureContainerTransform(container);
            display.EditorAssignChipSet(visuals);
            EditorUtility.SetDirty(display);
            processed++;
        }

        Undo.CollapseUndoOperations(group);

        EditorUtility.DisplayDialog("Chip Spawner", $"Настроено мест: {processed}", "Готово");
    }

    private static void ConfigureContainerTransform(RectTransform container)
    {
        if (container == null)
            return;

        Undo.RecordObject(container, "Configure Chip Container");
        container.anchorMin = container.anchorMax = new Vector2(0.5f, 0.5f);
        container.pivot = new Vector2(0.5f, 0.5f);
        container.sizeDelta = new Vector2(260f, 260f);
        container.localRotation = Quaternion.identity;
        container.localScale = Vector3.one;
    }

    private static List<Sprite> LoadChipSprites()
    {
        var sprites = new List<Sprite>();
        if (!File.Exists(SpriteAssetPath))
            return sprites;

        var assets = AssetDatabase.LoadAllAssetsAtPath(SpriteAssetPath);
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
                sprites.Add(sprite);
        }

        return sprites;
    }

    private static BetChipDisplay.ChipVisual[] BuildChipVisuals(List<Sprite> sprites)
    {
        var visuals = new List<BetChipDisplay.ChipVisual>();
        var keywordMapping = new (string keyword, int value)[]
        {
            ("white", 1),
            ("blue", 5),
            ("red", 10),
            ("green", 25),
            ("black", 100),
            ("purple", 500),
            ("orange", 1000),
            ("yellow", 1000)
        };

        int[] fallbackValues = { 1000, 500, 250, 100, 50, 25, 10, 5, 1 };
        int fallbackIndex = 0;

        foreach (var sprite in sprites)
        {
            var nameLower = sprite.name.ToLowerInvariant();
            int value = 0;

            foreach (var entry in keywordMapping)
            {
                if (nameLower.Contains(entry.keyword))
                {
                    value = entry.value;
                    break;
                }
            }

            if (value == 0)
            {
                if (int.TryParse(new string(nameLower.Where(char.IsDigit).ToArray()), out var parsed))
                    value = parsed;
            }

            if (value == 0)
            {
                value = fallbackValues[Mathf.Min(fallbackIndex, fallbackValues.Length - 1)];
                fallbackIndex++;
            }

            if (value == 0)
                continue;

            visuals.Add(new BetChipDisplay.ChipVisual
            {
                value = value,
                sprite = sprite
            });
        }

        return visuals
            .GroupBy(v => v.value)
            .Select(g => g.First())
            .OrderByDescending(v => v.value)
            .ToArray();
    }
}
