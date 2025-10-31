using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.Collections.Generic;

public static class SetupLocalizationEditor
{
#if UNITY_EDITOR
    [MenuItem("Tools/Poker/Setup Localization (MainMenu)")]
    public static void SetupMainMenuLocalization()
    {
        // Ищем корневой объект сценового главного меню
        var mainMenu = GameObject.Find("MainMenu");
        if (mainMenu == null)
        {
            Debug.LogError("MainMenu root not found. Create MainMenu object in the scene.");
            return;
        }

        var loc = mainMenu.GetComponent<AutoLocalizationByName>();
        if (loc == null) loc = mainMenu.AddComponent<AutoLocalizationByName>();

        // Предзаполнение стандартных кнопок по именам
        var entries = new List<LocalizationEntry>
        {
            new LocalizationEntry{ key = "PlayButton", russian = "Играть", english = "Play" },
            new LocalizationEntry{ key = "SettingsButton", russian = "Настройки", english = "Settings" },
            new LocalizationEntry{ key = "LeadersButton", russian = "Лидеры", english = "Leaders" },
            new LocalizationEntry{ key = "FriendsButton", russian = "Друзья", english = "Friends" },
            new LocalizationEntry{ key = "CardSkinsButton", russian = "Скины карт", english = "Card Skins" },
            new LocalizationEntry{ key = "ProfileButton", russian = "Профиль", english = "Profile" },
            new LocalizationEntry{ key = "ExitButton", russian = "Выход", english = "Exit" },
            new LocalizationEntry{ key = "BackButton", russian = "Назад", english = "Back" },
            new LocalizationEntry{ key = "MasterVolume", russian = "Громкость", english = "Volume" },
            new LocalizationEntry{ key = "LanguageLabel", russian = "Язык", english = "Language" },
        };

        loc.SetEntries(entries);

        EditorUtility.SetDirty(loc);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Localization set up. Ensure your buttons/labels are named as in entries (PlayButton, SettingsButton, ...). Changing language in settings will update texts.");
    }
#endif
}


