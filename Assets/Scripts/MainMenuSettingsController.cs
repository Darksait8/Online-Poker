using UnityEngine;
using UnityEngine.UI;

public enum AppLanguage { Russian = 0, English = 1 }

public static class LocalizationManager
{
    public static AppLanguage CurrentLanguage
    {
        get => (AppLanguage)PlayerPrefs.GetInt(LanguageKey, (int)AppLanguage.Russian);
        set { PlayerPrefs.SetInt(LanguageKey, (int)value); OnLanguageChanged?.Invoke(value); }
    }

    public const string LanguageKey = "appLanguage";
    public static System.Action<AppLanguage> OnLanguageChanged;
}

public class MainMenuSettingsController : MonoBehaviour
{
    [Header("Громкость звука")]
    [SerializeField] private Slider volumeSlider;    // 0..1

    [Header("Яркость (оверлей)")]
    [SerializeField] private Image brightnessOverlay; // полноэкранный Image поверх меню
    [SerializeField] private Slider brightnessSlider;  // 0..1

    [Header("Язык (Dropdown)")]
    [SerializeField] private Dropdown languageDropdown; // Options: 0=Русский, 1=English
    
    [Header("Кнопка выхода")]
    [SerializeField] private Button logoutButton;

    private const string VolumeKey = "masterVolume";
    private const string BrightnessKey = "brightness";

    private void Awake()
    {
        // Volume
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            float saved = PlayerPrefs.GetFloat(VolumeKey, AudioListener.volume);
            volumeSlider.value = saved;
            AudioListener.volume = saved;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Brightness
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 1f;
            float savedB = PlayerPrefs.GetFloat(BrightnessKey, 1f);
            brightnessSlider.value = savedB;
            ApplyBrightness(savedB);
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        // Language
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "Русский", "English" });
            languageDropdown.value = (int)LocalizationManager.CurrentLanguage;
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
        
        // Logout button
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
    }

    private void OnDestroy()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
        if (languageDropdown != null) languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        if (logoutButton != null) logoutButton.onClick.RemoveListener(OnLogoutClicked);
    }

    private void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VolumeKey, v);
    }

    private void OnBrightnessChanged(float b)
    {
        ApplyBrightness(b);
        PlayerPrefs.SetFloat(BrightnessKey, b);
    }

    private void ApplyBrightness(float brightness)
    {
        if (brightnessOverlay == null) return;
        float alpha = 1f - Mathf.Clamp01(brightness);
        var c = brightnessOverlay.color; c.a = alpha; brightnessOverlay.color = c;
    }

    private void OnLanguageChanged(int idx)
    {
        var lang = (AppLanguage)Mathf.Clamp(idx, 0, 1);
        LocalizationManager.CurrentLanguage = lang;
        // Здесь можно обновлять тексты меню, когда появится словарь локализации
    }
    
    private void OnLogoutClicked()
    {
        AuthManager.Logout();
        SceneTransitionManager.Instance?.LoadAuthScene();
    }
}


