using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AuthUIController : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("Кнопки переключения")]
    [SerializeField] private Button showLoginButton;
    [SerializeField] private Button showRegisterButton;
    
    [Header("Поля входа")]
    [SerializeField] private InputField loginUsernameField;
    [SerializeField] private InputField loginPasswordField;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button loginAsGuestButton;
    
    [Header("Поля регистрации")]
    [SerializeField] private InputField registerUsernameField;
    [SerializeField] private InputField registerEmailField;
    [SerializeField] private InputField registerPasswordField;
    [SerializeField] private InputField registerConfirmPasswordField;
    [SerializeField] private Button registerButton;
    
    [Header("Сообщения об ошибках")]
    [SerializeField] private Text errorText;
    [SerializeField] private Text successText;
    
    [Header("Настройки")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float loadingDelay = 1f;

    private bool isAuthenticating;
    
    private void Awake()
    {
        // Подписываемся на события авторизации
        AuthManager.OnUserLoggedIn += OnUserLoggedIn;
        AuthManager.OnUserLoggedOut += OnUserLoggedOut;
        AuthManager.OnAuthError += OnAuthError;
        
        // Настраиваем кнопки
        SetupButtons();
        
        // Показываем панель входа по умолчанию
        ShowLoginPanel();
        SetInteractionEnabled(true);
        
        // Проверяем, не авторизован ли пользователь уже
        if (AuthManager.IsLoggedIn)
        {
            OnUserLoggedIn(AuthManager.CurrentUser);
        }
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        AuthManager.OnUserLoggedIn -= OnUserLoggedIn;
        AuthManager.OnUserLoggedOut -= OnUserLoggedOut;
        AuthManager.OnAuthError -= OnAuthError;
    }
    
    private void SetupButtons()
    {
        if (showLoginButton != null) showLoginButton.onClick.AddListener(ShowLoginPanel);
        if (showRegisterButton != null) showRegisterButton.onClick.AddListener(ShowRegisterPanel);
        
        if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
        if (loginAsGuestButton != null) loginAsGuestButton.onClick.AddListener(OnLoginAsGuestClicked);
        
        if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);
    }
    
    private void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        ClearMessages();
    }
    
    private void ShowRegisterPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        ClearMessages();
    }
    
    private void OnLoginClicked()
    {
        string username = loginUsernameField != null ? loginUsernameField.text : "";
        string password = loginPasswordField != null ? loginPasswordField.text : "";
        
        ClearMessages();
        BeginAuthOperation();
        
        AuthManager.Login(username, password);
    }
    
    private void OnLoginAsGuestClicked()
    {
        ClearMessages();
        BeginAuthOperation();
        
        // Используем метод AuthManager для входа как гость
        AuthManager.LoginAsGuest();
    }
    
    private void OnRegisterClicked()
    {
        string username = registerUsernameField != null ? registerUsernameField.text : "";
        string email = registerEmailField != null ? registerEmailField.text : "";
        string password = registerPasswordField != null ? registerPasswordField.text : "";
        string confirmPassword = registerConfirmPasswordField != null ? registerConfirmPasswordField.text : "";
        
        ClearMessages();
        BeginAuthOperation();
        
        AuthManager.Register(username, email, password, confirmPassword);
    }
    
    private void OnUserLoggedIn(UserProfile user)
    {
        EndAuthOperation();
        ShowSuccessMessage($"Добро пожаловать, {user.username}!");
        
        // Переходим в главное меню через небольшую задержку
        Invoke(nameof(LoadMainMenu), loadingDelay);
    }
    
    private void OnUserLoggedOut()
    {
        EndAuthOperation();
        ClearMessages();
    }
    
    private void OnAuthError(string errorMessage)
    {
        EndAuthOperation();
        ShowErrorMessage(errorMessage);
    }
    
    private void ShowLoading(bool show)
    {
        if (loadingPanel != null) loadingPanel.SetActive(show);
    }
    
    private void ShowErrorMessage(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
        
        if (successText != null) successText.gameObject.SetActive(false);
    }
    
    private void ShowSuccessMessage(string message)
    {
        if (successText != null)
        {
            successText.text = message;
            successText.gameObject.SetActive(true);
        }
        
        if (errorText != null) errorText.gameObject.SetActive(false);
    }
    
    private void ClearMessages()
    {
        if (errorText != null) errorText.gameObject.SetActive(false);
        if (successText != null) successText.gameObject.SetActive(false);
    }
    
    private void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    // Методы для кнопок выхода (если нужны)
    public void Logout()
    {
        AuthManager.Logout();
    }
    
    public void ClearUserData()
    {
        AuthManager.ClearAllUserData();
        Debug.Log("Все данные пользователя очищены");
    }

    private void BeginAuthOperation()
    {
        isAuthenticating = true;
        SetInteractionEnabled(false);
        ShowLoading(true);
    }

    private void EndAuthOperation()
    {
        isAuthenticating = false;
        ShowLoading(false);
        SetInteractionEnabled(true);
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (!enabled && !isAuthenticating)
            enabled = true;

        if (loginUsernameField != null) loginUsernameField.interactable = enabled;
        if (loginPasswordField != null) loginPasswordField.interactable = enabled;
        if (loginButton != null) loginButton.interactable = enabled;
        if (loginAsGuestButton != null) loginAsGuestButton.interactable = enabled;

        if (registerUsernameField != null) registerUsernameField.interactable = enabled;
        if (registerEmailField != null) registerEmailField.interactable = enabled;
        if (registerPasswordField != null) registerPasswordField.interactable = enabled;
        if (registerConfirmPasswordField != null) registerConfirmPasswordField.interactable = enabled;
        if (registerButton != null) registerButton.interactable = enabled;
    }
}
