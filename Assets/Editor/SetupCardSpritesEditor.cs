using UnityEngine;
using UnityEditor;
using System.Linq;

public class SetupCardSpritesEditor : EditorWindow
{
    [MenuItem("Tools/Poker/Setup Card Sprites")]
    public static void ShowWindow()
    {
        GetWindow<SetupCardSpritesEditor>("Setup Card Sprites");
    }

    private void OnGUI()
    {
        GUILayout.Label("Настройка спрайтов карт", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Создать CardSpritesData"))
        {
            CreateCardSpritesData();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Автозаполнить спрайты из CuteCards"))
        {
            AutoFillCardSprites();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Показать все спрайты CuteCards"))
        {
            ShowAllCuteCardSprites();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Ручная настройка (только карты 0-51)"))
        {
            ManualFillCardSprites();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Инструкции:", EditorStyles.boldLabel);
        GUILayout.Label("1. Нажмите 'Создать CardSpritesData'");
        GUILayout.Label("2. Нажмите 'Автозаполнить спрайты из CuteCards'");
        GUILayout.Label("3. Проверьте созданный файл в Assets/Resources/");
    }

    private void CreateCardSpritesData()
    {
        // Создаем папку Resources если её нет
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // Создаем ScriptableObject
        var cardSpritesData = ScriptableObject.CreateInstance<CardSpritesData>();
        
        // Сохраняем в Resources
        AssetDatabase.CreateAsset(cardSpritesData, "Assets/Resources/CardSpritesData.asset");
        AssetDatabase.SaveAssets();
        
        Debug.Log("CardSpritesData создан в Assets/Resources/CardSpritesData.asset");
        
        // Выделяем созданный файл
        Selection.activeObject = cardSpritesData;
        EditorGUIUtility.PingObject(cardSpritesData);
    }

    private void AutoFillCardSprites()
    {
        // Находим CardSpritesData
        var cardSpritesData = AssetDatabase.LoadAssetAtPath<CardSpritesData>("Assets/Resources/CardSpritesData.asset");
        
        if (cardSpritesData == null)
        {
            Debug.LogError("CardSpritesData не найден. Сначала создайте его.");
            return;
        }

        // Находим все спрайты CuteCards из конкретного файла
        string[] guids = AssetDatabase.FindAssets("CuteCards t:Texture2D", new[] { "Assets/Art" });
        
        if (guids.Length == 0)
        {
            Debug.LogError("Файл CuteCards.png не найден в Assets/Art");
            return;
        }

        string texturePath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        
        var cuteCardSprites = allAssets
            .OfType<Sprite>()
            .Where(sprite => sprite.name.StartsWith("CuteCards_"))
            .OrderBy(sprite => GetSpriteNumber(sprite.name))
            .ToArray();

        Debug.Log($"Найдено {cuteCardSprites.Length} спрайтов CuteCards");

        // Фильтруем только игровые карты (обычно первые 52 спрайта - это карты)
        // Пропускаем рубашки и лишние спрайты
        var gameCardSprites = cuteCardSprites
            .Where(sprite => {
                int num = GetSpriteNumber(sprite.name);
                // Берем спрайты с 0 по 51 (52 карты)
                return num >= 0 && num <= 51;
            })
            .OrderBy(sprite => GetSpriteNumber(sprite.name))
            .ToArray();

        Debug.Log($"Отфильтровано {gameCardSprites.Length} игровых карт");

        if (gameCardSprites.Length < 52)
        {
            Debug.LogError($"Найдено только {gameCardSprites.Length} игровых карт. Нужно 52.");
            
            // Показываем какие спрайты найдены
            for (int i = 0; i < System.Math.Min(10, cuteCardSprites.Length); i++)
            {
                Debug.Log($"Спрайт {i}: {cuteCardSprites[i].name}");
            }
            return;
        }

        // Заполняем массив спрайтов
        for (int i = 0; i < 52 && i < gameCardSprites.Length; i++)
        {
            cardSpritesData.cardSprites[i] = gameCardSprites[i];
        }

        // Ищем рубашку среди оставшихся спрайтов
        var backCardSprites = cuteCardSprites
            .Where(sprite => {
                int num = GetSpriteNumber(sprite.name);
                return num > 51; // Спрайты после 52-й карты
            })
            .ToArray();

        if (backCardSprites.Length > 0)
        {
            cardSpritesData.cardBack = backCardSprites[0]; // Первый найденный спрайт рубашки
            Debug.Log($"Рубашка установлена: {cardSpritesData.cardBack.name}");
        }

        // Сохраняем изменения
        EditorUtility.SetDirty(cardSpritesData);
        AssetDatabase.SaveAssets();

        Debug.Log($"Автозаполнение завершено! Заполнено {52} спрайтов карт.");
        
        // Выделяем файл для проверки
        Selection.activeObject = cardSpritesData;
        EditorGUIUtility.PingObject(cardSpritesData);
    }

    private void ShowAllCuteCardSprites()
    {
        string[] guids = AssetDatabase.FindAssets("CuteCards t:Texture2D", new[] { "Assets/Art" });
        
        if (guids.Length == 0)
        {
            Debug.LogError("Файл CuteCards.png не найден в Assets/Art");
            return;
        }

        string texturePath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        
        var cuteCardSprites = allAssets
            .OfType<Sprite>()
            .Where(sprite => sprite.name.StartsWith("CuteCards_"))
            .OrderBy(sprite => GetSpriteNumber(sprite.name))
            .ToArray();

        Debug.Log($"=== Все спрайты CuteCards ({cuteCardSprites.Length}) ===");
        for (int i = 0; i < cuteCardSprites.Length; i++)
        {
            Debug.Log($"{i}: {cuteCardSprites[i].name}");
        }
    }

    private void ManualFillCardSprites()
    {
        var cardSpritesData = AssetDatabase.LoadAssetAtPath<CardSpritesData>("Assets/Resources/CardSpritesData.asset");
        
        if (cardSpritesData == null)
        {
            Debug.LogError("CardSpritesData не найден. Сначала создайте его.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("CuteCards t:Texture2D", new[] { "Assets/Art" });
        string texturePath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        
        var cuteCardSprites = allAssets
            .OfType<Sprite>()
            .Where(sprite => sprite.name.StartsWith("CuteCards_"))
            .OrderBy(sprite => GetSpriteNumber(sprite.name))
            .ToArray();

        // Заполняем только первые 52 спрайта (CuteCards_0 до CuteCards_51)
        int filled = 0;
        for (int i = 0; i < cuteCardSprites.Length && filled < 52; i++)
        {
            int spriteNum = GetSpriteNumber(cuteCardSprites[i].name);
            if (spriteNum >= 0 && spriteNum <= 51)
            {
                cardSpritesData.cardSprites[filled] = cuteCardSprites[i];
                filled++;
            }
        }

        // Ищем рубашку (обычно это один из последних спрайтов)
        for (int i = cuteCardSprites.Length - 1; i >= 0; i--)
        {
            int spriteNum = GetSpriteNumber(cuteCardSprites[i].name);
            if (spriteNum > 51)
            {
                cardSpritesData.cardBack = cuteCardSprites[i];
                break;
            }
        }

        EditorUtility.SetDirty(cardSpritesData);
        AssetDatabase.SaveAssets();

        Debug.Log($"Ручное заполнение завершено! Заполнено {filled} карт.");
        Selection.activeObject = cardSpritesData;
    }

    private int GetSpriteNumber(string spriteName)
    {
        var parts = spriteName.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[1], out int num))
            return num;
        return 0;
    }
}