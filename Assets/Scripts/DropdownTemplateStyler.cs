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
    }
}
