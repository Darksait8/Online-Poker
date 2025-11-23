using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class CardThemeBuilder : EditorWindow
{
    [MenuItem("Tools/Poker/Создать темы карт из папок")]
    public static void ShowWindow()
    {
        GetWindow<CardThemeBuilder>("Создание тем карт");
    }

    private void OnGUI()
    {
        GUILayout.Label("Создание тем карт", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Создать все темы из папок карт"))
        {
            CreateAllThemes();
        }

        GUILayout.Space(10);
        GUILayout.Label("Инструкции:", EditorStyles.boldLabel);
        GUILayout.Label("1. Убедитесь, что папки 'карты1' и 'карты 2' находятся в Assets/Art/");
        GUILayout.Label("2. Нажмите кнопку выше");
        GUILayout.Label("3. Темы будут созданы в Assets/Resources/CardThemes/");
    }

    private static void CreateAllThemes()
    {
        // Создаем папку Resources/CardThemes если её нет
        if (!AssetDatabase.IsValidFolder("Assets/Resources/CardThemes"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateFolder("Assets/Resources", "CardThemes");
        }

        // Создаем тему для "карты1"
        CreateThemeFromFolder("Assets/Art/карты1", "карты1", "Карты 1");

        // Создаем тему для "карты 2"
        CreateThemeFromFolder("Assets/Art/карты 2", "карты2", "Карты 2");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Все темы карт созданы! Перезагрузите CardThemeService через MainMenuUIController.");
    }

    private static void CreateThemeFromFolder(string folderPath, string themeId, string themeDisplayName)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError($"Папка {folderPath} не найдена!");
            return;
        }

        // Создаем CardSpritesData
        var cardSpritesData = ScriptableObject.CreateInstance<CardSpritesData>();
        
        // Заполняем спрайты карт
        bool success = FillCardSprites(cardSpritesData, folderPath);
        
        if (!success)
        {
            Debug.LogError($"Не удалось заполнить спрайты для темы {themeId}");
            return;
        }

        // Сохраняем в Resources/CardThemes
        string assetPath = $"Assets/Resources/CardThemes/{themeId}.asset";
        AssetDatabase.CreateAsset(cardSpritesData, assetPath);
        EditorUtility.SetDirty(cardSpritesData);

        Debug.Log($"Тема '{themeDisplayName}' создана: {assetPath}");
    }

    private static bool FillCardSprites(CardSpritesData data, string folderPath)
    {
        // Порядок мастей: Clubs (0), Diamonds (1), Hearts (2), Spades (3)
        // Порядок карт в каждой масти: 2, 3, 4, 5, 6, 7, 8, 9, 10, J, Q, K, A

        string[] suitFolders = { "Clubs", "Diamonds", "Hearts", "Spades" };
        string[] suitCodes = { "c", "d", "h", "s" }; // Для карты1
        string[] suitCodes2 = { "C", "D", "H", "S" }; // Для карты 2

        // Определяем формат именования файлов
        bool isTheme1 = folderPath.Contains("карты1");
        string[] codes = isTheme1 ? suitCodes : suitCodes2;

        // Для "карты 2" используем правильные папки
        if (!isTheme1 && folderPath.Contains("карты 2"))
        {
            suitFolders[2] = "Red Hearts"; // Hearts = Red Hearts для карты 2
            // Проверяем, есть ли Spades, если нет - используем Black Hearts как fallback для Spades
            string spadesPath = Path.Combine(folderPath, "Spades").Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(spadesPath))
            {
                // Если нет Spades, используем Black Hearts (они черные, подходят для Spades)
                suitFolders[3] = "Black Hearts";
                codes[3] = "H"; // Используем H для Black Hearts как Spades
                Debug.LogWarning("Папка Spades не найдена, используется Black Hearts для Spades");
            }
            else
            {
                codes[3] = "S"; // Если Spades есть, используем S
            }
        }

        int spriteIndex = 0;
        bool allFound = true;

        for (int suitIndex = 0; suitIndex < 4; suitIndex++)
        {
            string suitFolder = suitFolders[suitIndex];
            string suitPath = Path.Combine(folderPath, suitFolder).Replace('\\', '/');

            if (!AssetDatabase.IsValidFolder(suitPath))
            {
                Debug.LogWarning($"Папка масти не найдена: {suitPath}");
                allFound = false;
                spriteIndex += 13; // Пропускаем 13 карт этой масти
                continue;
            }

            // Загружаем спрайты для этой масти
            for (int rankIndex = 0; rankIndex < 13; rankIndex++)
            {
                Sprite sprite = LoadCardSprite(suitPath, rankIndex, codes[suitIndex], isTheme1);
                if (sprite != null)
                {
                    data.cardSprites[spriteIndex] = sprite;
                }
                else
                {
                    Debug.LogWarning($"Не найден спрайт для масти {suitFolder}, ранг {rankIndex} (индекс {spriteIndex})");
                    allFound = false;
                }
                spriteIndex++;
            }
        }

        // Загружаем рубашку
        LoadCardBack(data, folderPath, isTheme1);

        return allFound;
    }

    private static Sprite LoadCardSprite(string suitFolder, int rankIndex, string suitCode, bool isTheme1)
    {
        // rankIndex: 0=Two, 1=Three, ..., 11=King, 12=Ace
        // Для карты1: 1=Ace, 2=Two, ..., 13=King
        // Для карты 2: 2=Two, 3=Three, ..., A=Ace, J=Jack, Q=Queen, K=King

        string fileName;
        if (isTheme1)
        {
            // Формат: 1c.png (Ace), 2c.png (Two), ..., 13c.png (King)
            int fileNumber = rankIndex + 2; // 2->2, 3->3, ..., 12->13, 13->1 (Ace)
            if (fileNumber == 14) fileNumber = 1; // Ace = 1
            fileName = $"{fileNumber}{suitCode}.png";
        }
        else
        {
            // Формат: C2.png, C3.png, ..., CA.png
            string rankStr;
            if (rankIndex == 12) rankStr = "A"; // Ace
            else if (rankIndex == 9) rankStr = "J"; // Jack
            else if (rankIndex == 10) rankStr = "Q"; // Queen
            else if (rankIndex == 11) rankStr = "K"; // King
            else rankStr = (rankIndex + 2).ToString(); // 2-10

            fileName = $"{suitCode}{rankStr}.png";
        }

        string spritePath = Path.Combine(suitFolder, fileName).Replace('\\', '/');
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        if (sprite == null)
        {
            // Пробуем найти через поиск
            string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName), new[] { suitFolder });
            if (guids.Length > 0)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        return sprite;
    }

    private static void LoadCardBack(CardSpritesData data, string folderPath, bool isTheme1)
    {
        if (isTheme1)
        {
            // Для карты1: используем Back Red.png по умолчанию (красная рубашка стандартна для покера)
            string[] backNames = { "Back Red.png", "Back Blue.png" };
            foreach (string backName in backNames)
            {
                string backPath = Path.Combine(folderPath, backName).Replace('\\', '/');
                Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(backPath);
                if (backSprite != null)
                {
                    data.cardBack = backSprite;
                    Debug.Log($"Рубашка загружена: {backName}");
                    return;
                }
            }
            
            // Если в папке темы не найдено, пробуем загрузить из корня Assets/Art
            string[] globalBackNames = { "Back Red.png", "Back Blue.png" };
            foreach (string backName in globalBackNames)
            {
                string globalBackPath = Path.Combine("Assets/Art", backName).Replace('\\', '/');
                Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(globalBackPath);
                if (backSprite != null)
                {
                    data.cardBack = backSprite;
                    Debug.Log($"Рубашка загружена из Assets/Art: {backName}");
                    return;
                }
            }
        }
        else
        {
            // Для карты 2: ищем в корне папки или используем All cards.png как fallback
            // Можно также поискать в подпапках
            string[] possibleBacks = { "All cards.png" };
            foreach (string backName in possibleBacks)
            {
                string backPath = Path.Combine(folderPath, backName).Replace('\\', '/');
                Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(backPath);
                if (backSprite != null)
                {
                    data.cardBack = backSprite;
                    Debug.Log($"Рубашка загружена: {backName}");
                    return;
                }
            }
            
            // Если не найдено, пробуем загрузить из Assets/Art
            string[] globalBackNames = { "Back Red.png", "Back Blue.png" };
            foreach (string backName in globalBackNames)
            {
                string globalBackPath = Path.Combine("Assets/Art", backName).Replace('\\', '/');
                Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(globalBackPath);
                if (backSprite != null)
                {
                    data.cardBack = backSprite;
                    Debug.Log($"Рубашка загружена из Assets/Art: {backName}");
                    return;
                }
            }
        }

        Debug.LogWarning($"Рубашка не найдена для {folderPath}. Установите её вручную.");
    }
}

