using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerServer
{
    /// <summary>
    /// Представляет игровую сессию (стол) с несколькими игроками
    /// </summary>
    public class GameSession
    {
        public List<ClientConnection> Players { get; private set; }
        public bool IsGameActive { get; private set; }
        public bool IsFull => Players.Count >= 6; // Максимум 6 игроков
        public string CurrentPhase { get; private set; }
        public int CurrentBet { get; private set; }
        public int Pot { get; private set; }
        public int DealerIndex { get; private set; }
        public int SmallBlind { get; private set; } = 10;
        public int BigBlind { get; private set; } = 20;
        public List<string> CommunityCards { get; private set; }
        
        private Dictionary<ClientConnection, PlayerState> playerStates;
        private int currentPlayerIndex;
        private Random random;
        
        public GameSession()
        {
            Players = new List<ClientConnection>();
            playerStates = new Dictionary<ClientConnection, PlayerState>();
            CommunityCards = new List<string>();
            random = new Random();
            CurrentPhase = "waiting";
        }
        
        public void AddPlayer(ClientConnection player)
        {
            if (!Players.Contains(player) && !IsFull)
            {
                Players.Add(player);
                playerStates[player] = new PlayerState
                {
                    Stack = player.Stack,
                    CurrentBet = 0,
                    Folded = false,
                    HasActed = false
                };
            }
        }
        
        public void RemovePlayer(ClientConnection player)
        {
            Players.Remove(player);
            playerStates.Remove(player);
        }
        
        public void StartNewHand()
        {
            if (Players.Count < 2) return;
            
            IsGameActive = true;
            CurrentPhase = "preflop";
            CurrentBet = 0;
            Pot = 0;
            
            // Сбрасываем состояние всех игроков
            foreach (var player in Players)
            {
                var state = playerStates[player];
                state.Folded = false;
                state.CurrentBet = 0;
                state.HasActed = false;
            }
            
            // Устанавливаем дилера
            DealerIndex = (DealerIndex + 1) % Players.Count;
            
            // Собираем блайнды
            int smallBlindIndex = (DealerIndex + 1) % Players.Count;
            int bigBlindIndex = (DealerIndex + 2) % Players.Count;
            
            if (Players.Count >= 2)
            {
                var sbState = playerStates[Players[smallBlindIndex]];
                var bbState = playerStates[Players[bigBlindIndex]];
                
                sbState.CurrentBet = SmallBlind;
                sbState.Stack -= SmallBlind;
                Pot += SmallBlind;
                
                bbState.CurrentBet = BigBlind;
                bbState.Stack -= BigBlind;
                Pot += BigBlind;
                
                CurrentBet = BigBlind;
            }
            
            currentPlayerIndex = (bigBlindIndex + 1) % Players.Count;
            CommunityCards.Clear();
        }
        
        public void ProcessPlayerAction(ClientConnection player, string action, int amount)
        {
            if (!playerStates.ContainsKey(player))
                return;
            
            var state = playerStates[player];
            
            switch (action.ToLower())
            {
                case "fold":
                    state.Folded = true;
                    state.HasActed = true;
                    break;
                    
                case "check":
                case "call":
                    int callAmount = CurrentBet - state.CurrentBet;
                    if (callAmount > 0 && state.Stack >= callAmount)
                    {
                        state.Stack -= callAmount;
                        state.CurrentBet += callAmount;
                        Pot += callAmount;
                    }
                    state.HasActed = true;
                    break;
                    
                case "raise":
                case "bet":
                    int raiseAmount = amount - state.CurrentBet;
                    if (raiseAmount > 0 && state.Stack >= raiseAmount)
                    {
                        state.Stack -= raiseAmount;
                        state.CurrentBet = amount;
                        Pot += raiseAmount;
                        CurrentBet = amount;
                        
                        // Сбрасываем флаг hasActed для всех остальных игроков
                        foreach (var p in Players)
                        {
                            if (p != player)
                                playerStates[p].HasActed = false;
                        }
                    }
                    state.HasActed = true;
                    break;
            }
        }
        
        public bool CheckRoundComplete()
        {
            // Проверяем, все ли активные игроки сделали действие
            int activePlayers = Players.Count(p => !playerStates[p].Folded);
            int actedPlayers = Players.Count(p => !playerStates[p].Folded && playerStates[p].HasActed);
            
            // Также проверяем, что все ставки равны
            bool allBetsEqual = Players
                .Where(p => !playerStates[p].Folded)
                .Select(p => playerStates[p].CurrentBet)
                .Distinct()
                .Count() <= 1;
            
            return activePlayers > 0 && actedPlayers == activePlayers && allBetsEqual;
        }
        
        public void AdvancePhase()
        {
            switch (CurrentPhase)
            {
                case "preflop":
                    CurrentPhase = "flop";
                    // Добавляем 3 карты на стол (упрощенно)
                    CommunityCards.Add("flop1");
                    CommunityCards.Add("flop2");
                    CommunityCards.Add("flop3");
                    ResetBettingRound();
                    break;
                    
                case "flop":
                    CurrentPhase = "turn";
                    CommunityCards.Add("turn");
                    ResetBettingRound();
                    break;
                    
                case "turn":
                    CurrentPhase = "river";
                    CommunityCards.Add("river");
                    ResetBettingRound();
                    break;
                    
                case "river":
                    CurrentPhase = "showdown";
                    EndHand();
                    break;
            }
        }
        
        private void ResetBettingRound()
        {
            CurrentBet = 0;
            foreach (var player in Players)
            {
                var state = playerStates[player];
                state.CurrentBet = 0;
                state.HasActed = false;
            }
        }
        
        private void EndHand()
        {
            // Упрощенная логика: определяем победителя случайно
            var activePlayers = Players.Where(p => !playerStates[p].Folded).ToList();
            if (activePlayers.Count > 0)
            {
                var winner = activePlayers[random.Next(activePlayers.Count)];
                var winnerState = playerStates[winner];
                winnerState.Stack += Pot;
            }
            
            Pot = 0;
            IsGameActive = false;
            CurrentPhase = "waiting";
        }
        
        public string GetPlayerCard1(ClientConnection player)
        {
            // Упрощенная генерация карт
            return $"card_{random.Next(52)}";
        }
        
        public string GetPlayerCard2(ClientConnection player)
        {
            return $"card_{random.Next(52)}";
        }
        
        private class PlayerState
        {
            public int Stack { get; set; }
            public int CurrentBet { get; set; }
            public bool Folded { get; set; }
            public bool HasActed { get; set; }
        }
    }
}

