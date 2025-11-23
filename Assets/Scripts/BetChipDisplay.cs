using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BetChipDisplay : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private RectTransform chipContainer;
    [SerializeField] private RectTransform anchorTarget;
    [SerializeField] private Vector2 chipSize = new Vector2(44f, 44f);
    [SerializeField] private Vector2 spacing = new Vector2(6f, 6f);
    [SerializeField] private Vector2 offset = new Vector2(0f, -60f);
    [SerializeField] private int chipsPerColumn = 5;

    [Header("Stack layout")]
    [SerializeField] private bool useStackLayout = true;
    [SerializeField] private Vector2 stackStep = new Vector2(2f, 10f);
    [SerializeField] private float stackBaseSpacing = 32f;
    [SerializeField] private float stackTiltPerChip = -1.5f;
    [SerializeField] private float stackFanAngle = 10f;
    [SerializeField] private int maxVisualChips = 28;
    [SerializeField] private bool autoUpgradeDenomination = true;
    [SerializeField] private bool autoAssignChipValues = true;

    [Header("Набор фишек")]
    [SerializeField] private ChipVisual[] chipSet;
    [Header("Привязка к месту игрока")]
    [SerializeField] private float seatOffsetMultiplier = 1f;
    [SerializeField] private float seatExtraOffset = 45f;

    [Serializable]
    public struct ChipVisual
    {
        public int value;
        public Sprite sprite;
    }

    private readonly List<Image> activeImages = new();
    private readonly Stack<Image> pool = new();
    private bool initialized;
    private Canvas rootCanvas;
    private RectTransform resolvedParent;
    private int currentAmount;
    private RectTransform seatRect;
    private Vector2 seatInwardDirection = Vector2.down;
    private float seatPreferredDistance;
    private bool seatConfigAssigned;
    private readonly Dictionary<int, ChipVisual> chipLookup = new();
    private int[] chipValuesAsc = Array.Empty<int>();
    private static readonly HashSet<string> WarnedSpriteNames = new();

    private static readonly Dictionary<string, int> DefaultChipValues = new()
    {
        {"white", 1000},
        {"pink", 2500},
        {"blue", 5000},
        {"green", 10000},
        {"red", 25000},
        {"purple", 50000},
        {"black", 100000},
        {"orange", 250000},
        {"yellow", 500000}
    };

    private static readonly Dictionary<int, int> DefaultChipIndexValues = new()
    {
        {0, 1000},
        {1, 2500},
        {2, 5000},
        {3, 10000},
        {4, 25000},
        {5, 50000},
        {6, 100000},
        {7, 250000},
        {8, 500000}
    };

    public bool HasActiveAmount => currentAmount > 0;

    public void InitializeRuntime()
    {
        if (Mathf.Approximately(offset.x, 0f) && Mathf.Approximately(offset.y, -60f))
            offset = Vector2.zero;
        EnsureContainer();
        EnsureChipSet();
        if (autoAssignChipValues)
            RefreshChipValuesFromSprites();
        SortChipDefinitions();
        Show(false);
        PositionContainer();
        initialized = true;
        currentAmount = 0;
    }

    public void SetAmount(int amount)
    {
        EnsureInitialized();
        Debug.Log($"[{name}] BetChipDisplay: SetAmount({amount})");
        currentAmount = amount;
        if (chipContainer == null)
            return;

        Clear();

        if (amount <= 0 || chipSet == null || chipSet.Length == 0)
        {
            Show(false);
            Debug.Log($"[{name}] BetChipDisplay: amount <= 0 or no chipSet");
            currentAmount = 0;
            return;
        }

        Show(true);

        var layout = BuildChipLayout(amount);

        if (layout.Count == 0)
        {
            Debug.LogWarning($"[{name}] Chip layout empty. Amount={amount}, chipSet={chipSet?.Length ?? 0}");
            Show(false);
            return;
        }

        ApplyLayout(layout);
        RenderLayout(layout);

        chipContainer.SetAsLastSibling();
        PositionContainer();
        Debug.Log($"[{name}] BetChipDisplay: layoutCount={layout.Count}, containerActive={chipContainer.gameObject.activeSelf}");
    }

    public void Show(bool show)
    {
        EnsureContainer();
        if (chipContainer != null)
        {
            if (!show && currentAmount > 0)
            {
                Debug.Log($"[{name}] BetChipDisplay: ignoring Show(false) due to active amount {currentAmount}");
                return;
            }
            chipContainer.gameObject.SetActive(show);
            if (show)
            {
                chipContainer.SetAsLastSibling();
                PositionContainer();
            }
        }
        if (!show)
            Clear();
        Debug.Log($"[{name}] BetChipDisplay: Show({show})");
    }

    public void Reposition()
    {
        EnsureContainer();
        PositionContainer();
    }

    private void EnsureInitialized()
    {
        if (!initialized)
            InitializeRuntime();
    }

    private void EnsureContainer()
    {
        if (chipContainer == null)
        {
            var go = new GameObject("ChipContainer", typeof(RectTransform));
            chipContainer = go.GetComponent<RectTransform>();
        }

        ResolveParent();

        RectTransform preferredParent = anchorTarget != null ? anchorTarget :
            seatRect != null ? seatRect : resolvedParent;

        if (preferredParent != null && chipContainer.parent != preferredParent)
            chipContainer.SetParent(preferredParent, false);

        chipContainer.anchorMin = chipContainer.anchorMax = new Vector2(0.5f, 0.5f);
        chipContainer.pivot = new Vector2(0.5f, 0.5f);
        chipContainer.sizeDelta = Vector2.zero;
        chipContainer.localRotation = Quaternion.identity;
        chipContainer.localScale = Vector3.one;

        if (!chipContainer.TryGetComponent<LayoutElement>(out var layoutElement))
            layoutElement = chipContainer.gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        PositionContainer();
    }

    private void SortChipDefinitions()
    {
        if (chipSet == null || chipSet.Length == 0)
            return;
        Array.Sort(chipSet, (a, b) => b.value.CompareTo(a.value));

        chipLookup.Clear();
        var values = new List<int>();
        foreach (var chip in chipSet)
        {
            if (chip.value <= 0 || chip.sprite == null)
                continue;
            if (chipLookup.ContainsKey(chip.value))
                continue;

            chipLookup[chip.value] = chip;
            values.Add(chip.value);
        }

        chipValuesAsc = values.OrderBy(v => v).ToArray();
    }

    private void RefreshChipValuesFromSprites()
    {
        if (chipSet == null || chipSet.Length == 0)
            return;

        bool changed = false;
        for (int i = 0; i < chipSet.Length; i++)
        {
            var sprite = chipSet[i].sprite;
            if (sprite == null)
                continue;

            int resolved = ResolveDefaultValue(sprite.name);
            if (resolved <= 0)
                continue;

            if (chipSet[i].value != resolved)
            {
                chipSet[i].value = resolved;
                changed = true;
            }
        }

        if (changed)
            SortChipDefinitions();
    }

    private Image GetImage()
    {
        Image image;
        if (pool.Count > 0)
        {
            image = pool.Pop();
            image.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("BetChip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            image = go.GetComponent<Image>();
        }

        activeImages.Add(image);
        return image;
    }

    private void Clear()
    {
        for (int i = 0; i < activeImages.Count; i++)
        {
            var img = activeImages[i];
            if (img == null) continue;
            img.gameObject.SetActive(false);
            pool.Push(img);
        }
        activeImages.Clear();
    }

    private List<ChipLayoutEntry> BuildChipLayout(int amount)
    {
        var distribution = BuildChipDistribution(amount);
        if (distribution.Count == 0)
            return new List<ChipLayoutEntry>();

        if (autoUpgradeDenomination)
            OptimizeDistribution(distribution);

        var entries = BuildEntriesFromDistribution(distribution);
        if (entries.Count > maxVisualChips)
            return BuildAggregateLayout(amount);

        return entries;
    }

    private Dictionary<int, int> BuildChipDistribution(int amount)
    {
        var distribution = new Dictionary<int, int>();
        if (chipSet == null || chipSet.Length == 0)
            return distribution;

        int remaining = amount;

        foreach (var chip in chipSet)
        {
            if (chip.value <= 0 || chip.sprite == null)
                continue;

            int count = remaining / chip.value;
            if (count <= 0)
                continue;

            if (!distribution.ContainsKey(chip.value))
                distribution[chip.value] = 0;
            distribution[chip.value] += count;
            remaining -= count * chip.value;
        }

        if (remaining > 0)
        {
            var smallest = GetSmallestChip();
            if (smallest.sprite != null && smallest.value > 0)
            {
                if (!distribution.ContainsKey(smallest.value))
                    distribution[smallest.value] = 0;
                distribution[smallest.value] += 1;
            }
        }

        return distribution;
    }

    private void OptimizeDistribution(Dictionary<int, int> distribution)
    {
        if (distribution == null || distribution.Count == 0)
            return;

        int limit = Mathf.Max(1, maxVisualChips);
        int total = distribution.Values.Sum();
        if (total <= limit)
            return;

        bool modified;
        int safety = 256;
        do
        {
            modified = TryPromoteOnce(distribution);
            total = distribution.Values.Sum();
            safety--;
        } while (modified && total > limit && safety > 0);
    }

    private bool TryPromoteOnce(Dictionary<int, int> distribution)
    {
        if (chipValuesAsc == null || chipValuesAsc.Length < 2)
            return false;

        for (int i = 0; i < chipValuesAsc.Length - 1; i++)
        {
            int fromValue = chipValuesAsc[i];
            int toValue = chipValuesAsc[i + 1];

            if (!distribution.TryGetValue(fromValue, out var count) || count <= 0)
                continue;
            if (!chipLookup.ContainsKey(toValue))
                continue;
            if (toValue % fromValue != 0)
                continue;

            int needed = toValue / fromValue;
            if (needed <= 1 || count < needed)
                continue;

            distribution[fromValue] = count - needed;
            if (distribution[fromValue] <= 0)
                distribution.Remove(fromValue);

            if (!distribution.ContainsKey(toValue))
                distribution[toValue] = 0;
            distribution[toValue] += 1;
            return true;
        }

        return false;
    }

    private void ApplyLayout(List<ChipLayoutEntry> layout)
    {
        if (layout == null || layout.Count == 0)
            return;

        if (useStackLayout)
            ApplyStackLayout(layout);
        else
            ApplyGridLayout(layout);
    }

    private List<ChipLayoutEntry> BuildEntriesFromDistribution(Dictionary<int, int> distribution)
    {
        var orderedValues = distribution.Keys.OrderByDescending(v => v).ToArray();
        var entries = new List<ChipLayoutEntry>(distribution.Values.Sum());

        foreach (var value in orderedValues)
        {
            if (!distribution.TryGetValue(value, out var count) || count <= 0)
                continue;

            var visual = ResolveChipVisual(value);
            if (visual.sprite == null)
                continue;

            for (int i = 0; i < count; i++)
            {
                var image = GetImage();
                image.sprite = visual.sprite;
                image.rectTransform.sizeDelta = chipSize;
                entries.Add(new ChipLayoutEntry(image, Vector2.zero, value, 0f));
            }
        }

        return entries;
    }

    private List<ChipLayoutEntry> BuildAggregateLayout(int amount)
    {
        var entries = new List<ChipLayoutEntry>();
        if (chipSet == null || chipSet.Length == 0)
            return entries;

        int safeLimit = Mathf.Max(1, maxVisualChips);
        int desiredValue = Mathf.Max(1, Mathf.CeilToInt(amount / (float)safeLimit));
        int chipValue = ResolveNearestChipValue(desiredValue);
        var visual = ResolveChipVisual(chipValue);
        if (visual.sprite == null)
            visual = chipSet.FirstOrDefault(c => c.sprite != null);
        if (visual.sprite == null)
            return entries;

        chipValue = Mathf.Max(1, visual.value > 0 ? visual.value : chipValue);
        int chipCount = Mathf.Max(1, Mathf.CeilToInt(amount / (float)chipValue));
        int remaining = amount;

        for (int i = 0; i < chipCount; i++)
        {
            int chipsLeft = chipCount - i - 1;
            int minReserved = chipsLeft * chipValue;
            int valueForChip = Mathf.Max(1, remaining - minReserved);
            remaining -= valueForChip;

            var image = GetImage();
            image.sprite = visual.sprite;
            image.rectTransform.sizeDelta = chipSize;
            entries.Add(new ChipLayoutEntry(image, Vector2.zero, valueForChip, 0f));
        }

        return entries;
    }

    private void ApplyGridLayout(List<ChipLayoutEntry> layout)
    {
        int columnIndex = 0;
        int rowIndex = 0;
        int columnLimit = Mathf.Max(1, chipsPerColumn);

        for (int i = 0; i < layout.Count; i++)
        {
            var entry = layout[i];
            entry.position = new Vector2(
                columnIndex * (chipSize.x + spacing.x),
                rowIndex * (chipSize.y + spacing.y));
            entry.rotation = 0f;
            layout[i] = entry;

            rowIndex++;
            if (rowIndex >= columnLimit)
            {
                rowIndex = 0;
                columnIndex++;
            }
        }

        CenterLayout(layout);
    }

    private void ApplyStackLayout(List<ChipLayoutEntry> layout)
    {
        int stackHeight = Mathf.Max(1, chipsPerColumn);
        int stackCount = Mathf.CeilToInt(layout.Count / (float)stackHeight);
        float totalWidth = Mathf.Max(0f, (stackCount - 1) * stackBaseSpacing);

        for (int i = 0; i < layout.Count; i++)
        {
            int stackIndex = i / stackHeight;
            int levelIndex = i % stackHeight;

            float baseX = -totalWidth * 0.5f + stackIndex * stackBaseSpacing;
            float horizontalDrift = levelIndex * stackStep.x;

            float fanFactor = stackCount > 1 ? stackIndex / (float)(stackCount - 1) : 0.5f;
            float rotation = Mathf.Lerp(-stackFanAngle, stackFanAngle, fanFactor) + levelIndex * stackTiltPerChip;

            var entry = layout[i];
            entry.position = new Vector2(baseX + horizontalDrift, levelIndex * stackStep.y);
            entry.rotation = rotation;
            layout[i] = entry;
        }
    }

    private void CenterLayout(List<ChipLayoutEntry> layout)
    {
        if (layout == null || layout.Count == 0)
            return;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        for (int i = 0; i < layout.Count; i++)
        {
            minX = Mathf.Min(minX, layout[i].position.x);
            maxX = Mathf.Max(maxX, layout[i].position.x);
        }

        float offsetX = (minX + maxX) * 0.5f;

        for (int i = 0; i < layout.Count; i++)
        {
            var entry = layout[i];
            var pos = entry.position;
            pos.x -= offsetX;
            entry.position = pos;
            layout[i] = entry;
        }
    }

    private void RenderLayout(List<ChipLayoutEntry> layout)
    {
        if (layout == null || layout.Count == 0)
            return;

        var ordered = layout
            .OrderBy(entry => entry.position.y)
            .ThenBy(entry => entry.position.x)
            .ToList();

        foreach (var entry in ordered)
        {
            var image = entry.image;
            if (image == null)
                continue;

            var rt = image.rectTransform;
            rt.SetParent(chipContainer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = entry.position;
            rt.localRotation = Quaternion.Euler(0f, 0f, entry.rotation);
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }
    }

    private void PositionContainer()
    {
        if (chipContainer == null)
            return;

        ResolveParent();

        if (seatRect != null && seatConfigAssigned)
        {
            if (chipContainer.parent != seatRect)
                chipContainer.SetParent(seatRect, false);

            float baseDistance = seatPreferredDistance > 0f ? seatPreferredDistance : (seatRect.rect.height * 0.5f);
            float chipDistance = baseDistance * seatOffsetMultiplier + seatExtraOffset + (chipSize.y * 0.5f);
            Vector2 localOffset = seatInwardDirection * chipDistance + offset;
            chipContainer.anchoredPosition = localOffset;
            chipContainer.localRotation = Quaternion.identity;
            return;
        }

        RectTransform target = anchorTarget != null ? anchorTarget : transform as RectTransform;
        if (target == null)
            return;

        if (resolvedParent == null || rootCanvas == null)
            return;

        if (chipContainer.parent != resolvedParent)
            chipContainer.SetParent(resolvedParent, false);
        chipContainer.SetAsLastSibling();

        Camera eventCamera = null;
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera || rootCanvas.renderMode == RenderMode.WorldSpace)
            eventCamera = rootCanvas.worldCamera;

        Vector2 finalOffset = offset;
        if (anchorTarget != null)
        {
            float downward = (anchorTarget.rect.height * 0.5f) + (chipSize.y * 0.5f) + 12f;
            finalOffset += new Vector2(0f, -downward);
        }

        Vector3 worldPoint = target.TransformPoint(new Vector3(finalOffset.x, finalOffset.y, 0f));
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPoint);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(resolvedParent, screenPoint, eventCamera, out var localPoint))
        {
            chipContainer.anchoredPosition = localPoint;
            chipContainer.localRotation = Quaternion.identity;
            Debug.Log($"[{name}] BetChipDisplay: PositionContainer -> anchored={chipContainer.anchoredPosition}");
        }
    }

    private void ResolveParent()
    {
        if (resolvedParent != null && rootCanvas != null)
            return;

        Canvas candidateCanvas = null;

        if (anchorTarget != null)
        {
            resolvedParent = anchorTarget;
            candidateCanvas = anchorTarget.GetComponentInParent<Canvas>();
        }
        if (candidateCanvas == null)
            candidateCanvas = GetComponentInParent<Canvas>();

        if (candidateCanvas != null)
            rootCanvas = candidateCanvas.rootCanvas;

        if (rootCanvas == null)
        {
            var canvases = FindObjectsOfType<Canvas>();
            if (canvases.Length > 0)
                rootCanvas = canvases[0].rootCanvas;
        }

        if (rootCanvas != null)
        {
            resolvedParent = rootCanvas.transform as RectTransform;
        }
        else if (anchorTarget != null)
        {
            resolvedParent = anchorTarget.transform as RectTransform;
        }
        else
        {
            resolvedParent = transform as RectTransform;
        }
    }

    private ChipVisual ResolveChipVisual(int value)
    {
        if (chipLookup != null && chipLookup.TryGetValue(value, out var visual) && visual.sprite != null)
            return visual;

        foreach (var chip in chipSet)
        {
            if (chip.value == value && chip.sprite != null)
                return chip;
        }

        return chipSet.FirstOrDefault(c => c.sprite != null);
    }

    private int ResolveNearestChipValue(int minValue)
    {
        if (chipValuesAsc == null || chipValuesAsc.Length == 0)
            return minValue;

        for (int i = 0; i < chipValuesAsc.Length; i++)
        {
            if (chipValuesAsc[i] >= minValue)
                return chipValuesAsc[i];
        }

        return chipValuesAsc[chipValuesAsc.Length - 1];
    }

    private ChipVisual GetSmallestChip()
    {
        if (chipSet == null || chipSet.Length == 0)
            return default;

        for (int i = chipSet.Length - 1; i >= 0; i--)
        {
            if (chipSet[i].sprite != null && chipSet[i].value > 0)
                return chipSet[i];
        }

        return default;
    }

        public void EnsureChipSet()
        {
            if (!NeedsChipSet())
            {
                SortChipDefinitions();
                return;
            }

#if UNITY_EDITOR
            if (TryLoadChipSetFromAsset())
            {
                SortChipDefinitions();
                return;
            }
#endif

            if (TryLoadChipSetFromResources())
            {
                SortChipDefinitions();
                return;
            }

            Debug.LogWarning($"[{name}] BetChipDisplay: не удалось автоматически загрузить спрайты фишек. " +
                             $"Назначьте массив chipSet вручную или разместите спрайты в Resources/Art/PokerChipsPixel.");
        }

        private bool NeedsChipSet()
        {
            if (chipSet == null || chipSet.Length == 0)
                return true;
            for (int i = 0; i < chipSet.Length; i++)
            {
                if (chipSet[i].sprite == null || chipSet[i].value <= 0)
                    return true;
            }
            return false;
        }

        private bool TryLoadChipSetFromResources()
        {
            var sprites = Resources.LoadAll<Sprite>("Art/PokerChipsPixel");
            if (sprites == null || sprites.Length == 0)
                sprites = Resources.LoadAll<Sprite>("PokerChipsPixel");

            if (sprites == null || sprites.Length == 0)
                return false;

            chipSet = sprites
                .Where(s => s != null)
                .Select(s => new ChipVisual
                {
                    sprite = s,
                    value = ResolveDefaultValue(s.name)
                })
                .Where(v => v.value > 0 && v.sprite != null)
                .OrderByDescending(v => v.value)
                .ToArray();

            return chipSet != null && chipSet.Length > 0;
        }

