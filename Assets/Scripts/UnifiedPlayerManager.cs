using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Единая система управления игроками - предотвращает дублирование
/// Добавьте этот компонент на любой GameObject в сцене
/// </summary>
public class UnifiedPlayerManager : MonoBehaviour
{
    [Header("Настройки игроков")]
    [SerializeField] private int targetPlayerCount = 3;
    [SerializeField] private int defaultStack = 1000;
    [SerializeField] private bool manageOnStart = true;
    
    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private SeatsLayoutRadial seatsLayout;
    [SerializeField] private BoardController boardController;
    private bool isManaging = false;
    
    private void Awake()
    {
        // Находим SeatsLayoutRadial
        seatsLayout = FindObjectOfType<SeatsLayoutRadial>();
        if (seatsLayout == null)
        {
            Debug.LogError("UnifiedPlayerManager: SeatsLayoutRadial не найден!");
            return;
        }
        
        // Отключаем другие системы управления игроками
        DisableOtherPlayerSystems();
        
        // Принудительно устанавливаем правильное количество мест
        SetCorrectMaxSeats();
        
        // Принудительно создаем игроков сразу
        if (manageOnStart)
        {
            ManagePlayers();
        }
        
        if (enableDebugLogs)
            Debug.Log($"UnifiedPlayerManager: Инициализирован для {targetPlayerCount} игроков");
    }
    
    private void SetCorrectMaxSeats()
    {
        if (seatsLayout == null) return;
        
        // Используем настройки из TableRuntimeConfig если они есть
        int maxSeatsToUse = targetPlayerCount;
        
        if (TableRuntimeConfig.HasConfig)
        {
            maxSeatsToUse = TableRuntimeConfig.MaxSeats;
            targetPlayerCount = maxSeatsToUse; // Обновляем targetPlayerCount тоже
            if (enableDebugLogs)
                Debug.Log($"UnifiedPlayerManager: Используем настройки из TableRuntimeConfig: {maxSeatsToUse} мест");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"UnifiedPlayerManager: TableRuntimeConfig не настроен, используем targetPlayerCount: {maxSeatsToUse}");
        }
        
        // Устанавливаем правильное количество мест через рефлексию
        var slType = typeof(SeatsLayoutRadial);
        var maxSeatsField = slType.GetField("maxSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (maxSeatsField != null)
        {
            maxSeatsField.SetValue(seatsLayout, maxSeatsToUse);
            if (enableDebugLogs)
                Debug.Log($"UnifiedPlayerManager: Установил maxSeats = {maxSeatsToUse}");
        }
    }
    
    private void Start()
    {
        if (manageOnStart)
        {
            // Ждем один кадр, чтобы SeatsLayoutRadial успел инициализироваться
            StartCoroutine(ManagePlayersAfterFrame());
        }
    }
    
    private System.Collections.IEnumerator ManagePlayersAfterFrame()
    {
        // Ждем один кадр
        yield return null;
        
        // Принудительно создаем игроков
        ManagePlayers();
        
        // Дополнительная проверка через кадр
        Invoke(nameof(CheckAndFixPlayers), 0.5f);
    }
    
    private void CheckAndFixPlayers()
    {
        if (seatsLayout != null && seatsLayout.OccupiedCount != targetPlayerCount)
        {
            if (enableDebugLogs)
                Debug.Log($"UnifiedPlayerManager: Исправляем количество игроков: {seatsLayout.OccupiedCount} -> {targetPlayerCount}");
            
            ManagePlayers();
        }
    }
    
