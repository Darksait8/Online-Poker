using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CardThemeInfo
{
    public string Id { get; }
    public string DisplayName { get; }
    public CardSpritesData Data { get; }
    public Sprite Preview { get; }

    public CardThemeInfo(string id, string displayName, CardSpritesData data)
    {
        Id = id;
        DisplayName = displayName;
        Data = data;
        Preview = data != null ? data.cardBack : null;
    }
}

public static class CardThemeService
{
    private const string ThemesFolder = "CardThemes";
    private const string ThemePrefsKey = "cardThemeId";

    private static List<CardThemeInfo> cachedThemes;

    public static IReadOnlyList<CardThemeInfo> Themes
    {
        get
        {
            if (cachedThemes == null)
            {
                LoadThemes();
            }
            return cachedThemes;
        }
    }

    public static void ReloadThemes()
    {
        LoadThemes(true);
    }

    public static string GetSavedThemeId()
    {
        string defaultId = Themes.Count > 0 ? Themes[0].Id : "default";
        return PlayerPrefs.GetString(ThemePrefsKey, defaultId);
    }

    public static void ApplyTheme(string themeId)
    {
        if (Themes.Count == 0)
        {
            Debug.LogWarning("CardThemeService: нет доступных тем карт");
            return;
        }

        CardThemeInfo theme = Themes.FirstOrDefault(t => t.Id == themeId);
        if (theme == null)
        {
            theme = Themes[0];
        }

        if (theme.Data != null)
        {
            CardSpriteProvider.SetTheme(theme.Data, theme.Id);
            PlayerPrefs.SetString(ThemePrefsKey, theme.Id);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning($"CardThemeService: тема {theme.Id} не содержит данных карт");
        }
    }

    private static void LoadThemes(bool forceReload = false)
    {
        if (cachedThemes != null && !forceReload)
            return;

        cachedThemes = new List<CardThemeInfo>();

        // Загружаем пользовательские темы из Resources/CardThemes
        CardSpritesData[] resourcesThemes = Resources.LoadAll<CardSpritesData>(ThemesFolder);
        foreach (CardSpritesData data in resourcesThemes)
        {
            if (data == null) continue;
            string id = data.name;
            string display = FormatDisplayName(id);
            cachedThemes.Add(new CardThemeInfo(id, display, data));
        }

        // Добавляем стандартную тему, если она не была загружена
        CardSpritesData defaultData = Resources.Load<CardSpritesData>("CardSpritesData");
        if (defaultData != null && cachedThemes.All(t => t.Data != defaultData))
        {
            cachedThemes.Insert(0, new CardThemeInfo("default", "Классическая", defaultData));
        }

        if (cachedThemes.Count == 0)
        {
            Debug.LogWarning("CardThemeService: не удалось найти ни одной темы карт");
        }
    }

    private static string FormatDisplayName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return "Тема";

        string replaced = rawName.Replace('_', ' ');
        StringBuilder builder = new StringBuilder(replaced.Length);
        bool capitalize = true;
        foreach (char c in replaced)
        {
            if (char.IsWhiteSpace(c))
            {
                capitalize = true;
                builder.Append(c);
            }
            else
            {
                builder.Append(capitalize ? char.ToUpperInvariant(c) : c);
                capitalize = false;
            }
        }
        return builder.ToString();
    }
}
