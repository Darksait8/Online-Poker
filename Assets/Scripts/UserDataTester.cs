using UnityEngine;
using UnityEngine.UI;

public class UserDataTester : MonoBehaviour
{
    [Header("UI для тестирования")]
    [SerializeField] private Button createTestUserButton;
    [SerializeField] private Button loginTestUserButton;
    [SerializeField] private Button updateStatsButton;
    [SerializeField] private Button showDataInfoButton;
    [SerializeField] private Button clearDataButton;
    [SerializeField] private Text infoText;
    
    private void Start()
    {
        SetupButtons();
        UpdateInfoText();
    }
    
    private void SetupButtons()
    {
        if (createTestUserButton != null)
            createTestUserButton.onClick.AddListener(CreateTestUser);
        
        if (loginTestUserButton != null)
            loginTestUserButton.onClick.AddListener(LoginTestUser);
        
        if (updateStatsButton != null)
            updateStatsButton.onClick.AddListener(UpdateTestStats);
        
        if (showDataInfoButton != null)
            showDataInfoButton.onClick.AddListener(ShowDataInfo);
        
        if (clearDataButton != null)
            clearDataButton.onClick.AddListener(ClearAllData);
    }
    
    private void CreateTestUser()
    {
        AuthManager.Register("testuser", "test@example.com", "password123", "password123");
        UpdateInfoText();
        Debug.Log("🧪 Test user created: testuser / password123");
    }
    
    private void LoginTestUser()
    {
        AuthManager.Login("testuser", "password123");
        UpdateInfoText();
        Debug.Log("🔑 Logged in as testuser");
    }
    
    private void UpdateTestStats()
    {
        if (AuthManager.IsLoggedIn)
        {
            AuthManager.UpdateGameStats(true, 1000, 0);
            AuthManager.UpdateHandStats(HandResult.Won, HandAction.Raise);
            AuthManager.UnlockAchievement("first_win");
            UpdateInfoText();
            Debug.Log("📊 Test stats updated");
        }
        else
        {
            Debug.LogWarning("⚠️ No user logged in");
        }
    }
    
    private void ShowDataInfo()
    {
        string info = AuthManager.GetDataInfo();
        Debug.Log($"📊 Data Info: {info}");
        UpdateInfoText();
    }
    
    private void ClearAllData()
    {
        AuthManager.ClearAllUserData();
        UpdateInfoText();
        Debug.Log("🗑️ All data cleared");
    }
    
    private void UpdateInfoText()
    {
        if (infoText == null) return;
        
        string info = "";
        
        if (AuthManager.IsLoggedIn)
        {
            var user = AuthManager.CurrentUser;
            info += $"👤 User: {user.username}\n";
            info += $"💰 Chips: {user.chips}\n";
            info += $"🎮 Games: {user.totalGamesPlayed}\n";
            info += $"🏆 Wins: {user.gamesWon}\n";
            info += $"📈 Win Rate: {user.winRate:F1}%\n";
            info += $"🏅 Achievements: {user.achievements.Count}\n";
        }
        else
        {
            info += "👤 No user logged in\n";
        }
        
        info += $"\n📊 System: {AuthManager.GetDataInfo()}";
        
        infoText.text = info;
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("User Data System Tester", GUI.skin.label);
        
        if (GUILayout.Button("Create Test User"))
            CreateTestUser();
        
        if (GUILayout.Button("Login Test User"))
            LoginTestUser();
        
        if (GUILayout.Button("Update Test Stats"))
            UpdateTestStats();
        
        if (GUILayout.Button("Show Data Info"))
            ShowDataInfo();
        
        if (GUILayout.Button("Clear All Data"))
            ClearAllData();
        
        GUILayout.EndArea();
    }
}
