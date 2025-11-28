using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для настройки размера префаба места и его элементов
/// Добавьте на префаб Seat для управления размерами через код
/// </summary>
public class SeatPrefabConfig : MonoBehaviour
{
    [Header("Базовые размеры")]
    [SerializeField] private float baseSeatWidth = 160f;
    [SerializeField] private float baseSeatHeight = 110f;
    
    [Header("Множитель размера")]
    [SerializeField] private float sizeMultiplier = 1.0f;  // 1.0 = базовый размер
    
    [Header("Автоматическая настройка")]
    [SerializeField] private bool autoConfigureOnStart = false;
    
    private RectTransform seatRect;
    
    private void Start()
    {
        seatRect = GetComponent<RectTransform>();
        if (seatRect == null)
        {
            Debug.LogWarning("SeatPrefabConfig: RectTransform не найден!");
            return;
        }
        
        if (autoConfigureOnStart && sizeMultiplier != 1.0f)
        {
            ConfigureSeatSize(sizeMultiplier);
        }
    }
    
    /// <summary>
    /// Настраивает размер места и всех элементов пропорционально
    /// </summary>
    public void ConfigureSeatSize(float multiplier)
    {
        sizeMultiplier = multiplier;
        
        if (seatRect == null)
        {
            seatRect = GetComponent<RectTransform>();
            if (seatRect == null) return;
        }
        
        // Устанавливаем размер основного места
        float newWidth = baseSeatWidth * multiplier;
        float newHeight = baseSeatHeight * multiplier;
        seatRect.sizeDelta = new Vector2(newWidth, newHeight);
        
        // Масштабируем все дочерние элементы
        ScaleChildren(transform, multiplier);
        
        Debug.Log($"SeatPrefabConfig: Размер места настроен: {newWidth}x{newHeight} (множитель: {multiplier}x)");
    }
    
    /// <summary>
    /// Рекурсивно масштабирует дочерние элементы
    /// </summary>
    private void ScaleChildren(Transform parent, float multiplier)
    {
        foreach (Transform child in parent)
        {
            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                // Масштабируем размер
                if (childRect.sizeDelta != Vector2.zero)
                {
                    childRect.sizeDelta = childRect.sizeDelta * multiplier;
                }
                
                // Масштабируем позицию (если не использует anchors)
                if (childRect.anchorMin == childRect.anchorMax)
                {
                    childRect.anchoredPosition = childRect.anchoredPosition * multiplier;
                }
            }
            
            // Рекурсивно обрабатываем детей
            if (child.childCount > 0)
            {
                ScaleChildren(child, multiplier);
            }
        }
    }
    
    /// <summary>
    /// Устанавливает множитель размера
    /// </summary>
    public void SetSizeMultiplier(float multiplier)
    {
        ConfigureSeatSize(multiplier);
    }
}

