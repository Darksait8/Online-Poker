using UnityEngine;
using UnityEngine.UI;

public class TableSettingsDebugger : MonoBehaviour
{
    [Header("UI элементы для проверки")]
    [SerializeField] private InputField bigBlindInput;
    [SerializeField] private Dropdown maxSeatsDropdown;
    [SerializeField] private Button testButton;
    
    [Header("Тестовые значения")]
    [SerializeField] private int testBigBlind = 2000;
    [SerializeField] private int testMaxSeats = 8;
    
    private void Start()
    {
        SetupTestButton();
    }
    
    private void SetupTestButton()
    {
        if (testButton != null)
        {
            testButton.onClick.AddListener(TestTableSettings);
        }
    }
    
    /// <summary>
    /// Тестирует настройки стола
    /// </summary>
    public void TestTableSettings()
    {
        Debug.Log("=== ТЕСТ НАСТРОЕК СТОЛА ===");
        
        // Проверяем UI элементы
        CheckUIElements();
        
        // Тестируем чтение значений
        TestReadingValues();
        
        // Тестируем установку конфигурации
        TestSettingConfiguration();
        
        Debug.Log("=== КОНЕЦ ТЕСТА ===");
    }
    
    /// <summary>
    /// Проверяет UI элементы
    /// </summary>
    private void CheckUIElements()
    {
        Debug.Log("--- Проверка UI элементов ---");
        
        if (bigBlindInput != null)
        {
            Debug.Log($"✅ bigBlindInput найден: '{bigBlindInput.text}'");
        }
        else
        {
            Debug.LogError("❌ bigBlindInput не назначен!");
        }
        
        if (maxSeatsDropdown != null)
        {
            Debug.Log($"✅ maxSeatsDropdown найден: value = {maxSeatsDropdown.value}, options = {maxSeatsDropdown.options.Count}");
        }
        else
        {
            Debug.LogError("❌ maxSeatsDropdown не назначен!");
        }
    }
    
    /// <summary>
    /// Тестирует чтение значений из UI
    /// </summary>
    private void TestReadingValues()
    {
        Debug.Log("--- Тест чтения значений ---");
        
        int bb = 1000;
        int seats = 6;
        
        // Читаем Big Blind
        if (bigBlindInput != null)
        {
            string bbText = bigBlindInput.text.Trim();
            Debug.Log($"Big Blind text: '{bbText}'");
            
            if (int.TryParse(bbText, out var parsed))
            {
                bb = Mathf.Clamp(parsed, 10, 1000000);
                Debug.Log($"✅ Big Blind прочитан: {bb}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Не удалось распарсить Big Blind: '{bbText}'");
            }
        }
        
        // Читаем Max Seats
        if (maxSeatsDropdown != null)
        {
            seats = Mathf.Clamp(maxSeatsDropdown.value + 2, 2, 9);
            Debug.Log($"✅ Max Seats прочитан: dropdown value = {maxSeatsDropdown.value}, calculated = {seats}");
        }
        
        Debug.Log($"Итоговые значения: Big Blind = {bb}, Max Seats = {seats}");
    }
    
    /// <summary>
    /// Тестирует установку конфигурации
    /// </summary>
    private void TestSettingConfiguration()
    {
        Debug.Log("--- Тест установки конфигурации ---");
        
        // Очищаем предыдущую конфигурацию
        TableRuntimeConfig.Clear();
        Debug.Log("Конфигурация очищена");
        
        // Устанавливаем тестовую конфигурацию
        TableRuntimeConfig.SetPreset(testBigBlind, testMaxSeats);
        Debug.Log($"Установлена тестовая конфигурация: Big Blind = {testBigBlind}, Max Seats = {testMaxSeats}");
        
        // Проверяем результат
        Debug.Log($"Результат: Big Blind = {TableRuntimeConfig.BigBlind}, Max Seats = {TableRuntimeConfig.MaxSeats}, HasConfig = {TableRuntimeConfig.HasConfig}");
        
        if (TableRuntimeConfig.BigBlind == testBigBlind && TableRuntimeConfig.MaxSeats == testMaxSeats)
        {
            Debug.Log("✅ Конфигурация установлена корректно!");
        }
        else
        {
            Debug.LogError("❌ Конфигурация установлена некорректно!");
        }
    }
    
    /// <summary>
    /// Устанавливает тестовые значения в UI
    /// </summary>
    public void SetTestValues()
    {
        if (bigBlindInput != null)
        {
            bigBlindInput.text = testBigBlind.ToString();
            Debug.Log($"Установлен тестовый Big Blind: {testBigBlind}");
        }
        
        if (maxSeatsDropdown != null)
        {
            int dropdownValue = testMaxSeats - 2; // Конвертируем в индекс dropdown
            dropdownValue = Mathf.Clamp(dropdownValue, 0, maxSeatsDropdown.options.Count - 1);
            maxSeatsDropdown.value = dropdownValue;
            Debug.Log($"Установлен тестовый Max Seats: {testMaxSeats} (dropdown value: {dropdownValue})");
        }
    }
    
    /// <summary>
    /// Проверяет текущую конфигурацию стола
    /// </summary>
    public void CheckCurrentConfiguration()
    {
        Debug.Log("=== ТЕКУЩАЯ КОНФИГУРАЦИЯ СТОЛА ===");
        Debug.Log($"Big Blind: {TableRuntimeConfig.BigBlind}");
        Debug.Log($"Small Blind: {TableRuntimeConfig.SmallBlind}");
        Debug.Log($"Max Seats: {TableRuntimeConfig.MaxSeats}");
        Debug.Log($"Has Config: {TableRuntimeConfig.HasConfig}");
        Debug.Log("================================");
    }
    
    /// <summary>
    /// Симулирует создание стола с текущими настройками
    /// </summary>
    public void SimulateCreateTable()
    {
        Debug.Log("=== СИМУЛЯЦИЯ СОЗДАНИЯ СТОЛА ===");
        
        // Читаем значения из UI
        int bb = 1000;
        int seats = 6;
        
        if (bigBlindInput != null && int.TryParse(bigBlindInput.text, out var parsed))
        {
            bb = Mathf.Clamp(parsed, 10, 1000000);
        }
        
        if (maxSeatsDropdown != null)
        {
            seats = Mathf.Clamp(maxSeatsDropdown.value + 2, 2, 9);
        }
        
        // Устанавливаем конфигурацию
        TableRuntimeConfig.SetPreset(bb, seats);
        
        Debug.Log($"Стол создан с настройками: Big Blind = {bb}, Max Seats = {seats}");
        Debug.Log($"Проверка: Big Blind = {TableRuntimeConfig.BigBlind}, Max Seats = {TableRuntimeConfig.MaxSeats}");
        
        Debug.Log("=== КОНЕЦ СИМУЛЯЦИИ ===");
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Table Settings Debugger", GUI.skin.label);
        
        if (GUILayout.Button("Test Table Settings"))
            TestTableSettings();
        
        if (GUILayout.Button("Set Test Values"))
            SetTestValues();
        
        if (GUILayout.Button("Check Current Config"))
            CheckCurrentConfiguration();
        
        if (GUILayout.Button("Simulate Create Table"))
            SimulateCreateTable();
        
        GUILayout.EndArea();
    }
}