    private void DisableOtherPlayerSystems()
    {
        // Отключаем AutoSeatFiller
        var autoSeatFiller = FindObjectOfType<AutoSeatFiller>();
        if (autoSeatFiller != null)
        {
            autoSeatFiller.enabled = false;
            if (enableDebugLogs)
                Debug.Log("UnifiedPlayerManager: Отключил AutoSeatFiller");
        }
        
        // Отключаем TableManager autoFillOnStart
        var tableManager = FindObjectOfType<TableManager>();
        if (tableManager != null)
        {
            var tmType = typeof(TableManager);
            var autoFillField = tmType.GetField("autoFillOnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (autoFillField != null)
            {
                autoFillField.SetValue(tableManager, false);
                if (enableDebugLogs)
                    Debug.Log("UnifiedPlayerManager: Отключил TableManager autoFillOnStart");
            }
        }
        
        // Отключаем TableConfigTester
        var tableConfigTester = FindObjectOfType<TableConfigTester>();
        if (tableConfigTester != null)
        {
            tableConfigTester.enabled = false;
            if (enableDebugLogs)
                Debug.Log("UnifiedPlayerManager: Отключил TableConfigTester");
        }
    }
    
    [ContextMenu("Manage Players")]
    public void ManagePlayers()
    {
        if (isManaging)
        {
            Debug.LogWarning("UnifiedPlayerManager: Управление игроками уже выполняется!");
            return;
        }
        
        isManaging = true;
        
        try
        {
            // Определяем количество игроков для управления
            int playersToManage = targetPlayerCount;
            
            // Если есть настройки из TableRuntimeConfig, используем их
            if (TableRuntimeConfig.HasConfig)
            {
                playersToManage = TableRuntimeConfig.MaxSeats;
                targetPlayerCount = playersToManage; // Обновляем targetPlayerCount
                if (enableDebugLogs)
                    Debug.Log($"UnifiedPlayerManager: Используем TableRuntimeConfig.MaxSeats = {playersToManage}");
            }
            
            if (enableDebugLogs)
                Debug.Log($"=== УПРАВЛЕНИЕ ИГРОКАМИ: {playersToManage} ===");
            
            // 1. Полностью очищаем все места
            ClearAllSeats();
            
            // 2. Добавляем нужное количество игроков
            AddPlayers(playersToManage);
            
            // 3. Проверяем результат и исправляем если нужно
            if (seatsLayout != null && seatsLayout.OccupiedCount != playersToManage)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"UnifiedPlayerManager: Неправильное количество игроков: {seatsLayout.OccupiedCount}, исправляем...");
                
                // Принудительно исправляем
                ClearAllSeats();
                AddPlayers(playersToManage);
            }
            
            // 4. Запускаем раздачу карт
            StartCardDealing();
            
            if (enableDebugLogs)
                Debug.Log($"=== УПРАВЛЕНИЕ ЗАВЕРШЕНО: {playersToManage} игроков ===");
        }
        finally
        {
            isManaging = false;
        }
    }
    
    private void ClearAllSeats()
    {
        if (seatsLayout == null) return;
        
        int clearedCount = 0;
        
        // Очищаем все места принудительно
        for (int i = 0; i < seatsLayout.transform.childCount; i++)
        {
            var seat = seatsLayout.transform.GetChild(i);
            var ui = seat.GetComponent<NewBehaviourScript>();
            if (ui != null)
            {
                ui.SetPlayer("Свободно", 0);
                ui.ShowBet(0);
                ui.SetDealer(false);
                ui.HideHoles();
                clearedCount++;
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"UnifiedPlayerManager: Очистил {clearedCount} мест");
    }
    
    private void AddPlayers(int count)
    {
        if (seatsLayout == null) return;
        
        int addedCount = 0;
        
        for (int i = 0; i < count; i++)
        {
            string playerName = $"Игрок {i + 1}";
            bool success = seatsLayout.TryJoin(playerName, defaultStack);
            
            if (success)
            {
                addedCount++;
                if (enableDebugLogs)
                    Debug.Log($"UnifiedPlayerManager: Добавлен {playerName}");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"UnifiedPlayerManager: Не удалось добавить {playerName}");
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"UnifiedPlayerManager: Добавлено {addedCount} из {count} игроков");
    }
    
    [ContextMenu("Clear All Players")]
    public void ClearAllPlayers()
    {
        if (enableDebugLogs)
            Debug.Log("=== ОЧИСТКА ВСЕХ ИГРОКОВ ===");
        
        ClearAllSeats();
        
        if (enableDebugLogs)
            Debug.Log("=== ОЧИСТКА ЗАВЕРШЕНА ===");
    }
    
    [ContextMenu("Set Player Count")]
    public void SetPlayerCount(int count)
    {
        targetPlayerCount = Mathf.Clamp(count, 0, 9);
        
        if (enableDebugLogs)
            Debug.Log($"UnifiedPlayerManager: Установлено количество игроков: {targetPlayerCount}");
        
        ManagePlayers();
    }
    
    // Публичные методы для внешнего управления
    public void SetTargetPlayerCount(int count)
    {
        targetPlayerCount = Mathf.Clamp(count, 0, 9);
        ManagePlayers();
    }
    
    public int GetCurrentPlayerCount()
    {
        if (seatsLayout == null) return 0;
        return seatsLayout.OccupiedCount;
    }
    
    public int GetTargetPlayerCount()
    {
        return targetPlayerCount;
    }
    
    private void StartCardDealing()
    {
        if (enableDebugLogs)
            Debug.Log("UnifiedPlayerManager: Запускаем раздачу карт");
        
        // Находим GameStateMachine и запускаем раздачу
        var gameStateMachine = FindObjectOfType<GameStateMachine>();
        if (gameStateMachine != null)
        {
            gameStateMachine.StartHand();
            if (enableDebugLogs)
                Debug.Log("UnifiedPlayerManager: GameStateMachine.StartHand() вызван");
        }
        else
        {
            // Если GameStateMachine не найден, раздаем карты напрямую
            DealCardsDirectly();
        }
    }
    
    private void DealCardsDirectly()
    {
        if (enableDebugLogs)
            Debug.Log("UnifiedPlayerManager: Раздаем карты напрямую");
        
        var deck = new Deck();
        deck.Reset();
        deck.Shuffle();
        
        // Сначала раздаем только карты игрокам
        var occupiedSeats = seatsLayout.GetOccupiedSeats();
        foreach (var ui in occupiedSeats)
        {
            if (ui == null) continue;
            
            // Раздаем по 2 карты каждому игроку
            Card card1 = deck.DrawCard();
            Card card2 = deck.DrawCard();
            // NewBehaviourScript использует ShowHole для отображения карманных карт
            ui.ShowHole(card1, card2);
        }
        
        StartCoroutine(DealCommunityCardsSequence(deck));
    }
    
    private IEnumerator DealCommunityCardsSequence(Deck deck)
    {
        // Ждем действий игроков перед флопом
        yield return new WaitUntil(() => AllPlayersActed());
        
        // Флоп
        DealFlop(deck);
        yield return new WaitUntil(() => AllPlayersActed());
        
        // Терн
        DealTurn(deck);
        yield return new WaitUntil(() => AllPlayersActed());
        
        // Ривер
        DealRiver(deck);
    }
    
    private void DealFlop(Deck deck)
    {
        Card[] flopCards = new Card[3];
        for (int i = 0; i < 3; i++)
        {
            flopCards[i] = deck.DrawCard();
        }
        boardController.SetFlopCards(flopCards);
    }
    
    private void DealTurn(Deck deck)
    {
        Card turnCard = deck.DrawCard();
        boardController.SetTurnCard(turnCard);
    }
    
    private void DealRiver(Deck deck)
    {
        Card riverCard = deck.DrawCard();
        boardController.SetRiverCard(riverCard);
    }
    
    private bool AllPlayersActed()
    {
        var occupiedSeats = seatsLayout.GetOccupiedSeats();
        foreach (var ui in occupiedSeats)
        {
            if (ui == null) continue;

            // Попробуем через рефлексию получить свойство HasActed у UI (если есть)
            var prop = ui.GetType().GetProperty("HasActed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop != null)
            {
                var val = prop.GetValue(ui);
                if (val is bool b && !b)
                    return false;
            }
            else
            {
                // Если у UI нет свойства HasActed, считаем что игрок сделал ход (fallback)
                continue;
            }
        }
        return true;
    }
}
