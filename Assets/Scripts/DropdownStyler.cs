using UnityEngine;
using UnityEngine.UI;

public static class DropdownStyler
{
    public static void Apply(Dropdown dropdown, Color? textColor = null)
    {
        if (dropdown == null) return;
        
        Color textColorValue = textColor ?? new Color(0.1f, 0.1f, 0.1f, 1f); // По умолчанию темный для белого фона

        if (dropdown.captionText != null)
        {
            dropdown.captionText.color = textColorValue;
            dropdown.captionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            dropdown.captionText.fontSize = 20;
            dropdown.captionText.alignment = TextAnchor.MiddleLeft;
        }

        RectTransform template = dropdown.template;
        if (template == null) return;
        
        // Устанавливаем белый фон для template (списка dropdown)
        Image templateImage = template.GetComponent<Image>();
        if (templateImage != null)
        {
            templateImage.color = Color.white;
        }
        
        // Устанавливаем белый фон для Viewport
        Transform viewport = template.Find("Viewport");
        if (viewport != null)
        {
            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.color = Color.white;
            }
        }

        DropdownTemplateStyler templateStyler = template.GetComponent<DropdownTemplateStyler>();
        if (templateStyler == null)
        {
            templateStyler = template.gameObject.AddComponent<DropdownTemplateStyler>();
        }
        // Элементы списка всегда черные на белом фоне
        templateStyler.Configure(Color.black, Resources.GetBuiltinResource<Font>("Arial.ttf"), 20);

        Transform itemTransform = template.Find("Viewport/Content/Item");
        if (itemTransform != null)
        {
            Text itemLabel = itemTransform.Find("Item Label")?.GetComponent<Text>() ?? itemTransform.Find("Label")?.GetComponent<Text>();
            if (itemLabel != null)
            {
                // Элементы списка всегда черные на белом фоне
                itemLabel.color = Color.black;
                itemLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                itemLabel.fontSize = 20;
                itemLabel.alignment = TextAnchor.MiddleLeft;
                dropdown.itemText = itemLabel;
            }
        }

        Toggle itemToggle = itemTransform.GetComponent<Toggle>();
        if (itemToggle != null)
        {
            ColorBlock colors = itemToggle.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.95f);
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.colorMultiplier = 1f;
            itemToggle.colors = colors;
        }

        dropdown.RefreshShownValue();
    }
}
