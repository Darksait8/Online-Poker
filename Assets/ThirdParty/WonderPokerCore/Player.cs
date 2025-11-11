using System;

namespace WonderPokerCore
{
    public enum PlayerType
    {
        Human,
        Bot
    }

    public class Player
    {
        public string Nick { get; private set; }
        public PlayerType Type { get; }
        public string Rank { get; set; } = "Newbie";
        public int XP { get; set; }
        public int TokensCount { get; set; } = 1000;
        public GameTable Table { get; set; }
        public CardsCollection PlayerHand { get; set; } = new();
        public int SeatNr { get; set; }
        public int PlayersCurrentBet { get; set; }
        public bool Folded { get; private set; }
        public bool AllInMade { get; private set; }
        public string LastMove { get; set; }

        public Player(string nick, PlayerType type)
        {
            ChangeNick(nick);
            Type = type;
        }

        public override string ToString()
        {
            return $"{Nick} ({Type})\n{Rank}\n{XP} XP\n{TokensCount} Tokens\nCurrent table: {(Table == null ? "No table" : Table.Name)}\n";
        }

        protected bool SpendTokens(int amount, bool allowPartial = false)
        {
            if (amount <= 0)
                return true;

            if (TokensCount >= amount)
            {
                TokensCount -= amount;
                PlayersCurrentBet += amount;
                Table.TokensInGame += amount;
                return true;
            }

            if (!allowPartial || TokensCount <= 0)
                return false;

            // partial payment (used when going all-in automatically)
            Table.TokensInGame += TokensCount;
            PlayersCurrentBet += TokensCount;
            TokensCount = 0;
            AllInMade = true;
            return true;
        }

        public bool Fold()
        {
            Folded = true;
            LastMove = "Fold";
            return true;
        }

        public bool CheckOrCall(int currentBid)
        {
            int amountToMatch = Math.Max(0, currentBid - PlayersCurrentBet);
            bool success = SpendTokens(amountToMatch, allowPartial: true);
            if (success)
                LastMove = amountToMatch == 0 ? "Check" : "Call";
            return success;
        }

        public bool Raise(int currentBid, int raiseAmount)
        {
            if (raiseAmount < 0) raiseAmount = 0;

            int amountToMatch = Math.Max(0, currentBid - PlayersCurrentBet);
            int total = amountToMatch + raiseAmount;
            bool success = SpendTokens(total);
            if (success)
            {
                LastMove = raiseAmount == 0 ? "Call" : $"Raise({raiseAmount})";
                Table.CurrentBid = PlayersCurrentBet;
            }
            return success;
        }

        public bool AllIn()
        {
            if (TokensCount <= 0)
                return false;

            SpendTokens(TokensCount);
            AllInMade = true;
            LastMove = "AllIn";
            return true;
        }

        public bool PayBlind(int amount)
        {
            bool success = SpendTokens(amount);
            if (success)
            {
                int configuredBigBlind = Table?.Settings?.BigBlind ?? amount;
                LastMove = amount >= configuredBigBlind ? "BigBlind" : "SmallBlind";
                if (Table != null)
                    Table.CurrentBid = Math.Max(Table.CurrentBid, PlayersCurrentBet);
            }
            return success;
        }

        public bool ChangeNick(string newNick)
        {
            if (string.IsNullOrWhiteSpace(newNick))
            {
                if (!string.IsNullOrEmpty(Nick))
                    return false;

                Nick = "Player";
                return true;
            }

            Nick = newNick;
            return true;
        }

        public void ResetPlayerGameState()
        {
            PlayerHand = new CardsCollection();
            Folded = false;
            AllInMade = false;
            PlayersCurrentBet = 0;
            LastMove = null;
        }

        public string PlayerGameState()
        {
            return $"Player '{Nick}' game state:\n" +
                   $"Hand: {string.Join(", ", PlayerHand.Cards)}\n" +
                   $"Tokens: {TokensCount}\n" +
                   $"Current bet: {PlayersCurrentBet}\n" +
                   $"XP: {XP}";
        }
    }

    public class HumanPlayer : Player
    {
        public HumanPlayer(string nick, PlayerType type) : base(nick, type)
        {
        }

        public GameTable CreateTable(string name, GameTableSettings settings)
        {
            Table?.Remove(Nick);

            GameTable table = new(name, this);
            if (settings != null)
            {
                if (settings.MinPlayersExperience > XP)
                    settings.MinPlayersExperience = XP;

                if (settings.BigBlind > TokensCount)
                    settings.ChangeBigBlind(TokensCount);

                if (settings.MinTokens > TokensCount)
                    settings.ChangeMinTokens(TokensCount);
            }

            table.ChangeSettings(this, settings);
            return table;
        }
    }
}

