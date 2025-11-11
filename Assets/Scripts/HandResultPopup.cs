using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HandResultPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text messageText;
    [SerializeField] private float defaultDuration = 3f;

    private Coroutine showRoutine;

    private void Awake()
    {
        canvasGroup ??= GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (messageText == null)
        {
            messageText = GetComponentInChildren<Text>();
            if (messageText == null)
                messageText = CreateDefaultText();
        }

        HideImmediate();
    }

    public void Show(string message, float duration = -1f)
    {
        if (messageText == null)
            return;

        messageText.text = message;
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        float useDuration = duration > 0f ? duration : defaultDuration;
        showRoutine = StartCoroutine(HideAfterDelay(useDuration));
    }

    public void HideImmediate()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (messageText != null)
            messageText.text = string.Empty;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideImmediate();
    }

    private Text CreateDefaultText()
    {
        var textGO = new GameObject("Message", typeof(RectTransform), typeof(Text));
        var rect = textGO.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 180f);

        var text = textGO.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 36;
        text.color = Color.white;
        text.supportRichText = true;

        return text;
    }

    public static HandResultPopup CreateDefault(Transform parent)
    {
        var go = new GameObject("HandResultPopup", typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(640f, 200f);

        var image = go.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.75f);

        var canvasGroup = go.AddComponent<CanvasGroup>();
        var popup = go.AddComponent<HandResultPopup>();
        popup.canvasGroup = canvasGroup;
        popup.messageText = popup.CreateDefaultText();

        return popup;
    }
}

