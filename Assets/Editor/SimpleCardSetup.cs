using UnityEngine;
using UnityEditor;
using System.Linq;

public class SimpleCardSetup : EditorWindow
{
    [MenuItem("Tools/Poker/Simple Card Setup")]
    public static void ShowWindow()
    {
        GetWindow<SimpleCardSetup>("Simple Card Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Простая настройка карт", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Заполнить CardSpritesData напрямую"))
        {
            FillCardSpritesDirectly();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Проверить CardSpritesData"))
        {
            CheckCardSpritesData();
        }
    }

    private void FillCardSpritesDirectly()
    {
        // Находим или создаем CardSpritesData
        var cardSpritesData = AssetDatabase.LoadAssetAtPath<CardSpritesData>("Assets/Resources/CardSpritesData.asset");
        
        if (cardSpritesData == null)
        {
            Debug.LogError("CardSpritesData не найден. Создайте его сначала.");
            return;
        }

        // Находим все спрайты CuteCards
        var allSprites = AssetDatabase.FindAssets("t:Sprite")
            .Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(sprite => sprite != null && sprite.name.StartsWith("CuteCards_"))
            .OrderBy(sprite => {
                var parts = sprite.name.Split('_');
                return parts.Length > 1 && int.TryParse(parts[1], out int num) ? num : 0;
            })
            .ToArray();

        Debug.Log($"Найдено {allSprites.Length} спрайтов CuteCards");

        // Заполняем первые 52 спрайта как карты
        for (int i = 0; i < 52 && i < allSprites.Length; i++)
        {
            cardSpritesData.cardSprites[i] = allSprites[i];
            Debug.Log($"Карта {i}: {allSprites[i].name}");
        }

        // Последний спрайт как рубашка
        if (allSprites.Length > 52)
        {
            cardSpritesData.cardBack = allSprites[allSprites.Length - 1];
            Debug.Log($"Рубашка: {cardSpritesData.cardBack.name}");
        }

        EditorUtility.SetDirty(cardSpritesData);
        AssetDatabase.SaveAssets();

        Debug.Log("Прямое заполнение завершено!");
        Selection.activeObject = cardSpritesData;
    }

    private void CheckCardSpritesData()
    {
        var cardSpritesData = AssetDatabase.LoadAssetAtPath<CardSpritesData>("Assets/Resources/CardSpritesData.asset");
        
        if (cardSpritesData == null)
        {
            Debug.LogError("CardSpritesData не найден!");
            return;
        }

        Debug.Log("=== Проверка CardSpritesData ===");
        
        int filledCount = 0;
        for (int i = 0; i < cardSpritesData.cardSprites.Length; i++)
        {
            if (cardSpritesData.cardSprites[i] != null)
            {
                filledCount++;
                if (i < 5) // Показываем первые 5
                {
                    Debug.Log($"Карта {i}: {cardSpritesData.cardSprites[i].name}");
                }
            }
            else
            {
                Debug.LogWarning($"Карта {i}: NULL");
            }
        }

        Debug.Log($"Заполнено карт: {filledCount}/52");
        Debug.Log($"Рубашка: {(cardSpritesData.cardBack != null ? cardSpritesData.cardBack.name : "NULL")}");
    }
}