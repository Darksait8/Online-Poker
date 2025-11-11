using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WonderPokerCore
{
    /// <summary>
    /// Represents a single poker table with players and current game metadata.
    /// This is a cleaned-up version of the original server class – all networking calls were removed.
    /// </summary>
    public class GameTable
    {
        public string Name { get; private set; }
        public HumanPlayer Owner { get; private set; }
        public List<Player> Players { get; } = new();
        public CardsCollection ShownHelpingCards { get; set; } = new();
        public int TokensInGame { get; set; }
        public int CurrentBid { get; set; }
        public GameTableSettings Settings { get; private set; } = new();
        public bool IsGameActive { get; set; }
        public bool AlreadyHasGameThread { get; set; } // left for compatibility with original logic

        public GameTable(string name, HumanPlayer owner)
        {
            ChangeName(name);
            TokensInGame = 0;
            CurrentBid = 0;
            Owner = owner;
            AddPlayer(owner);
            IsGameActive = false;
            AlreadyHasGameThread = false;
        }

        public event Action<Player> PlayerAdded;
        public event Action<Player> PlayerRemoved;
        public event Action SettingsChanged;

        private bool PlayerSitsAtTable(Player player) => Players.Contains(player);

        public bool AddPlayer(Player player)
        {
            if (player == null) return false;

            if (IsGameActive)
                return false;

            if (PlayerSitsAtTable(player))
                return false;

            if (Players.Count >= Settings.MaxPlayersCountInGame)
                return false;

            if (Settings.MinPlayersExperience > player.XP)
                return false;

            if (Settings.MinTokens > player.TokensCount)
                return false;

            int seatIndex = GetFirstFreeSeat();
            player.SeatNr = seatIndex;
            Players.Add(player);
            player.Table = this;

            if (Owner == null && player.Type == PlayerType.Human)
                ChangeOwner((HumanPlayer)player);

            PlayerAdded?.Invoke(player);
            return true;
        }

        public bool Remove(string playerNick)
        {
            Player player = Players.Find(p => p.Nick == playerNick);
            if (player == null)
                return false;

            Players.Remove(player);
            player.Table = null;

            if (player == Owner)
            {
                HumanPlayer newOwner = Players.OfType<HumanPlayer>().FirstOrDefault();
                ChangeOwner(newOwner);
            }

            PlayerRemoved?.Invoke(player);
            return true;
        }

        public bool ChangeName(string newName)
        {
            Name = newName;
            return true;
        }

        public bool ChangeOwner(HumanPlayer newOwner)
        {
            Owner = newOwner;
            if (newOwner != null && !PlayerSitsAtTable(newOwner))
            {
                AddPlayer(newOwner);
            }

            return true;
        }

        public bool ChangeSettings(Player requestingPlayer, GameTableSettings settings)
        {
            if (Owner == null || requestingPlayer == null)
                return false;

            if (requestingPlayer.Nick != Owner.Nick)
                return false;

            Settings = settings ?? new GameTableSettings();
            SettingsChanged?.Invoke();
            return true;
        }

        public int GetPlayerTypeCount(PlayerType type) => Players.Count(p => p.Type == type);

        public List<int> GetFreeSeatsList()
        {
            List<int> allSeats = Enumerable.Range(0, Settings.MaxPlayersCountInGame).ToList();
            List<int> takenSeats = Players.Select(p => p.SeatNr).ToList();
            return allSeats.Except(takenSeats).ToList();
        }

        public int GetFirstFreeSeat()
        {
            List<int> freeSeats = GetFreeSeatsList();
            return freeSeats.Count > 0 ? freeSeats.First() : Players.Count;
        }

        public void SortPlayersBySeats()
        {
            Players.Sort((p1, p2) => p1.SeatNr.CompareTo(p2.SeatNr));
        }

        public void ResetGameState()
        {
            TokensInGame = 0;
            CurrentBid = 0;
            ShownHelpingCards = new CardsCollection();
            Players.ForEach(p => p.ResetPlayerGameState());
        }

        public string TableGameState()
        {
            return "Table '" + Name + "' game state:\n"
                   + "Cards: " + string.Join(", ", ShownHelpingCards.Cards)
                   + "\nTokens in game: " + TokensInGame
                   + "\nCurrent bid: " + CurrentBid;
        }

        public override string ToString()
        {
            return new StringBuilder()
                .AppendLine($"Name: {Name}")
                .AppendLine($"Owner: {(Owner == null ? "No owner" : Owner.Nick)}")
                .AppendLine($"Human count: {GetPlayerTypeCount(PlayerType.Human)}")
                .AppendLine($"Bots count: {GetPlayerTypeCount(PlayerType.Bot)}")
                .AppendLine($"Min XP: {Settings.MinPlayersExperience}")
                .AppendLine($"Min Chips: {Settings.MinTokens}")
                .AppendLine($"Big Blind: {Settings.BigBlind}")
                .ToString();
        }

        public string MessageGameState()
        {
            return ":Name:" + Name
                   + ":Cards:" + ShownHelpingCards
                   + ":Tokens in game:" + TokensInGame
                   + ":Current bid:" + CurrentBid;
        }
    }
}

