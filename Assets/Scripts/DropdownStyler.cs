using UnityEngine;
using UnityEngine.UI;

public static class DropdownStyler
{
    public static void Apply(Dropdown dropdown)
    {
        if (dropdown == null) return;

        if (dropdown.captionText != null)
        {
            dropdown.captionText.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            dropdown.captionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            dropdown.captionText.fontSize = 20;
            dropdown.captionText.alignment = TextAnchor.MiddleLeft;
        }

        RectTransform template = dropdown.template;
        if (template == null) return;

        DropdownTemplateStyler templateStyler = template.GetComponent<DropdownTemplateStyler>();
        if (templateStyler == null)
        {
            templateStyler = template.gameObject.AddComponent<DropdownTemplateStyler>();
        }
        templateStyler.Configure(Color.black, Resources.GetBuiltinResource<Font>("Arial.ttf"), 20);

        Transform itemTransform = template.Find("Viewport/Content/Item");
        if (itemTransform == null) return;

        Text itemLabel = itemTransform.Find("Item Label")?.GetComponent<Text>() ?? itemTransform.Find("Label")?.GetComponent<Text>();
        if (itemLabel != null)
        {
            itemLabel.color = Color.black;
            itemLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            itemLabel.fontSize = 20;
            itemLabel.alignment = TextAnchor.MiddleLeft;
            dropdown.itemText = itemLabel;
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
