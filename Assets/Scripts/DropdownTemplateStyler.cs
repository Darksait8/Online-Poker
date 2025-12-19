using UnityEngine;
using UnityEngine.UI;

public class DropdownTemplateStyler : MonoBehaviour
{
    [SerializeField] private Color itemTextColor = Color.black;
    [SerializeField] private Font itemFont;
    [SerializeField] private int fontSize = 20;

    public void Configure(Color color, Font font, int size)
    {
        itemTextColor = color;
        itemFont = font;
        fontSize = size;
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Apply()
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text txt in texts)
        {
            txt.color = itemTextColor;
            if (itemFont != null)
                txt.font = itemFont;
            if (fontSize > 0)
                txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleLeft;
        }
        
        // Устанавливаем белый фон для элементов списка
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            // Пропускаем checkmark и другие специальные изображения
            if (img.name.Contains("Checkmark") || img.name.Contains("Arrow"))
                continue;
            
            // Устанавливаем белый фон для фоновых элементов
            Transform parent = img.transform.parent;
            if (parent != null && (parent.name.Contains("Item") || parent.name.Contains("Toggle")))
            {
                img.color = Color.white;
            }
        }
    }
    
    // Вызывается каждый кадр для применения цветов к динамически создаваемым элементам
    private void Update()
    {
        // Применяем цвета к новым элементам, которые могут быть созданы при открытии dropdown
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text txt in texts)
        {
            if (txt.color != itemTextColor)
            {
                txt.color = itemTextColor;
            }
        }
    }
}
