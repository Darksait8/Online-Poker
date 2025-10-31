using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Пауза")]
    [SerializeField] private GameObject pausePanel;           // корневая панель паузы (фон + кнопки)
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button exitButton;

    [Header("Настройки внутри паузы")]
    [SerializeField] private GameObject settingsPanel;        // внутренняя панель настроек
    [SerializeField] private Slider masterVolumeSlider;       // громкость
    [SerializeField] private Button backFromSettingsButton;   // назад из настроек

    [Header("Яркость (оверлей)")]
    [SerializeField] private Image brightnessOverlay;         // полноэкранный Image поверх сцены
    [SerializeField] private Slider brightnessSlider;         // ползунок яркости (0..1)

    [Header("Горячая клавиша")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    private bool isPaused;

    private void Awake()
    {
        // гарантируем нормальную скорость при входе в сцену
        Time.timeScale = 1f;
        isPaused = false;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (backFromSettingsButton != null) backFromSettingsButton.onClick.AddListener(HideSettings);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = PlayerPrefs.GetFloat("masterVolume", AudioListener.volume);
            AudioListener.volume = masterVolumeSlider.value;
            masterVolumeSlider.onValueChanged.AddListener(v =>
            {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat("masterVolume", v);
            });
        }

        // Яркость: 0 = темно, 1 = ярко. Оверлей затемняет экран: alpha = 1 - brightness
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 1f;
            float brightness = PlayerPrefs.GetFloat("brightness", 1f);
            brightnessSlider.value = brightness;
            ApplyBrightness(brightness);
            brightnessSlider.onValueChanged.AddListener(b =>
            {
                ApplyBrightness(b);
                PlayerPrefs.SetFloat("brightness", b);
            });
        }
    }

    private void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(ShowSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(LoadMainMenu);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitGame);
        if (backFromSettingsButton != null) backFromSettingsButton.onClick.RemoveListener(HideSettings);
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveAllListeners();
        if (brightnessSlider != null) brightnessSlider.onValueChanged.RemoveAllListeners();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isPaused) Resume(); else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void ShowSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void HideSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void LoadMainMenu()
    {
        // перед сменой сцены снимаем паузу
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ApplyBrightness(float brightness)
    {
        if (brightnessOverlay == null) return;
        // яркость 1 => оверлей полностью прозрачный; 0 => оверлей полностью чёрный
        float alpha = 1f - Mathf.Clamp01(brightness);
        var c = brightnessOverlay.color;
        c.a = alpha;
        brightnessOverlay.color = c;
    }
}


