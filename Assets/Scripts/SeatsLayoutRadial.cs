using UnityEngine;
using UnityEngine.UI;

public class SeatsLayoutRadial : MonoBehaviour
{
    [Header("Настройки раскладки")]
    [SerializeField, Range(2, 9)] private int maxSeats = 9; // максимум мест на столе
    [SerializeField] private RectTransform tableRect;   // перетащи сюда RectTransform твоего стола (Table Panel)
    [SerializeField] private RectTransform seatPrefab;  // перетащи сюда префаб места (объект с NewBehaviourScript)
    [SerializeField] private float radiusX = 520f;      // радиус по X вокруг стола
    [SerializeField] private float radiusY = 300f;      // радиус по Y вокруг стола
    [SerializeField] private float startAngleDeg = 90f; // стартовый угол (90 = сверху по центру)
    [SerializeField] private bool clockwise = true;     // направление обхода по часовой
    [SerializeField] private bool snapToPixels = true;  // выравнивать позиции к пиксельной сетке

    [Header("Тестовые данные")]
    [SerializeField] private bool spawnOnStart = true;  // создать места при старте

    [Header("Карманные карты — раскладка")]
    [SerializeField] private bool applyHoleLayout = true;
    [SerializeField] private float holeDistance = 28f;     // расстояние от центра стула к центру пары карт
    [SerializeField] private float holeSpacing = 22f;      // расстояние между картами


    // Рантайм: текущие занятые места
    private readonly System.Collections.Generic.List<NewBehaviourScript> occupied = new System.Collections.Generic.List<NewBehaviourScript>();
    public int OccupiedCount => occupied.Count;
    public int MaxSeats => maxSeats;
    public System.Action<int> OnOccupiedChanged;

    private void Start()
    {
        Debug.Log($"SeatsLayoutRadial: TableRuntimeConfig.HasConfig = {TableRuntimeConfig.HasConfig}");
        Debug.Log($"SeatsLayoutRadial: maxSeats из инспектора = {maxSeats}");

        // Если есть конфиг из TableRuntimeConfig, используем его
        if (TableRuntimeConfig.HasConfig)
        {
            int configMaxSeats = TableRuntimeConfig.MaxSeats;
            Debug.Log($"SeatsLayoutRadial: TableRuntimeConfig.MaxSeats = {configMaxSeats}");
            
            // Проверяем, что значение из конфига разумное
            if (configMaxSeats >= 2 && configMaxSeats <= 9)
            {
                maxSeats = configMaxSeats;
                Debug.Log($"SeatsLayoutRadial: Используем maxSeats из TableRuntimeConfig = {maxSeats}");
            }
            else
            {
                Debug.LogWarning($"SeatsLayoutRadial: TableRuntimeConfig.MaxSeats = {configMaxSeats} вне диапазона [2,9], используем значение из инспектора = {maxSeats}");
            }
        }
        else
        {
            Debug.Log($"SeatsLayoutRadial: TableRuntimeConfig не настроен, используем значение из инспектора = {maxSeats}");
        }

        if (spawnOnStart)
            SpawnSeats();
    }

    [ContextMenu("Spawn Seats")]
    [UnityEngine.ContextMenu("Recreate Seats")]
    public void SpawnSeats()
    {
        if (seatPrefab == null)
        {
            Debug.LogWarning("SeatsLayoutRadial: не назначен seatPrefab");
            return;
        }

        // Удаляем старых детей, если были
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (Application.isEditor)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
        }
        occupied.Clear();
        OnOccupiedChanged?.Invoke(occupied.Count);

