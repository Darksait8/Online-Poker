using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.SceneManagement;

public class AuthUIBuilder : EditorWindow
{
    [MenuItem("Tools/Auth UI Builder")]
    public static void ShowWindow()
    {
        GetWindow<AuthUIBuilder>("Auth UI Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auth UI Builder", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Create Complete Auth UI", GUILayout.Height(30)))
        {
            CreateCompleteAuthUI();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Create Canvas Only", GUILayout.Height(25)))
        {
            CreateCanvas();
        }

        if (GUILayout.Button("Create Login Panel Only", GUILayout.Height(25)))
        {
            CreateLoginPanel();
        }

        if (GUILayout.Button("Create Register Panel Only", GUILayout.Height(25)))
        {
            CreateRegisterPanel();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Setup Auth Controller", GUILayout.Height(25)))
        {
            SetupAuthController();
        }

        if (GUILayout.Button("Setup Scene Manager", GUILayout.Height(25)))
        {
            SetupSceneManager();
        }
    }

    private void CreateCompleteAuthUI()
    {
        // Удаляем старый Canvas если есть
        Canvas oldCanvas = FindObjectOfType<Canvas>();
        if (oldCanvas != null)
        {
            DestroyImmediate(oldCanvas.gameObject);
        }

        // Создаем новый Canvas
        CreateCanvas();
        
        // Создаем все панели
        CreateLoginPanel();
        CreateRegisterPanel();
        CreateLoadingPanel();
        CreateMessageTexts();
        
        // Настраиваем контроллеры
        SetupAuthController();
        SetupSceneManager();
        
        Debug.Log("✅ Complete Auth UI created successfully!");
    }

    private void CreateCanvas()
    {
        // Создаем Canvas
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Добавляем Canvas Scaler
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // Добавляем Graphic Raycaster
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Настраиваем RectTransform
        RectTransform rectTransform = canvasGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        
        // Создаем EventSystem если его нет
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        Debug.Log("✅ Canvas created successfully!");
    }

    private void CreateLoginPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found! Create Canvas first.");
            return;
        }

        // Создаем панель входа
        GameObject loginPanel = CreatePanel("LoginPanel", canvas.transform, new Vector2(400, 500));
        
        // Заголовок
        CreateText("LoginTitle", loginPanel.transform, "Вход в игру", 24, new Vector2(0, 200), new Vector2(360, 40));
        
        // Поле имени пользователя
        GameObject usernameField = CreateInputField("LoginUsernameField", loginPanel.transform, 
            "Введите имя пользователя", new Vector2(0, 120), new Vector2(340, 40));
        
        // Поле пароля
        GameObject passwordField = CreateInputField("LoginPasswordField", loginPanel.transform, 
            "Введите пароль", new Vector2(0, 60), new Vector2(340, 40));
        passwordField.GetComponent<InputField>().contentType = InputField.ContentType.Password;
        
        // Кнопка входа
        CreateButton("LoginButton", loginPanel.transform, "Войти", new Vector2(0, 0), new Vector2(340, 50));
        
        // Кнопка входа как гость
        CreateButton("LoginAsGuestButton", loginPanel.transform, "Войти как гость", 
            new Vector2(0, -60), new Vector2(340, 40));
        
        // Кнопка перехода к регистрации
        CreateButton("ShowRegisterButton", loginPanel.transform, "Нет аккаунта? Зарегистрироваться", 
            new Vector2(0, -200), new Vector2(340, 30));
        
        Debug.Log("✅ Login Panel created successfully!");
    }

    private void CreateRegisterPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found! Create Canvas first.");
            return;
        }

        // Создаем панель регистрации
        GameObject registerPanel = CreatePanel("RegisterPanel", canvas.transform, new Vector2(400, 600));
        registerPanel.SetActive(false); // Скрываем по умолчанию
        
        // Заголовок
        CreateText("RegisterTitle", registerPanel.transform, "Регистрация", 24, 
            new Vector2(0, 250), new Vector2(360, 40));
        
        // Поле имени пользователя
        CreateInputField("RegisterUsernameField", registerPanel.transform, 
            "Введите имя пользователя", new Vector2(0, 190), new Vector2(340, 40));
        
        // Поле email
        GameObject emailField = CreateInputField("RegisterEmailField", registerPanel.transform, 
            "Введите email", new Vector2(0, 130), new Vector2(340, 40));
        emailField.GetComponent<InputField>().contentType = InputField.ContentType.EmailAddress;
        
