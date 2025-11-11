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

    private static readonly Dictionary<string, int> DefaultChipValues = new()
    {
        {"white", 100},
        {"blue", 200},
        {"green", 200},
        {"red", 500},
        {"purple", 500},
        {"black", 1000},
        {"orange", 1000},
        {"yellow", 1000},
        {"pink", 200}
    };

    public bool HasActiveAmount => currentAmount > 0;

    public void InitializeRuntime()
    {
        if (Mathf.Approximately(offset.x, 0f) && Mathf.Approximately(offset.y, -60f))
            offset = Vector2.zero;
        EnsureContainer();
        EnsureChipSet();
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

        int remaining = amount;
        int columnIndex = 0;
        int stackIndex = 0;
        var layout = new List<ChipLayoutEntry>();

        foreach (var chip in chipSet)
        {
            if (chip.value <= 0 || chip.sprite == null)
                continue;

            while (remaining >= chip.value)
            {
                var image = GetImage();
                image.sprite = chip.sprite;
                image.rectTransform.sizeDelta = chipSize;
                layout.Add(new ChipLayoutEntry(
                    image,
                    new Vector2(
                        columnIndex * (chipSize.x + spacing.x),
                        stackIndex * (chipSize.y + spacing.y)),
                    chip.value));

                remaining -= chip.value;
                stackIndex++;
                if (stackIndex >= Mathf.Max(1, chipsPerColumn))
                {
                    stackIndex = 0;
                    columnIndex++;
                }
            }
        }

        if (remaining > 0 && layout.Count > 0)
        {
            var fallbackChip = chipSet.LastOrDefault(c => c.sprite != null);
            if (fallbackChip.sprite != null)
            {
                var image = GetImage();
                image.sprite = fallbackChip.sprite;
                image.rectTransform.sizeDelta = chipSize;
                layout.Add(new ChipLayoutEntry(
                    image,
                    new Vector2(
                        columnIndex * (chipSize.x + spacing.x),
                        stackIndex * (chipSize.y + spacing.y)),
                    fallbackChip.value > 0 ? fallbackChip.value : remaining));
                remaining = 0;
            }
        }

        if (layout.Count == 0)
        {
            Debug.LogWarning($"[{name}] Chip layout empty. Amount={amount}, chipSet={chipSet?.Length ?? 0}");
            Show(false);
            return;
        }

        layout = CompressLayout(layout);

        float totalWidth = 0f;
        foreach (var entry in layout)
            totalWidth = Mathf.Max(totalWidth, entry.position.x);
        float offsetX = totalWidth * 0.5f;

        foreach (var entry in layout)
        {
            var image = entry.image;
            var rt = image.rectTransform;
            rt.SetParent(chipContainer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(entry.position.x - offsetX, entry.position.y);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }

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

    private List<ChipLayoutEntry> CompressLayout(List<ChipLayoutEntry> layout)
    {
        const int maxVisualChips = 16;
        if (layout.Count <= maxVisualChips)
            return layout;

        // Collapse to grid without visual overlap by increasing spacing and stacking
        float colWidth = chipSize.x + spacing.x;
        float rowHeight = chipSize.y + spacing.y;

        for (int i = 0; i < layout.Count; i++)
        {
            int columnIndex = i / chipsPerColumn;
            int rowIndex = i % chipsPerColumn;
            layout[i] = new ChipLayoutEntry(
                layout[i].image,
                new Vector2(columnIndex * colWidth, rowIndex * rowHeight),
                layout[i].value);
        }

        return layout;
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
                if (int.TryParse(part, out var parsed) && parsed > 0)
                    return parsed;
            }
        }

        Debug.LogWarning($"BetChipDisplay: не удалось определить номинал для спрайта '{spriteName}', используем 100");
        return 100;
    }

    private struct ChipLayoutEntry
    {
        public ChipLayoutEntry(Image image, Vector2 position, int value)
        {
            this.image = image;
            this.position = position;
            this.value = value;
        }

        public Image image;
        public Vector2 position;
        public int value;
    }
}