#if UNITY_EDITOR
        private bool TryLoadChipSetFromAsset()
        {
            const string assetPath = "Assets/Art/PokerChipsPixel.png";
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null || assets.Length == 0)
                return false;

            var sprites = assets.OfType<Sprite>().ToList();
            if (sprites.Count == 0)
                return false;

            chipSet = sprites
                .Where(s => s != null)
                .Select(s => new ChipVisual
                {
                    sprite = s,
                    value = ResolveDefaultValue(s.name)
                })
                .Where(v => v.value > 0 && v.sprite != null)
                .OrderByDescending(v => v.value)
                .ToArray();

            UnityEditor.EditorUtility.SetDirty(this);
            return chipSet != null && chipSet.Length > 0;
        }
#endif

    public void SetAnchorTarget(RectTransform target)
    {
        anchorTarget = target;
        resolvedParent = null;
        if (initialized)
            PositionContainer();
    }

    public void ConfigureSeatAnchor(RectTransform seat, Vector2 inwardDirection, float preferredDistance)
    {
        seatRect = seat;
        if (inwardDirection.sqrMagnitude > 0.0001f)
            seatInwardDirection = inwardDirection.normalized;
        seatPreferredDistance = preferredDistance;
        seatConfigAssigned = seat != null;
        resolvedParent = null;
        if (initialized)
            PositionContainer();
    }

