namespace WonderPokerCore
{
    public enum GameMode
    {
        NoBots,
        YouAndBots,
        Mixed
    }

    /// <summary>
    /// Configuration for a table. Kept close to the source project so that game logic behaves identically.
    /// </summary>
    public class GameTableSettings
    {
        public const int MaxPlayersCountByRules = 9;
        public const int MinPlayersCountByRules = 2;

        public GameMode Mode { get; set; }
        public int MaxPlayersCountInGame { get; set; }
        /// <summary>
        /// Number of bots sitting at the table when the game starts.
        /// </summary>
        public int BotsNumberOnStart { get; set; }
        public int MinPlayersExperience { get; set; }
        public int BigBlind { get; private set; }
        public int MinTokens { get; private set; }

        public GameTableSettings()
        {
            Mode = GameMode.Mixed;
            MaxPlayersCountInGame = MaxPlayersCountByRules;
            BotsNumberOnStart = 0;
            MinPlayersExperience = 0;
            BigBlind = 0;
            MinTokens = 0;
        }

        public override string ToString()
        {
            return $"Game mode: {Mode}\n" +
                   $"Max player count: {MaxPlayersCountInGame}\n" +
                   $"Bots starting number: {BotsNumberOnStart}\n" +
                   $"Min XP: {MinPlayersExperience}\n" +
                   $"Min Chips: {MinTokens}\n" +
                   $"Big Blind: {BigBlind}\n";
        }

        public bool ChangeMode(GameMode mode)
        {
            Mode = mode;
            if (mode == GameMode.NoBots)
            {
                BotsNumberOnStart = 0;
            }
            else if (mode == GameMode.YouAndBots)
            {
                BotsNumberOnStart = MinPlayersCountByRules - 1;
            }

            return true;
        }

        public bool ChangeMaxPlayers(int maxPlayers)
        {
            if (maxPlayers < MinPlayersCountByRules)
            {
                MaxPlayersCountInGame = MinPlayersCountByRules;
                return true;
            }

            if (maxPlayers > MaxPlayersCountByRules)
            {
                MaxPlayersCountInGame = MaxPlayersCountByRules;
                return true;
            }

            MaxPlayersCountInGame = maxPlayers;
            return true;
        }

        public bool ChangeBotsNumber(int botsNumber)
        {
            if (Mode == GameMode.NoBots)
                return false;

            if (botsNumber < 0)
            {
                BotsNumberOnStart = Mode == GameMode.YouAndBots
                    ? MinPlayersCountByRules - 1
                    : 0;
                return true;
            }

            if (botsNumber > MaxPlayersCountByRules - 1)
            {
                BotsNumberOnStart = MaxPlayersCountByRules - 1;
                return true;
            }

            BotsNumberOnStart = botsNumber;
            return true;
        }

        public bool ChangeMinExperience(int minXp)
        {
            MinPlayersExperience = minXp;
            return true;
        }

        public bool ChangeMinTokens(int minTokens)
        {
            MinTokens = minTokens;
            return true;
        }

        public bool ChangeBigBlind(int bigBlind)
        {
            BigBlind = bigBlind;
            return true;
        }
    }
}

