using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Помощник для быстрого создания стола и мест
/// </summary>
public class TableSetupHelper : MonoBehaviour
{
    [ContextMenu("Create Table Setup")]
    public void CreateTableSetup()
    {
        // Находим или создаем Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Создаем основной контейнер стола
        GameObject table = new GameObject("Table");
        table.transform.SetParent(canvas.transform, false);
        
        RectTransform tableRect = table.AddComponent<RectTransform>();
        tableRect.anchorMin = new Vector2(0.5f, 0.5f);
        tableRect.anchorMax = new Vector2(0.5f, 0.5f);
        tableRect.sizeDelta = new Vector2(800, 600);

        // Добавляем фон стола
        GameObject tableBG = new GameObject("TableBackground");
        tableBG.transform.SetParent(table.transform, false);
        
        RectTransform bgRect = tableBG.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImage = tableBG.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.6f, 0.2f, 1f); // Зеленый цвет стола

        // Создаем контейнер для мест
        GameObject seatsContainer = new GameObject("SeatsContainer");
        seatsContainer.transform.SetParent(table.transform, false);
        
        RectTransform seatsRect = seatsContainer.AddComponent<RectTransform>();
        seatsRect.anchorMin = Vector2.zero;
        seatsRect.anchorMax = Vector2.one;
        seatsRect.offsetMin = Vector2.zero;
        seatsRect.offsetMax = Vector2.zero;

        // Добавляем SeatsLayoutRadial
        SeatsLayoutRadial seatsLayout = seatsContainer.AddComponent<SeatsLayoutRadial>();

        // Создаем префаб места
        GameObject seatPrefab = CreateSeatPrefab();

        // Настраиваем SeatsLayoutRadial через рефлексию
        var maxSeatsField = typeof(SeatsLayoutRadial).GetField("maxSeats", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tableRectField = typeof(SeatsLayoutRadial).GetField("tableRect", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var seatPrefabField = typeof(SeatsLayoutRadial).GetField("seatPrefab", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var radiusXField = typeof(SeatsLayoutRadial).GetField("radiusX", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var radiusYField = typeof(SeatsLayoutRadial).GetField("radiusY", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        maxSeatsField?.SetValue(seatsLayout, 6);
        tableRectField?.SetValue(seatsLayout, bgRect);
        seatPrefabField?.SetValue(seatsLayout, seatPrefab.GetComponent<RectTransform>());
        radiusXField?.SetValue(seatsLayout, 300f);
        radiusYField?.SetValue(seatsLayout, 200f);

        // Создаем места
        seatsLayout.SpawnSeats();

        Debug.Log("Table setup created successfully!");
    }

    private GameObject CreateSeatPrefab()
    {
        GameObject seat = new GameObject("SeatPrefab");
        RectTransform seatRect = seat.AddComponent<RectTransform>();
        seatRect.sizeDelta = new Vector2(120, 100);

        // Добавляем NewBehaviourScript
        NewBehaviourScript seatScript = seat.AddComponent<NewBehaviourScript>();

        // Создаем фон места
        Image seatBG = seat.AddComponent<Image>();
        seatBG.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        // Создаем аватар
        GameObject avatar = new GameObject("Avatar");
        avatar.transform.SetParent(seat.transform, false);
        RectTransform avatarRect = avatar.AddComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0.5f, 0.7f);
        avatarRect.anchorMax = new Vector2(0.5f, 0.7f);
        avatarRect.sizeDelta = new Vector2(40, 40);
        Image avatarImage = avatar.AddComponent<Image>();
        avatarImage.color = Color.gray;

        // Создаем текст имени
        GameObject nameText = new GameObject("NameText");
        nameText.transform.SetParent(seat.transform, false);
        RectTransform nameRect = nameText.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.4f);
        nameRect.anchorMax = new Vector2(1f, 0.6f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        Text nameTextComp = nameText.AddComponent<Text>();
        nameTextComp.text = "Empty";
        nameTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        nameTextComp.fontSize = 12;
        nameTextComp.alignment = TextAnchor.MiddleCenter;

        // Создаем текст стека
        GameObject stackText = new GameObject("StackText");
        stackText.transform.SetParent(seat.transform, false);
        RectTransform stackRect = stackText.AddComponent<RectTransform>();
        stackRect.anchorMin = new Vector2(0f, 0.2f);
        stackRect.anchorMax = new Vector2(1f, 0.4f);
        stackRect.offsetMin = Vector2.zero;
        stackRect.offsetMax = Vector2.zero;
        Text stackTextComp = stackText.AddComponent<Text>();
        stackTextComp.text = "0";
        stackTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        stackTextComp.fontSize = 10;
        stackTextComp.alignment = TextAnchor.MiddleCenter;

        // Создаем карты
        GameObject hole1 = new GameObject("Hole1");
        hole1.transform.SetParent(seat.transform, false);
        RectTransform hole1Rect = hole1.AddComponent<RectTransform>();
        hole1Rect.anchorMin = new Vector2(0.3f, 0f);
        hole1Rect.anchorMax = new Vector2(0.3f, 0f);
        hole1Rect.sizeDelta = new Vector2(20, 30);
        Image hole1Image = hole1.AddComponent<Image>();
        hole1Image.color = Color.white;
        hole1Image.enabled = false;

        GameObject hole2 = new GameObject("Hole2");
        hole2.transform.SetParent(seat.transform, false);
        RectTransform hole2Rect = hole2.AddComponent<RectTransform>();
        hole2Rect.anchorMin = new Vector2(0.7f, 0f);
        hole2Rect.anchorMax = new Vector2(0.7f, 0f);
        hole2Rect.sizeDelta = new Vector2(20, 30);
        Image hole2Image = hole2.AddComponent<Image>();
        hole2Image.color = Color.white;
        hole2Image.enabled = false;

        // Настраиваем ссылки в NewBehaviourScript через рефлексию
        var avatarField = typeof(NewBehaviourScript).GetField("avatarImage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameField = typeof(NewBehaviourScript).GetField("nameText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var stackField = typeof(NewBehaviourScript).GetField("stackText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var hole1Field = typeof(NewBehaviourScript).GetField("hole1Image", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var hole2Field = typeof(NewBehaviourScript).GetField("hole2Image", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        avatarField?.SetValue(seatScript, avatarImage);
        nameField?.SetValue(seatScript, nameTextComp);
        stackField?.SetValue(seatScript, stackTextComp);
        hole1Field?.SetValue(seatScript, hole1Image);
        hole2Field?.SetValue(seatScript, hole2Image);

        return seat;
    }
}