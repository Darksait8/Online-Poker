using UnityEngine;
using UnityEditor;
using System.IO;

public class CardBackChanger : EditorWindow
{
    private CardSpritesData targetData;
    private Sprite selectedBack;

    [MenuItem("Tools/Poker/Изменить рубашку карт")]
    public static void ShowWindow()
    {
        GetWindow<CardBackChanger>("Смена рубашки карт");
    }

    private void OnGUI()
    {
        GUILayout.Label("Изменение рубашки карт", EditorStyles.boldLabel);
        GUILayout.Space(10);

        targetData = EditorGUILayout.ObjectField("Card Sprites Data:", targetData, typeof(CardSpritesData), false) as CardSpritesData;

        GUILayout.Space(10);

        if (targetData != null)
        {
            GUILayout.Label("Текущая рубашка:", EditorStyles.boldLabel);
            if (targetData.cardBack != null)
            {
                EditorGUILayout.ObjectField(targetData.cardBack, typeof(Sprite), false);
            }
            else
            {
                GUILayout.Label("Рубашка не установлена", EditorStyles.helpBox);
            }

            GUILayout.Space(10);

            GUILayout.Label("Новая рубашка:", EditorStyles.boldLabel);
            selectedBack = EditorGUILayout.ObjectField(selectedBack, typeof(Sprite), false) as Sprite;

            GUILayout.Space(10);

            if (GUILayout.Button("Применить красную рубашку (Back Red.png)"))
            {
                LoadAndApplyBack("Back Red.png");
            }

            if (GUILayout.Button("Применить синюю рубашку (Back Blue.png)"))
            {
                LoadAndApplyBack("Back Blue.png");
            }

            GUILayout.Space(10);

            if (selectedBack != null && GUILayout.Button("Применить выбранную рубашку"))
            {
                ApplyBack(selectedBack);
            }
        }
        else
        {
            GUILayout.Label("Выберите Card Sprites Data для изменения", EditorStyles.helpBox);
        }
    }

    private void LoadAndApplyBack(string backFileName)
    {
        if (targetData == null)
        {
            Debug.LogError("Card Sprites Data не выбран!");
            return;
        }

        // Ищем в папке темы (если это тема из CardThemes)
        string assetPath = AssetDatabase.GetAssetPath(targetData);
        string themeFolder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        
        // Пробуем найти в папке темы
        string backPath = Path.Combine(themeFolder, backFileName).Replace('\\', '/');
        Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(backPath);

        // Если не найдено в папке темы, пробуем в Assets/Art
        if (backSprite == null)
        {
            string artBackPath = Path.Combine("Assets/Art", backFileName).Replace('\\', '/');
            backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artBackPath);
        }

        // Если не найдено, пробуем в папке карты1
        if (backSprite == null)
        {
            string cards1BackPath = Path.Combine("Assets/Art/карты1", backFileName).Replace('\\', '/');
            backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(cards1BackPath);
        }

        if (backSprite != null)
        {
            ApplyBack(backSprite);
            Debug.Log($"Рубашка {backFileName} применена к {targetData.name}");
        }
        else
        {
            Debug.LogError($"Рубашка {backFileName} не найдена!");
        }
    }

    private void ApplyBack(Sprite backSprite)
    {
        if (targetData == null || backSprite == null)
        {
            Debug.LogError("Не выбран Card Sprites Data или рубашка!");
            return;
        }

        targetData.cardBack = backSprite;
        EditorUtility.SetDirty(targetData);
        AssetDatabase.SaveAssets();
        Debug.Log($"Рубашка {backSprite.name} применена к {targetData.name}");
    }
}

