using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class AvatarInfo
{
    public string Id { get; }
    public string DisplayName { get; }
    public Sprite Sprite { get; }

    public AvatarInfo(string id, string displayName, Sprite sprite)
    {
        Id = id;
        DisplayName = displayName;
        Sprite = sprite;
    }
}

public static class AvatarLibrary
{
    private const string Folder = "Avatars";
    private static List<AvatarInfo> cachedAvatars;
    private static Sprite fallbackSprite;

    public static IReadOnlyList<AvatarInfo> Avatars
    {
        get
        {
            if (cachedAvatars == null)
            {
                LoadAvatars();
            }
            return cachedAvatars;
        }
    }

    public static void Reload()
    {
        LoadAvatars(true);
    }

    public static Sprite GetAvatarSprite(string avatarId)
    {
        if (string.IsNullOrWhiteSpace(avatarId))
            return GetFallbackSprite();

        AvatarInfo info = Avatars.FirstOrDefault(a => a.Id == avatarId);
        return info?.Sprite ?? GetFallbackSprite();
    }

    private static void LoadAvatars(bool forceReload = false)
    {
        if (cachedAvatars != null && !forceReload)
            return;

        cachedAvatars = new List<AvatarInfo>();
        Sprite[] sprites = Resources.LoadAll<Sprite>(Folder);
        foreach (Sprite sprite in sprites)
        {
            if (sprite == null) continue;
            string id = sprite.name;
            string display = FormatDisplayName(id);
            cachedAvatars.Add(new AvatarInfo(id, display, sprite));
        }

        if (cachedAvatars.All(a => a.Id != "default"))
        {
            cachedAvatars.Insert(0, new AvatarInfo("default", "По умолчанию", GetFallbackSprite()));
        }
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
            return fallbackSprite;

        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color fill = new Color(0.15f, 0.6f, 0.3f, 1f);
        Color[] colors = Enumerable.Repeat(fill, 64 * 64).ToArray();
        texture.SetPixels(colors);
        texture.Apply();

        fallbackSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        fallbackSprite.name = "fallback";
        return fallbackSprite;
    }

    private static string FormatDisplayName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return "Аватар";

        string replaced = rawName.Replace('_', ' ');
        StringBuilder builder = new StringBuilder(replaced.Length);
        bool capitalize = true;
        foreach (char c in replaced)
        {
            if (char.IsWhiteSpace(c))
            {
                builder.Append(c);
                capitalize = true;
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
