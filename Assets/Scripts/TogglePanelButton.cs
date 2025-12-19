using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Простой скрипт для кнопки переключения панели.
/// Повесьте на кнопку и укажите панель для переключения.
/// </summary>
public class TogglePanelButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject targetPanel;
    
    private void Start()
    {
        // Автопоиск панели если не назначена
        if (targetPanel == null)
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                foreach (Transform sibling in parent)
                {
                    if (sibling != transform && sibling.name.Contains("Probability"))
                    {
                        targetPanel = sibling.gameObject;
                        Debug.Log($"TogglePanelButton: Найдена панель '{sibling.name}'");
                        break;
                    }
                }
            }
        }
        
        // Также привязываем к Button если есть
        var button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(Toggle);
        }
        
        Debug.Log($"TogglePanelButton: Инициализирован, targetPanel = {(targetPanel != null ? targetPanel.name : "null")}");
    }
    
    // Обработка клика через интерфейс — работает даже без Button компонента
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("TogglePanelButton: OnPointerClick вызван");
        Toggle();
    }
    
    public void Toggle()
    {
        Debug.Log($"TogglePanelButton: Toggle вызван");
        
        if (targetPanel != null)
        {
            bool newState = !targetPanel.activeSelf;
            targetPanel.SetActive(newState);
            Debug.Log($"TogglePanelButton: Панель {(newState ? "открыта" : "закрыта")}");
        }
        else
        {
            Debug.LogError("TogglePanelButton: targetPanel не назначен!");
        }
    }
}