        float dir = clockwise ? -1f : 1f;
        for (int i = 0; i < maxSeats; i++)
        {
            float t = i / (float)maxSeats;
            float angleRad = Mathf.Deg2Rad * (startAngleDeg + dir * t * 360f);
            Vector2 p = new Vector2(Mathf.Cos(angleRad) * radiusX, Mathf.Sin(angleRad) * radiusY);

            RectTransform seat = Instantiate(seatPrefab, transform);
            seat.name = $"Seat_{i + 1}";
            seat.anchorMin = seat.anchorMax = new Vector2(0.5f, 0.5f);
            seat.pivot = new Vector2(0.5f, 0.5f);
            if (snapToPixels)
            {
                p = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
            }
            seat.anchoredPosition = p;

            // Инициализация места как свободного
            var ui = seat.GetComponent<NewBehaviourScript>();
            if (ui != null)
            {
                ui.SetPlayer("Свободно", 0);
                ui.ShowBet(0);
                ui.SetDealer(false);
                ui.HideHoles();

                // Настроим расположение и поворот карманных карт в зависимости от сектора
                if (applyHoleLayout)
                {
                    float angleDeg = startAngleDeg + dir * t * 360f;
                    float rad = Mathf.Deg2Rad * angleDeg;
                    // направление К центру стола (вектор от места к центру)
                    Vector2 inward = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
                    
                    // Поворот карт: карты должны быть ПЕРПЕНДИКУЛЯРНЫ игроку
                    // Это означает, что карты должны быть повернуты на 90° относительно направления к центру
                    float rot = angleDeg + 90f; // Добавляем 90° чтобы карты были перпендикулярны
                    
                    // Нормализуем угол поворота
                    rot = Mathf.Repeat(rot, 360f);
                    
                    Debug.Log($"Seat_{i + 1}: angleDeg = {angleDeg:F1}°, rot = {rot:F1}°");
                    ui.ConfigureHoleLayout(inward, rot, holeDistance, holeSpacing);
                }
            }
        }
    }

    // Найти первое свободное место (по индексу ребёнка)
    private int FindFirstFreeIndex()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var ui = transform.GetChild(i).GetComponent<NewBehaviourScript>();
            if (ui != null && !occupied.Contains(ui))
                return i;
        }
        return -1;
    }

    public bool TryJoin(string playerName, int stack)
    {
        if (occupied.Count >= maxSeats) return false;
        int idx = FindFirstFreeIndex();
        if (idx < 0) return false;
        var ui = transform.GetChild(idx).GetComponent<NewBehaviourScript>();
        if (ui == null) return false;
        ui.SetPlayer(playerName, stack);
        occupied.Add(ui);
        OnOccupiedChanged?.Invoke(occupied.Count);
        return true;
    }

    public bool Leave(string playerName)
    {
        for (int i = 0; i < occupied.Count; i++)
        {
            var ui = occupied[i];
            // сравним по имени
            // если используешь уникальные ID — заменим логикой позже
            // здесь просто очищаем место
            // (имя берём из TMP или Text)
            bool match = false;
            var nameField = ui.GetType().GetField("nameTextTMP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(ui) as TMPro.TMP_Text;
            if (nameField != null) match = nameField.text == playerName;
            if (!match)
            {
                var legacy = ui.GetType().GetField("nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(ui) as UnityEngine.UI.Text;
                if (legacy != null) match = legacy.text == playerName;
            }
            if (match)
            {
                ui.SetPlayer("Свободно", 0);
                ui.ShowBet(0);
                ui.SetDealer(false);
                occupied.RemoveAt(i);
                OnOccupiedChanged?.Invoke(occupied.Count);
                return true;
            }
        }
        return false;
    }

    // Перечень занятых мест (UI контроллеров)
    public System.Collections.Generic.IReadOnlyList<NewBehaviourScript> GetOccupiedSeats()
    {
        return occupied;
    }
    
    /// <summary>
    /// Обновляет UI конкретного игрока
    /// </summary>
    public void UpdatePlayerUI(object player)
    {
        // Простая заглушка - в реальной игре здесь должна быть логика обновления UI
        Debug.Log($"UpdatePlayerUI called for player: {player}");
    }
    
    /// <summary>
    /// Обновляет UI всех игроков
    /// </summary>
    public void UpdateAllPlayersUI()
    {
        // Простая заглушка - в реальной игре здесь должна быть логика обновления UI всех игроков
        Debug.Log("UpdateAllPlayersUI called");
    }
}


