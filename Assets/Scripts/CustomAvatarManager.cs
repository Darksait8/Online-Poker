using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CustomAvatarManager
{
    public const string CustomAvatarId = UserProfile.CustomAvatarId;

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

    public static Sprite GetAvatarSprite(UserProfile profile)
    {
        if (profile == null)
            return AvatarLibrary.GetAvatarSprite("default");

        if (profile.avatarId == CustomAvatarId)
        {
            Sprite customSprite = LoadSprite(profile.customAvatarPath);
            return customSprite ?? AvatarLibrary.GetAvatarSprite("default");
        }

        return AvatarLibrary.GetAvatarSprite(profile.avatarId);
    }

    public static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[CustomAvatarManager] Файл аватара не найден: {path}");
            return null;
        }

        if (spriteCache.TryGetValue(path, out Sprite cached) && cached != null)
            return cached;

        try
        {
            byte[] data = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogWarning($"[CustomAvatarManager] LoadImage вернул false для {path}");
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = Path.GetFileNameWithoutExtension(path);

            spriteCache[path] = sprite;
            textureCache[path] = texture;
            Debug.Log($"[CustomAvatarManager] Загружен аватар из файла: {path}");
            return sprite;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Не удалось загрузить аватар из файла: {e.Message}");
            return null;
        }
    }

    public static string ImportAvatar(string username, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(sourcePath))
            return null;
        if (!File.Exists(sourcePath))
            return null;

        try
        {
            byte[] data = File.ReadAllBytes(sourcePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogWarning($"[CustomAvatarManager] Не удалось распарсить изображение {sourcePath}");
                return null;
            }

            string directory = UserDataManager.GetUserAvatarDirectory(username);
            if (string.IsNullOrEmpty(directory))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogWarning("[CustomAvatarManager] Не удалось получить директорию для аватаров.");
                return null;
            }

            string fileName = $"avatar_{Guid.NewGuid():N}.png";
            string destinationPath = Path.Combine(directory, fileName);
            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(destinationPath, png);

            UnityEngine.Object.Destroy(texture);

            ReleaseSprite(destinationPath);
            Debug.Log($"[CustomAvatarManager] Аватар импортирован в {destinationPath}");
            return destinationPath;
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка импорта аватара: {e.Message}");
            return null;
        }
    }

    public static bool TryCreatePreview(string sourcePath, out Sprite sprite, out Texture2D texture)
    {
        sprite = null;
        texture = null;

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return false;

        try
        {
            byte[] data = File.ReadAllBytes(sourcePath);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data))
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
                Debug.LogWarning($"[CustomAvatarManager] Не удалось распарсить изображение {sourcePath} для превью");
                return false;
            }

            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = Path.GetFileNameWithoutExtension(sourcePath) + "_preview";
            Debug.Log($"[CustomAvatarManager] Создан превью аватар: {sourcePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Не удалось создать предварительный просмотр аватара: {e.Message}");
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
            return false;
        }
    }

    public static void ReleaseSprite(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (spriteCache.TryGetValue(path, out Sprite sprite) && sprite != null)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(sprite);
            else
                UnityEngine.Object.DestroyImmediate(sprite);
        }
        spriteCache.Remove(path);

        if (textureCache.TryGetValue(path, out Texture2D texture) && texture != null)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(texture);
            else
                UnityEngine.Object.DestroyImmediate(texture);
        }
        textureCache.Remove(path);
    }
}