        // Поле пароля
        GameObject passwordField = CreateInputField("RegisterPasswordField", registerPanel.transform, 
            "Введите пароль", new Vector2(0, 70), new Vector2(340, 40));
        passwordField.GetComponent<InputField>().contentType = InputField.ContentType.Password;
        
        // Поле подтверждения пароля
        GameObject confirmPasswordField = CreateInputField("RegisterConfirmPasswordField", registerPanel.transform, 
            "Подтвердите пароль", new Vector2(0, 10), new Vector2(340, 40));
        confirmPasswordField.GetComponent<InputField>().contentType = InputField.ContentType.Password;
        
        // Кнопка регистрации
        CreateButton("RegisterButton", registerPanel.transform, "Зарегистрироваться", 
            new Vector2(0, -50), new Vector2(340, 50));
        
        // Кнопка перехода к входу
        CreateButton("ShowLoginButton", registerPanel.transform, "Уже есть аккаунт? Войти", 
            new Vector2(0, -250), new Vector2(340, 30));
        
        Debug.Log("✅ Register Panel created successfully!");
    }

    private void CreateLoadingPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found! Create Canvas first.");
            return;
        }

        // Создаем панель загрузки
        GameObject loadingPanel = CreatePanel("LoadingPanel", canvas.transform, new Vector2(1920, 1080));
        loadingPanel.SetActive(false); // Скрываем по умолчанию
        
        // Настраиваем как полноэкранную панель
        RectTransform rectTransform = loadingPanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Полупрозрачный фон
        Image bgImage = loadingPanel.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        
        // Текст загрузки
        CreateText("LoadingText", loadingPanel.transform, "Загрузка...", 20, 
            new Vector2(0, 0), new Vector2(300, 50));
        
        Debug.Log("✅ Loading Panel created successfully!");
    }

    private void CreateMessageTexts()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found! Create Canvas first.");
            return;
        }

        // Текст ошибок
        GameObject errorText = CreateText("ErrorText", canvas.transform, "", 16, 
            new Vector2(0, -200), new Vector2(1820, 30));
        errorText.GetComponent<Text>().color = Color.red;
        errorText.SetActive(false);
        
        // Текст успеха
        GameObject successText = CreateText("SuccessText", canvas.transform, "", 16, 
            new Vector2(0, -200), new Vector2(1820, 30));
        successText.GetComponent<Text>().color = Color.green;
        successText.SetActive(false);
        
        Debug.Log("✅ Message Texts created successfully!");
    }

    private void SetupAuthController()
    {
        // Создаем контроллер авторизации
        GameObject authController = GameObject.Find("AuthController");
        if (authController == null)
        {
            authController = new GameObject("AuthController");
        }
        
        // Добавляем компонент
        AuthUIController controller = authController.GetComponent<AuthUIController>();
        if (controller == null)
        {
            controller = authController.AddComponent<AuthUIController>();
        }
        
        // Находим все UI элементы и назначаем их
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            // Находим панели
            Transform loginPanel = canvas.transform.Find("LoginPanel");
            Transform registerPanel = canvas.transform.Find("RegisterPanel");
            Transform loadingPanel = canvas.transform.Find("LoadingPanel");
            Transform errorText = canvas.transform.Find("ErrorText");
            Transform successText = canvas.transform.Find("SuccessText");
            
            if (loginPanel != null)
            {
                // Используем SerializedObject для назначения полей
                SerializedObject serializedController = new SerializedObject(controller);
                
                // Назначаем панели
                serializedController.FindProperty("loginPanel").objectReferenceValue = loginPanel.gameObject;
                serializedController.FindProperty("registerPanel").objectReferenceValue = registerPanel?.gameObject;
                serializedController.FindProperty("loadingPanel").objectReferenceValue = loadingPanel?.gameObject;
                
                // Находим кнопки и поля
                serializedController.FindProperty("showLoginButton").objectReferenceValue = 
                    registerPanel?.Find("ShowLoginButton")?.GetComponent<Button>();
                serializedController.FindProperty("showRegisterButton").objectReferenceValue = 
                    loginPanel.Find("ShowRegisterButton")?.GetComponent<Button>();
                
                serializedController.FindProperty("loginUsernameField").objectReferenceValue = 
                    loginPanel.Find("LoginUsernameField")?.GetComponent<InputField>();
                serializedController.FindProperty("loginPasswordField").objectReferenceValue = 
                    loginPanel.Find("LoginPasswordField")?.GetComponent<InputField>();
                serializedController.FindProperty("loginButton").objectReferenceValue = 
                    loginPanel.Find("LoginButton")?.GetComponent<Button>();
                serializedController.FindProperty("loginAsGuestButton").objectReferenceValue = 
                    loginPanel.Find("LoginAsGuestButton")?.GetComponent<Button>();
                
                serializedController.FindProperty("registerUsernameField").objectReferenceValue = 
                    registerPanel?.Find("RegisterUsernameField")?.GetComponent<InputField>();
                serializedController.FindProperty("registerEmailField").objectReferenceValue = 
                    registerPanel?.Find("RegisterEmailField")?.GetComponent<InputField>();
                serializedController.FindProperty("registerPasswordField").objectReferenceValue = 
                    registerPanel?.Find("RegisterPasswordField")?.GetComponent<InputField>();
                serializedController.FindProperty("registerConfirmPasswordField").objectReferenceValue = 
                    registerPanel?.Find("RegisterConfirmPasswordField")?.GetComponent<InputField>();
                serializedController.FindProperty("registerButton").objectReferenceValue = 
                    registerPanel?.Find("RegisterButton")?.GetComponent<Button>();
                
                serializedController.FindProperty("errorText").objectReferenceValue = 
                    errorText?.GetComponent<Text>();
                serializedController.FindProperty("successText").objectReferenceValue = 
                    successText?.GetComponent<Text>();
                
                serializedController.ApplyModifiedProperties();
            }
        }
        
        Debug.Log("✅ Auth Controller setup successfully!");
    }

    private void SetupSceneManager()
    {
        // Создаем менеджер сцен
        GameObject sceneManager = GameObject.Find("SceneManager");
        if (sceneManager == null)
        {
            sceneManager = new GameObject("SceneManager");
        }
        
        // Добавляем компонент
        SceneTransitionManager manager = sceneManager.GetComponent<SceneTransitionManager>();
        if (manager == null)
        {
            manager = sceneManager.AddComponent<SceneTransitionManager>();
        }
        
        Debug.Log("✅ Scene Manager setup successfully!");
    }

    // Вспомогательные методы для создания UI элементов
    private GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        // Добавляем Image
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
        
        // Настраиваем RectTransform
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
        
        return panel;
    }

    private GameObject CreateText(string name, Transform parent, string text, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        
        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = fontSize;
        textComponent.color = Color.black;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        RectTransform rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = position;
        
        return textGO;
    }

    private GameObject CreateButton(string name, Transform parent, string text, Vector2 position, Vector2 size)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        
        // Добавляем Image
        Image image = buttonGO.AddComponent<Image>();
        image.color = new Color(0.2f, 0.5f, 0.8f, 1f);
        
        // Добавляем Button
        Button button = buttonGO.AddComponent<Button>();
        
        // Создаем дочерний Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 18;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Настраиваем RectTransform кнопки
        RectTransform rectTransform = buttonGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = position;
        
        return buttonGO;
    }

    private GameObject CreateInputField(string name, Transform parent, string placeholder, Vector2 position, Vector2 size)
    {
        GameObject inputGO = new GameObject(name);
        inputGO.transform.SetParent(parent, false);
        
        // Добавляем Image
        Image image = inputGO.AddComponent<Image>();
        image.color = Color.white;
        
        // Добавляем InputField
        InputField inputField = inputGO.AddComponent<InputField>();
        
        // Создаем дочерний Text для ввода
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(inputGO.transform, false);
        
        Text textComponent = textGO.AddComponent<Text>();
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 14;
        textComponent.color = Color.black;
        textComponent.alignment = TextAnchor.MiddleLeft;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);
        
        // Создаем Placeholder
        GameObject placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(inputGO.transform, false);
        
        Text placeholderText = placeholderGO.AddComponent<Text>();
        placeholderText.text = placeholder;
        placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        placeholderText.fontSize = 14;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        placeholderRect.anchoredPosition = Vector2.zero;
        placeholderRect.offsetMin = new Vector2(10, 0);
        placeholderRect.offsetMax = new Vector2(-10, 0);
        
        // Настраиваем InputField
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;
        inputField.characterLimit = 50;
        
        // Настраиваем RectTransform
        RectTransform rectTransform = inputGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = position;
        
        return inputGO;
    }
}