#if UNITY_EDITOR
    public void EditorAssignChipSet(ChipVisual[] visuals)
    {
        chipSet = visuals;
        SortChipDefinitions();
        UnityEditor.EditorUtility.SetDirty(this);
        currentAmount = 0;
    }

    public RectTransform EditorEnsureContainer()
    {
        EnsureContainer();
        UnityEditor.EditorUtility.SetDirty(this);
        return chipContainer;
    }

    public void EditorAssignAnchor(RectTransform anchor)
    {
        anchorTarget = anchor;
        resolvedParent = null;
        PositionContainer();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private static int ResolveDefaultValue(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return 1;

        var lower = spriteName.ToLowerInvariant();
        foreach (var pair in DefaultChipValues)
        {
            if (lower.Contains(pair.Key))
                return pair.Value;
        }

        if (lower.Contains("_"))
        {
            var parts = lower.Split('_');
            foreach (var part in parts.Reverse())
            {
                if (int.TryParse(part, out var indexValue) && DefaultChipIndexValues.TryGetValue(indexValue, out var mapped))
                    return mapped;
                if (int.TryParse(part, out var parsed) && parsed > 0)
                    return parsed;
            }
        }

        if (WarnedSpriteNames.Add(spriteName))
            Debug.LogWarning($"BetChipDisplay: не удалось определить номинал для спрайта '{spriteName}', используем 1000");
        return 1000;
    }

    private struct ChipLayoutEntry
    {
        public ChipLayoutEntry(Image image, Vector2 position, int value, float rotation)
        {
            this.image = image;
            this.position = position;
            this.value = value;
            this.rotation = rotation;
        }

        public Image image;
        public Vector2 position;
        public float rotation;
        public int value;
    }
}
