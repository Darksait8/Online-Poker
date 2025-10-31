using System.Collections.Generic;
using UnityEngine;

public class TableManager : MonoBehaviour
{
    [Header("Правила стола")]
    [SerializeField, Range(2, 9)] private int minPlayers = 2;
    [SerializeField, Range(2, 9)] private int maxPlayers = 9;
    [SerializeField] private bool autoFillOnStart = true;

    [Header("Ссылки")]
    [SerializeField] private SeatsLayoutRadial seatsLayout;

    private readonly HashSet<string> seatedNames = new HashSet<string>();
    private int autoNameCounter = 1;

    private void Reset()
    {
        seatsLayout = GetComponentInChildren<SeatsLayoutRadial>();
    }

    private void Awake()
    {
        if (seatsLayout == null)
            seatsLayout = GetComponentInChildren<SeatsLayoutRadial>();
        if (seatsLayout == null)
            seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
    }

    private void Start()
    {
        // применяем конфиг, если он есть
        if (TableRuntimeConfig.HasConfig)
        {
            maxPlayers = Mathf.Clamp(TableRuntimeConfig.MaxSeats, 2, 9);
            Debug.Log($"TableManager: Применяем конфиг из TableRuntimeConfig, maxPlayers = {maxPlayers}");
        }
        else
        {
            Debug.Log($"TableManager: TableRuntimeConfig.HasConfig = false, используем настройки из инспектора maxPlayers = {maxPlayers}");
        }

        // НЕ перезаписываем настройки SeatsLayoutRadial, если они уже правильные
        // SeatsLayoutRadial сам решает, использовать ли TableRuntimeConfig или свои настройки

        // Проверяем, есть ли AutoSeatFiller - если есть, то не добавляем игроков сами
        var autoSeatFiller = GetComponent<AutoSeatFiller>();
        if (autoSeatFiller != null)
        {
            Debug.Log("TableManager: Найден AutoSeatFiller, пропускаем автоматическое добавление игроков");
            return;
        }

        if (autoFillOnStart)
        {
            Debug.Log($"TableManager: Добавляем игроков до минимума: minPlayers = {minPlayers}");
            EnsureMinPlayers();
        }
    }

    public bool JoinPlayer(string playerName, int stack)
    {
        if (seatedNames.Contains(playerName)) return false;
        bool ok = seatsLayout != null && seatsLayout.TryJoin(playerName, stack);
        if (ok) seatedNames.Add(playerName);
        return ok;
    }

    public bool LeavePlayer(string playerName)
    {
        if (!seatedNames.Contains(playerName)) return false;
        bool ok = seatsLayout != null && seatsLayout.Leave(playerName);
        if (ok) seatedNames.Remove(playerName);
        return ok;
    }

    // Добавить гостей до минимума
    public void EnsureMinPlayers()
    {
        int retries = 100;
        while (seatedNames.Count < Mathf.Min(minPlayers, maxPlayers) && retries-- > 0)
        {
            string name = $"Игрок {autoNameCounter++}";
            if (JoinPlayer(name, 1000) == false)
                continue;
        }
    }
}


