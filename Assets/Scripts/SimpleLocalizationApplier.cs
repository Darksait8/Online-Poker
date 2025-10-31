using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class LocalizedText
{
    public Text legacyText;           // если используешь старый Text
    public TMP_Text tmpText;          // если используешь TMP
    [TextArea] public string russian; // текст на русском
    [TextArea] public string english; // текст на английском
}

public class SimpleLocalizationApplier : MonoBehaviour
{
    [SerializeField] private LocalizedText[] texts;

    private void OnEnable()
    {
        Apply(LocalizationManager.CurrentLanguage);
        LocalizationManager.OnLanguageChanged += Apply;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Apply;
    }

    private void Apply(AppLanguage lang)
    {
        if (texts == null) return;
        foreach (var t in texts)
        {
            if (t == null) continue;
            string value = lang == AppLanguage.Russian ? t.russian : t.english;
            if (t.tmpText != null) t.tmpText.text = value;
            if (t.legacyText != null) t.legacyText.text = value;
        }
    }
}


