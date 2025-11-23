using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class AdvertisementPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject videoContainer;
    [SerializeField] private GameObject imageContainer;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private Image imageDisplay;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text timerText;
    [SerializeField] private TMP_Text timerTextTMP;
    [SerializeField] private Text titleText;
    [SerializeField] private TMP_Text titleTextTMP;

    [Header("Advertisement Settings")]
    [SerializeField] private float adDuration = 10f;
    [SerializeField] private string[] adVideoFiles = { "IMG_4134.MP4" };
    [SerializeField] private string[] adImageFiles = { "photo_2025-11-21_17-42-09.jpg" };

    public event Action OnAdCompleted;
    public event Action OnAdClosed;

    private bool isVideoAd = false;
    private float currentTimer = 0f;
    private bool canClose = false;
    private bool isCompleted = false;
    private bool isSequentialAds = false; // Показываем ли последовательную рекламу (видео + фото)
    private int currentAdIndex = 1; // Текущая реклама (1 или 2)
    private int totalAdsCount = 1; // Общее количество реклам
    private Coroutine timerCoroutine;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HandleClose);
            closeButton.interactable = false; // Изначально заблокирована
        }

        SetOptionalText(titleText, titleTextTMP, "Реклама от наших партнёров");
        Hide();
    }

    public void ShowRandomAd(int depositAmount)
    {
        Debug.Log($"AdvertisementPanel: Показ рекламы для суммы пополнения {depositAmount}");
        
        // Сбрасываем состояние
        isCompleted = false;
        canClose = false;
        
        // Определяем тип рекламы и количество
        if (depositAmount >= 5000)
        {
            isSequentialAds = true;
            totalAdsCount = 2;
            currentAdIndex = 1;
        }
        else
        {
            isSequentialAds = false;
            totalAdsCount = 1;
            currentAdIndex = 1;
        }
        
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Логика выбора рекламы в зависимости от суммы
        if (depositAmount >= 5000) // Большие суммы - показываем и видео и фото
        {
            ShowBothAds();
        }
        else // Малые суммы - случайная реклама
        {
            ShowRandomSingleAd();
        }

        // Перемещаем панель на верхний слой
        transform.SetAsLastSibling();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);
        }
    }

    private void ShowRandomSingleAd()
    {
        // Случайно выбираем между видео и фото
        bool showVideo = UnityEngine.Random.Range(0, 2) == 0;
        
        if (showVideo && adVideoFiles.Length > 0)
        {
            ShowVideoAd(GetRandomVideoFile());
        }
        else if (adImageFiles.Length > 0)
        {
            ShowImageAd(GetRandomImageFile());
        }
        else
        {
            Debug.LogWarning("AdvertisementPanel: Нет доступных рекламных материалов!");
            HandleAdCompleted();
        }
    }

    private void ShowBothAds()
    {
        Debug.Log("AdvertisementPanel: Показываем полную рекламу (видео + фото) для большой суммы");
        
        // Для больших сумм показываем сначала видео, потом фото
        if (adVideoFiles.Length > 0)
        {
            Debug.Log("AdvertisementPanel: Начинаем с видео рекламы");
            ShowVideoAd(GetRandomVideoFile(), () => {
                // После видео показываем фото - убеждаемся что панель активна
                if (gameObject.activeInHierarchy && adImageFiles.Length > 0)
                {
                    Debug.Log("AdvertisementPanel: Видео завершено, показываем фото рекламу");
                    currentAdIndex = 2; // Переходим ко второй рекламе
                    ShowImageAd(GetRandomImageFile());
                }
                else
                {
                    Debug.Log("AdvertisementPanel: Видео завершено, но нет фото для показа");
                    // НЕ закрываем автоматически - ждем действия пользователя
                }
            });
        }
        else if (adImageFiles.Length > 0)
        {
            Debug.Log("AdvertisementPanel: Нет видео, показываем только фото");
            ShowImageAd(GetRandomImageFile());
        }
        else
        {
            Debug.LogWarning("AdvertisementPanel: Нет рекламных материалов для показа!");
            HandleAdCompleted();
        }
    }

    private void ShowVideoAd(string videoPath, Action onVideoComplete = null)
    {
        isVideoAd = true;
        videoContainer.SetActive(true);
        imageContainer.SetActive(false);

        if (videoPlayer != null)
        {
            string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, "advertisement", videoPath);
            if (!System.IO.File.Exists(fullPath))
            {
                // Пробуем относительный путь от корня проекта
                fullPath = System.IO.Path.Combine(Application.dataPath, "..", "advertisement", videoPath);
            }

            if (System.IO.File.Exists(fullPath))
            {
                videoPlayer.url = "file://" + fullPath;
                videoPlayer.isLooping = false;
                videoPlayer.Play();

                // Подписываемся на завершение видео
                videoPlayer.loopPointReached += (vp) => {
                    if (onVideoComplete != null)
                        onVideoComplete();
                    else
                        HandleAdCompleted();
                };
            }
            else
            {
                Debug.LogWarning($"AdvertisementPanel: Видеофайл не найден: {fullPath}");
                if (onVideoComplete != null)
                    onVideoComplete();
                else
                    HandleAdCompleted();
                return;
            }
        }

        StartAdTimer(onVideoComplete);
    }

    private void ShowImageAd(string imagePath)
    {
        // Убеждаемся что панель активна
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("AdvertisementPanel: Попытка показать изображение на неактивной панели!");
            HandleAdCompleted();
            return;
        }

        isVideoAd = false;
        videoContainer.SetActive(false);
        imageContainer.SetActive(true);

        if (imageDisplay != null)
        {
            // Загружаем изображение
            string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, "advertisement", imagePath);
            if (!System.IO.File.Exists(fullPath))
            {
                fullPath = System.IO.Path.Combine(Application.dataPath, "..", "advertisement", imagePath);
            }

            if (System.IO.File.Exists(fullPath))
            {
                StartCoroutine(LoadImageCoroutine(fullPath));
            }
            else
            {
                Debug.LogWarning($"AdvertisementPanel: Изображение не найдено: {fullPath}");
                HandleAdCompleted();
                return;
            }
        }

        StartAdTimer();
    }

    private IEnumerator LoadImageCoroutine(string imagePath)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture("file://" + imagePath))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success && imageDisplay != null)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                imageDisplay.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"AdvertisementPanel: Ошибка загрузки изображения: {www.error}");
                HandleAdCompleted();
            }
        }
    }

    private void StartAdTimer(Action onComplete = null)
    {
        // Проверяем что GameObject активен перед запуском корутины
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("AdvertisementPanel: Попытка запустить таймер на неактивном объекте!");
            return;
        }

        currentTimer = adDuration;
        canClose = false;
        
        if (closeButton != null)
        {
            closeButton.interactable = false;
        }

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        timerCoroutine = StartCoroutine(AdTimerCoroutine(onComplete));
    }

    private IEnumerator AdTimerCoroutine(Action onComplete = null)
    {
        while (currentTimer > 0)
        {
            int seconds = Mathf.CeilToInt(currentTimer);
            string adInfo = isSequentialAds ? $"({currentAdIndex}/{totalAdsCount}) " : "";
            SetOptionalText(timerText, timerTextTMP, $"{adInfo}Можно закрыть через: {seconds}с");
            yield return new WaitForSeconds(1f);
            currentTimer -= 1f;
        }

        // Таймер закончился
        canClose = true;
        if (closeButton != null)
        {
            closeButton.interactable = true;
        }
        
        // Показываем разные сообщения в зависимости от типа рекламы
        if (onComplete != null)
        {
            SetOptionalText(timerText, timerTextTMP, $"({currentAdIndex}/{totalAdsCount}) Переходим к следующей рекламе...");
            yield return new WaitForSeconds(1f); // Небольшая пауза перед переходом
            onComplete();
        }
        else
        {
            string adInfo = isSequentialAds ? $"({currentAdIndex}/{totalAdsCount}) " : "";
            SetOptionalText(timerText, timerTextTMP, $"{adInfo}🎯 Нажмите X чтобы получить фишки!");
            Debug.Log("AdvertisementPanel: Таймер завершен, ожидаем действия пользователя");
        }
    }

    private void HandleClose()
    {
        if (!canClose)
        {
            Debug.Log("AdvertisementPanel: Реклама ещё не закончилась!");
            return;
        }

        if (isCompleted)
        {
            Debug.Log("AdvertisementPanel: Реклама уже была завершена!");
            return;
        }

        isCompleted = true;
        Hide();
        OnAdClosed?.Invoke();
    }

    private void HandleAdCompleted()
    {
        if (isCompleted)
        {
            Debug.Log("AdvertisementPanel: Реклама уже была завершена!");
            return;
        }

        isCompleted = true;
        Hide();
        OnAdCompleted?.Invoke();
    }

    public void Hide()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private string GetRandomVideoFile()
    {
        if (adVideoFiles.Length == 0) return "";
        return adVideoFiles[UnityEngine.Random.Range(0, adVideoFiles.Length)];
    }

    private string GetRandomImageFile()
    {
        if (adImageFiles.Length == 0) return "";
        return adImageFiles[UnityEngine.Random.Range(0, adImageFiles.Length)];
    }

    private void SetOptionalText(Text legacy, TMP_Text tmp, string value)
    {
        if (tmp != null)
            tmp.text = value;
        if (legacy != null)
            legacy.text = value;
    }

    public static AdvertisementPanel CreateDefault(Transform parent)
    {
        Debug.Log("AdvertisementPanel: CreateDefault вызван");
        
        GameObject root = new GameObject("AdvertisementPanel", typeof(RectTransform), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        
        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0.9f); // Полупрозрачный черный фон

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

        // Content Container
        GameObject content = new GameObject("Content", typeof(RectTransform));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(root.transform, false);
        contentRect.anchorMin = new Vector2(0.1f, 0.1f);
        contentRect.anchorMax = new Vector2(0.9f, 0.9f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.SetParent(content.transform, false);
        titleRect.anchorMin = new Vector2(0f, 0.9f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text title = titleObj.GetComponent<Text>();
        title.text = "Реклама от наших партнёров";
        title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        title.fontSize = 24;
        title.fontStyle = FontStyle.Bold;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;

        // Video Container
        GameObject videoContainer = new GameObject("VideoContainer", typeof(RectTransform));
        RectTransform videoRect = videoContainer.GetComponent<RectTransform>();
        videoRect.SetParent(content.transform, false);
        videoRect.anchorMin = new Vector2(0f, 0.1f);
        videoRect.anchorMax = new Vector2(1f, 0.85f);
        videoRect.offsetMin = Vector2.zero;
        videoRect.offsetMax = Vector2.zero;

        // Video Player
        GameObject videoPlayerObj = new GameObject("VideoPlayer", typeof(RectTransform), typeof(VideoPlayer), typeof(RawImage));
        RectTransform videoPlayerRect = videoPlayerObj.GetComponent<RectTransform>();
        videoPlayerRect.SetParent(videoContainer.transform, false);
        videoPlayerRect.anchorMin = Vector2.zero;
        videoPlayerRect.anchorMax = Vector2.one;
        videoPlayerRect.offsetMin = Vector2.zero;
        videoPlayerRect.offsetMax = Vector2.zero;

        VideoPlayer videoPlayer = videoPlayerObj.GetComponent<VideoPlayer>();
        RawImage videoDisplay = videoPlayerObj.GetComponent<RawImage>();
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Создаем RenderTexture для видео
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 16);
        videoPlayer.targetTexture = renderTexture;
        videoDisplay.texture = renderTexture;

        // Image Container
        GameObject imageContainer = new GameObject("ImageContainer", typeof(RectTransform));
        RectTransform imageRect = imageContainer.GetComponent<RectTransform>();
        imageRect.SetParent(content.transform, false);
        imageRect.anchorMin = new Vector2(0f, 0.1f);
        imageRect.anchorMax = new Vector2(1f, 0.85f);
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        // Image Display
        GameObject imageDisplayObj = new GameObject("ImageDisplay", typeof(RectTransform), typeof(Image));
        RectTransform imageDisplayRect = imageDisplayObj.GetComponent<RectTransform>();
        imageDisplayRect.SetParent(imageContainer.transform, false);
        imageDisplayRect.anchorMin = Vector2.zero;
        imageDisplayRect.anchorMax = Vector2.one;
        imageDisplayRect.offsetMin = Vector2.zero;
        imageDisplayRect.offsetMax = Vector2.zero;

        Image imageDisplay = imageDisplayObj.GetComponent<Image>();
        imageDisplay.preserveAspect = true;

        // Bottom Panel (Timer + Close Button)
        GameObject bottomPanel = new GameObject("BottomPanel", typeof(RectTransform));
        RectTransform bottomRect = bottomPanel.GetComponent<RectTransform>();
        bottomRect.SetParent(content.transform, false);
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0.1f);
        bottomRect.offsetMin = Vector2.zero;
        bottomRect.offsetMax = Vector2.zero;

        // Timer Text
        GameObject timerObj = new GameObject("Timer", typeof(RectTransform), typeof(Text));
        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.SetParent(bottomPanel.transform, false);
        timerRect.anchorMin = new Vector2(0f, 0f);
        timerRect.anchorMax = new Vector2(0.8f, 1f);
        timerRect.offsetMin = Vector2.zero;
        timerRect.offsetMax = Vector2.zero;

        Text timerText = timerObj.GetComponent<Text>();
        timerText.text = "Закрыть через: 10с";
        timerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        timerText.fontSize = 18;
        timerText.color = Color.yellow;
        timerText.alignment = TextAnchor.MiddleLeft;

        // Close Button
        GameObject closeButtonObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform closeButtonRect = closeButtonObj.GetComponent<RectTransform>();
        closeButtonRect.SetParent(bottomPanel.transform, false);
        closeButtonRect.anchorMin = new Vector2(0.8f, 0.2f);
        closeButtonRect.anchorMax = new Vector2(1f, 0.8f);
        closeButtonRect.offsetMin = Vector2.zero;
        closeButtonRect.offsetMax = Vector2.zero;

        Image closeButtonImage = closeButtonObj.GetComponent<Image>();
        closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button closeButton = closeButtonObj.GetComponent<Button>();
        closeButton.targetGraphic = closeButtonImage;

        // Close Button Text
        GameObject closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.SetParent(closeButtonObj.transform, false);
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        Text closeText = closeTextObj.GetComponent<Text>();
        closeText.text = "X";
        closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        closeText.fontSize = 24;
        closeText.fontStyle = FontStyle.Bold;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;

        // Создаем компонент панели
        var panel = root.AddComponent<AdvertisementPanel>();
        panel.canvasGroup = canvasGroup;
        panel.videoContainer = videoContainer;
        panel.imageContainer = imageContainer;
        panel.videoPlayer = videoPlayer;
        panel.videoDisplay = videoDisplay;
        panel.imageDisplay = imageDisplay;
        panel.closeButton = closeButton;
        panel.timerText = timerText;
        panel.titleText = title;

        // Изначально скрываем оба контейнера
        videoContainer.SetActive(false);
        imageContainer.SetActive(false);

        panel.Initialize();
        panel.Hide();

        return panel;
    }
}
