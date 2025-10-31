using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class LocalizationEntry
{
    public string key;        // имя объекта (или родителя) с текстом
    [TextArea] public string russian;
    [TextArea] public string english;
}

public class AutoLocalizationByName : MonoBehaviour
{
    [SerializeField] private List<LocalizationEntry> entries = new List<LocalizationEntry>();
    [SerializeField] private bool matchByParentIfChild = true; // если текст — дочерний объект кнопки

    private Dictionary<string, LocalizationEntry> map;

    private void OnEnable()
    {
        BuildMap();
        Apply(LocalizationManager.CurrentLanguage);
        LocalizationManager.OnLanguageChanged += Apply;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Apply;
    }

    private void BuildMap()
    {
        map = new Dictionary<string, LocalizationEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            map[e.key] = e;
        }
    }

    private void Apply(AppLanguage lang)
    {
        if (map == null || map.Count == 0) BuildMap();

        var texts = GetComponentsInChildren<Text>(true);
        foreach (var t in texts)
        {
            string key = t.gameObject.name;
            if (matchByParentIfChild && t.transform.parent != null)
                key = map.ContainsKey(key) ? key : t.transform.parent.name;
            if (map.TryGetValue(key, out var e))
                t.text = lang == AppLanguage.Russian ? e.russian : e.english;
        }

        var tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps)
        {
            string key = t.gameObject.name;
            if (matchByParentIfChild && t.transform.parent != null)
                key = map.ContainsKey(key) ? key : t.transform.parent.name;
            if (map.TryGetValue(key, out var e))
                t.text = lang == AppLanguage.Russian ? e.russian : e.english;
        }
    }

    public void SetEntries(List<LocalizationEntry> newEntries)
    {
        entries = newEntries;
        BuildMap();
        Apply(LocalizationManager.CurrentLanguage);
    }
}


