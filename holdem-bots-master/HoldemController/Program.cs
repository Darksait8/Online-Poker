using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using HoldemController.ConsoleDisplay;
using HoldemController.Logging;
using HoldemPlayerContract;

namespace HoldemController
{
    internal class Program
    {
        private readonly List<IDisplay> _displays = new List<IDisplay>();
        private readonly List<IEventHandler> _eventHandlers = new List<IEventHandler>();

        private ServerHoldemPlayer[] _players;
        private List<ServerHoldemPlayer> _allBots;
        private readonly Deck _deck = new Deck();
        private readonly PotManager _potMan = new PotManager();
        private int _totalMoneyInGame;

        private int _dealerPlayerNum;
        private int _smallBlindPlayerId;
        private int _bigBlindPlayerNum;

        // Game config - default values
        private int _numPlayers;
        private int _smallBlindSize = 100;
        private int _bigBlindSize = 200;
        private int _startingStack = 5000;
        private int _maxNumRaisesPerBettingRound = -1; 
        private int _maxHands = -1;
        private int _doubleBlindFrequency = -1;
        private int _botTimeOutMilliSeconds = 5000;
        private bool _bRandomDealer = true;
        private bool _bRandomSeating = true;
        private bool _bPauseAfterEachHand;
        private int _sleepAfterActionMilliSeconds;
        private bool _bGraphicsDisplay;

        public bool GraphicsDisplay() { return _bGraphicsDisplay; }
        
        static void Main(string [] args)
        {
            bool bGraphicsDisplay = false;

            try
            {
                string sConfigFile;

                if(args.Length > 0)
                {
                    sConfigFile = args[0];
                }
                else
                {
                    sConfigFile = "HoldemConfig.xml";
                }

                var sOutputBase = Path.GetFileNameWithoutExtension(sConfigFile);

                int numGames = 1; 

                if(args.Length > 1)
                {
                    if(!Int32.TryParse(args[1], out numGames))
                    {
                        numGames = 1;
                    }
                }

                int gameNum = 0;

                while(gameNum < numGames)
                {
                    var prog = new Program();

                    prog.PlayGame(sConfigFile, sOutputBase + "." + gameNum);
                    bGraphicsDisplay = prog.GraphicsDisplay();

                    Logger.Close();
                    TimingLogger.Close();
                    gameNum++;
                }
            }
            catch (Exception e)
            {
                var sExceptionMessage = "EXCEPTION : " + e + "\nPlease send gamelog.txt to gmcdonald73@gmail.com";
                Console.WriteLine(sExceptionMessage);
            }
            finally
            {
                Logger.Close();
                TimingLogger.Close();

                if(bGraphicsDisplay)
                {
                    Console.SetCursorPosition(0,0);
                }

                Console.WriteLine("-- press any key to exit --");
                Console.ReadKey();
            }
        }

        public void PlayGame(string sConfigFile, string sOutputBase)
        {
            LoadConfig(sConfigFile, sOutputBase);

            var bDone = false;
            var handNum = 0;
            var rnd = new Random();

            _totalMoneyInGame = _players.Sum(p => p.StackSize);

            _dealerPlayerNum = 0;

            if(_bRandomDealer)
            {
                int dealerIncr = rnd.Next(GetNumActivePlayers());

                while(dealerIncr>0)
                {
                    _dealerPlayerNum = GetNextActivePlayer(_dealerPlayerNum);
                    dealerIncr--;
                }
            }

            _smallBlindPlayerId = GetNextActivePlayer(_dealerPlayerNum);
            _bigBlindPlayerNum = GetNextActivePlayer(_smallBlindPlayerId);

            BroadcastInitialisation();

            while (!bDone)
            {
                var communityCards = new Card[5];
                int lastToAct;

                // init round for each player
                handNum++;
                InitialiseHand(handNum);

                BroadcastBeginHand();

                // deal out hole cards to all active players
                DealHoleCards();

                BroadcastBeginStage(Stage.StagePreflop, communityCards);
                // First betting round - get player actions and broadcast to all players until betting round done
                DoBettingRound(Stage.StagePreflop, out lastToAct);
                BroadcastEndStage();

                if (GetNumActivePlayers() > 1)
                {
                    // deal flop
                    communityCards[0] = _deck.DealCard();
                    BroadcastBoardCard(EBoardCardType.BoardFlop1, communityCards[0]);

                    communityCards[1] = _deck.DealCard();
                    BroadcastBoardCard(EBoardCardType.BoardFlop2, communityCards[1]);

                    communityCards[2] = _deck.DealCard();
                    BroadcastBoardCard(EBoardCardType.BoardFlop3, communityCards[2]);

                    BroadcastBeginStage(Stage.StageFlop, communityCards);
                    // Second betting round - get player actions and broadcast to all players until betting round done
                    if (IsBettingRoundRequired())
                    {
                        DoBettingRound(Stage.StageFlop, out lastToAct);
                    }
                    BroadcastEndStage();
                }

                if (GetNumActivePlayers() > 1)
                {
                    // deal turn
                    communityCards[3] = _deck.DealCard();
                    BroadcastBoardCard(EBoardCardType.BoardTurn, communityCards[3]);

                    BroadcastBeginStage(Stage.StageTurn, communityCards);
                    // Third betting round - get player actions and broadcast to all players until betting round done
                    if (IsBettingRoundRequired())
                    {
                        DoBettingRound(Stage.StageTurn, out lastToAct);
                    }
                    BroadcastEndStage();
                }

                if (GetNumActivePlayers() > 1)
                {
                    // deal river
                    communityCards[4] = _deck.DealCard();
                    BroadcastBoardCard(EBoardCardType.BoardRiver, communityCards[4]);

                    BroadcastBeginStage(Stage.StageRiver, communityCards);
                    // Fourth betting round - get player actions and broadcast to all players until betting round done
                    if (IsBettingRoundRequired())
                    {
                        DoBettingRound(Stage.StageRiver, out lastToAct);
                    }
                    BroadcastEndStage();
                }

                // ViewCash();

                var handRanker = new HandRanker();

                if (GetNumActivePlayers() > 1)
                {
                    Showdown(communityCards, ref handRanker, lastToAct);
                }

                if (GetNumActivePlayers() > 1)
                {
                    // More than one player has shown cards at showdown. Work out how to allocate the pot(s)
                    DistributeWinnings(handRanker);
                }
                else
                {
                    // all players except 1 have folded. Just give entire pot to last man standing
                    var winningPlayer = _players.First(p => p.IsActive).PlayerNum;

                    _players[winningPlayer].StackSize += _potMan.Size();
                    BroadcastAction(Stage.StageShowdown, winningPlayer, ActionType.Win, _potMan.Size());
                    _potMan.EmptyPot();
                }

                // check that money hasn't disappeared or magically appeared
                ReconcileCash();

                // Kill off broke players & check if only one player left
                KillBrokePlayers();

                BroadcastEndHand();

                if (GetNumLivePlayers() == 1)
                {
                    bDone = true;
                }
                else if (_maxHands > 0 && handNum >= _maxHands)
                {
                    bDone = true;
                }
                else
                {
                    // Move to next dealer 
                    MoveDealerAndBlinds();
                }

                if(_bPauseAfterEachHand)
                {
                    Console.WriteLine("--- Press any key to continue (ESC to exit) ---");
                    bDone = Console.ReadKey().Key == ConsoleKey.Escape;
                }
            }

            EndOfGame();

            // Write entry to summary log - !!! do this in EndofGame / new Display class?
            TextWriter logTextWriter = new StreamWriter("logs\\Summary.txt", true);

            var playerInfos = GetListOfPlayerInfos();
            foreach(var p in playerInfos)
            {
                if(p.IsAlive)
                {
                    logTextWriter.WriteLine(sConfigFile + "\t" + sOutputBase + "\t" + p.Name + "\t" + handNum);
                }
            }
            
            logTextWriter.Close();

        }

        private void LoadConfig(string sConfigFile, string sOutputBase)
        {
            var doc = XDocument.Load(sConfigFile);

            // This must happen before we write the first log message
            Logger.Initialize("logs\\" + sOutputBase + "_gamelog.txt");
            TimingLogger.Initialize("logs\\" + sOutputBase + "_calllog.csv");

            Logger.Log("--- *** CONFIG *** ---");
            Logger.Log(doc.ToString());

            var holdemConfig = doc.Element("HoldemConfig");

            if(holdemConfig == null)
            {
                throw new Exception("Unable to find HoldemConfig element in HoldemConfig.xml");
            }

            var gameRules = holdemConfig.Element("GameRules");

            if (gameRules == null)
            {
                throw new Exception("Unable to find GameRules element in HoldemConfig.xml");
            }

            // Get game rules
            var gameConfigSettings = new Dictionary<string, string>
            {
                {"smallBlind", _smallBlindSize.ToString()},
                {"bigBlind", _bigBlindSize.ToString()},
                {"startingStack", _startingStack.ToString()},
                {"maxNumRaisesPerBettingRound", _maxNumRaisesPerBettingRound.ToString()},
                {"maxHands", _maxHands.ToString()},
                {"doubleBlindFrequency", _doubleBlindFrequency.ToString()},
                {"botTimeOutMilliSeconds", _botTimeOutMilliSeconds.ToString()},
                {"randomDealer", _bRandomDealer.ToString()},
                {"randomSeating", _bRandomSeating.ToString()},
                {"pauseAfterEachHand", _bPauseAfterEachHand.ToString()},
                {"sleepAfterActionMilliSeconds", _sleepAfterActionMilliSeconds.ToString()},
                {"graphicsDisplay", _bGraphicsDisplay.ToString()}
            };

            // add defaults to dictionary

            // add or update values in dictionary from values in xml
            foreach(var attr in gameRules.Attributes())
            {
                if (gameConfigSettings.ContainsKey(attr.Name.ToString()))
                {
                    gameConfigSettings[attr.Name.ToString()] = attr.Value;
                }
                else
                {
                    gameConfigSettings.Add(attr.Name.ToString(), attr.Value);
                }
            }

            // read values from dictionary
            _smallBlindSize = Convert.ToInt32(gameConfigSettings["smallBlind"]);
            _bigBlindSize = Convert.ToInt32(gameConfigSettings["bigBlind"]);
            _startingStack = Convert.ToInt32(gameConfigSettings["startingStack"]);
            _maxNumRaisesPerBettingRound = Convert.ToInt32(gameConfigSettings["maxNumRaisesPerBettingRound"]);
            _maxHands = Convert.ToInt32(gameConfigSettings["maxHands"]);
            _doubleBlindFrequency = Convert.ToInt32(gameConfigSettings["doubleBlindFrequency"]);
            _botTimeOutMilliSeconds = Convert.ToInt32(gameConfigSettings["botTimeOutMilliSeconds"]);
            _bRandomDealer = Convert.ToBoolean(gameConfigSettings["randomDealer"]);
            _bRandomSeating = Convert.ToBoolean(gameConfigSettings["randomSeating"]);
            _bPauseAfterEachHand = Convert.ToBoolean(gameConfigSettings["pauseAfterEachHand"]);
            _sleepAfterActionMilliSeconds = Convert.ToInt32(gameConfigSettings["sleepAfterActionMilliSeconds"]);
            _bGraphicsDisplay = Convert.ToBoolean(gameConfigSettings["graphicsDisplay"]);

            // setup displays
            if(_bGraphicsDisplay)
            {
                //_displays.Add(new GraphicsDisplay());
                _eventHandlers.Add(new ConsoleDisplayHandler());
            }
            if (_sleepAfterActionMilliSeconds > 0)
            {
                _eventHandlers.Add(new SleepHandler(_sleepAfterActionMilliSeconds));
            }

            var textDisplay = new TextDisplay();
            textDisplay.SetWriteToConsole(!_bGraphicsDisplay);

            _displays.Add(textDisplay);

            var gameConfig = new GameConfig
            {
                ConfigFileName = sConfigFile,
                OutputBase = sOutputBase,
                SmallBlindSize = _smallBlindSize,
                BigBlindSize = _bigBlindSize,
                StartingStack = _startingStack,
                MaxNumRaisesPerBettingRound = _maxNumRaisesPerBettingRound,
                MaxHands = _maxHands,
                DoubleBlindFrequency = _doubleBlindFrequency,
                BotTimeOutMilliSeconds = _botTimeOutMilliSeconds,
                RandomDealer = _bRandomDealer,
                RandomSeating = _bRandomSeating
            };

            // Create players
            var xplayers = doc.Descendants("Player").ToList();
            int i;
            var numBots = xplayers.Count;

            if(numBots == 0)
            {
                throw new Exception("No Player elements found in HoldemConfig.xml");
            }

            _allBots = new List<ServerHoldemPlayer>();
            var playerConfigSettingsList = new List<Dictionary<string, string>>();

            // Create bots - work out how many player and how many observers
            var botNum = 0;
            foreach (var player in xplayers)
            {
                var playerConfigSettings = player.Attributes().ToDictionary(attr => attr.Name.ToString(), attr => attr.Value);

                // read player attributes, add to player config
                string sValue;
                bool isTrusted = false;
                if(playerConfigSettings.TryGetValue("trusted", out sValue))
                {
                    Boolean.TryParse(sValue, out isTrusted);
                }

                playerConfigSettingsList.Add(playerConfigSettings);
                var bot = new ServerHoldemPlayer(botNum,  playerConfigSettings["dll"], isTrusted);
                _allBots.Add(bot);
                botNum++;

                if(!bot.IsObserver)
                {
                    _numPlayers++;
                }
            }

            if(_numPlayers < 2 || _numPlayers > 23)
            {
                throw new Exception($"The number of live (non observer) players found is {_numPlayers}. It must be between 2 and 23");
            }
            
            // Create array to hold players (actual players not observers)
            _players = new ServerHoldemPlayer[_numPlayers];

            var rnd = new Random();
            List<int> unusedSlots = new List<int>();

            for(i=0; i< _numPlayers; i++)
            {
                unusedSlots.Add(i);
            }

            // assign id to each bot and call InitPlayer
            int nextObserverId = _numPlayers;
            int nextPlayerId = 0;

            botNum = 0;

            foreach(var bot in _allBots)
            {
                int botId;

                // work out player id
                if(bot.IsObserver)
                {
                    botId = nextObserverId;
                    nextObserverId++;
                }
                else
                {
                    if(_bRandomSeating)
                    {
                        int pos = rnd.Next(unusedSlots.Count);
                        botId = unusedSlots[pos];
                        unusedSlots.RemoveAt(pos);
                    }
                    else
                    {
                        botId = nextPlayerId;
                        nextPlayerId++;
                    }
                }

                // Need to ensure that playerId matches a players index in _players because some code is relying on this 
                bot.InitPlayer(botId, gameConfig, playerConfigSettingsList[botNum]);

                // Just call this to preload Name and write entry to timing log
                // ReSharper disable once UnusedVariable
                var sName = bot.Name; // todo: properties shouldn't do anything, so should handle whatever this is doing differently
                botNum++;
                 
                if(!bot.IsObserver)
                {
                    _players[botId] = bot;
                }
            }

            foreach(var display in _displays)
            {
                display.Initialise(gameConfig, _numPlayers, _sleepAfterActionMilliSeconds);
            }
        }

        private void InitialiseHand(int handNumber)
        {
            // Double the blinds if required. Do this here because later on we may want to include this in info to players
            if (_doubleBlindFrequency > 0 && handNumber % _doubleBlindFrequency == 0)
            {
                _smallBlindSize *= 2;
                _bigBlindSize *= 2;
            }
            
            var playerInfoList = GetListOfPlayerInfos();

            // broadcast player info to all players
            foreach (var player in _allBots)
            {
                player.InitHand(handNumber, playerInfoList.Count, playerInfoList, _dealerPlayerNum, _smallBlindSize, _bigBlindSize);
            }

            foreach(var display in _displays)
            {
                display.InitHand(handNumber, playerInfoList.Count, playerInfoList, _dealerPlayerNum, _smallBlindSize, _bigBlindSize);
            }

            // shuffle deck
            _deck.Shuffle();
        }

        private List<PlayerInfo> GetListOfPlayerInfos()
        {
            return _players.Select((shp, i) => new PlayerInfo(i, shp.Name, shp.IsAlive, shp.StackSize)).ToList();
        }

        private void EndOfGame()
        {
            var playerInfoList = GetListOfPlayerInfos();

            // broadcast player info to all players
            foreach (var player in _allBots)
            {
                player.EndOfGame(playerInfoList.Count, playerInfoList);
            }

            foreach(var display in _displays)
            {
                display.DisplayEndOfGame(playerInfoList.Count, playerInfoList);
            }

            BroadcastEndGame();
        }

        private void TakeBlinds(Dictionary<int, int> roundBets)
        {
            // take blinds - inform all players of blinds

            // small blind might not be live (if big blind was eliminated last hand)
            var smallBlindPlayer = _players[_smallBlindPlayerId];
            if (smallBlindPlayer.IsAlive)
            {
                // check player has enough chips - if not go all in
                var smallBlindBet = _smallBlindSize > smallBlindPlayer.StackSize
                                         ? smallBlindPlayer.StackSize
                                         : _smallBlindSize;

                TransferMoneyToPot(_smallBlindPlayerId, smallBlindBet);
                roundBets[_smallBlindPlayerId] += smallBlindBet;
                BroadcastAction(Stage.StagePreflop, _smallBlindPlayerId, ActionType.Blind, smallBlindBet, smallBlindBet);
            }

            // check player has enough chips - if not go all in
            var bigBlindPlayer = _players[_bigBlindPlayerNum];
            var bigBlindBet = _bigBlindSize > bigBlindPlayer.StackSize
                ? bigBlindPlayer.StackSize
                : _bigBlindSize;

            TransferMoneyToPot(_bigBlindPlayerNum, bigBlindBet);
            roundBets[_bigBlindPlayerNum] += bigBlindBet;
            BroadcastAction(Stage.StagePreflop, _bigBlindPlayerNum, ActionType.Blind, bigBlindBet, bigBlindBet);
        }

        private void DealHoleCards()
        {
            foreach (var player in _players.Where(p => p.IsActive))
            {
                var hole1 = _deck.DealCard();
                var hole2 = _deck.DealCard();

                player.ReceiveHoleCards(hole1, hole2);

                var playerId = player.PlayerNum;
                foreach (var display in _displays)
                {
                    display.DisplayHoleCards(playerId, hole1, hole2);
                }

                BroadcastHoleCards(playerId, hole1, hole2);
            }
        }

        private void BroadcastBoardCard(EBoardCardType cardType, Card boardCard)
        {
            foreach (var player in _allBots)
            {
                player.SeeBoardCard(cardType, boardCard);
            }

            foreach(IDisplay d in _displays)
            {
                d.DisplayBoardCard(cardType, boardCard);
            }
        }

        // broadcast action of a player to all players (including themselves)
        private void BroadcastAction(Stage stage, int playerId, ActionType action, int amount, int betSize = 0, int callAmount = 0)
        {
            var raiseAmount = amount - callAmount;
            var serverHoldemPlayer = _players[playerId];
            var isAllIn = serverHoldemPlayer.StackSize <= 0;

            foreach (var player in _allBots)
            {
                player.SeeAction(stage, playerId, action, amount);
            }

            foreach(var display in _displays)
            {
                display.DisplayAction(stage, playerId, action, amount, betSize, callAmount, raiseAmount, isAllIn, _potMan);
            }

            switch (action)
            {
                case ActionType.Blind:
                    BroadcastBlinds(playerId, amount);
                    break;
                case ActionType.Fold:
                case ActionType.Check:
                case ActionType.Call:
                case ActionType.Raise:
                    BroadcastPlayerAction(playerId, serverHoldemPlayer.StackSize, action, betSize);
                    break;
                case ActionType.Show:
                    break;
                case ActionType.Win:
                    break;
            }
        }

        private void BroadcastPlayerHand(int playerNum, Hand playerBestHand)
        {
            var card1 = _players[playerNum].HoleCards()[0];
            var card2 = _players[playerNum].HoleCards()[1];

            foreach (var player in _allBots)
            {
                player.SeePlayerHand(playerNum, card1, card2, playerBestHand);
            }

            foreach (IDisplay d in _displays)
            {
                d.DisplayPlayerHand(playerNum, card1, card2, playerBestHand);
            }
        }

        private bool IsBettingRoundRequired()
        {
            // Don't do betting if all players or all but one are all in.
            var numActivePlayersWithChips = _players.Count(p => p.IsActive && (p.StackSize > 0));

            return numActivePlayersWithChips > 1;
        }

        private void DoBettingRound(Stage stage, out int lastToAct)
        {
            var bDone = false;
            var raisesRemaining = _maxNumRaisesPerBettingRound < 0 ? 999 : _maxNumRaisesPerBettingRound;
            int firstBettorPlayerNum;

            // calc call /raise amounts req
            var lastFullPureRaise = _bigBlindSize;
            int callLevel;

            var roundBets = _players.ToDictionary(p => p.PlayerNum, p => 0);
            if (stage == Stage.StagePreflop)
            {
                TakeBlinds(roundBets);
                firstBettorPlayerNum = GetNextActivePlayer(_bigBlindPlayerNum);
                callLevel = _bigBlindSize; //set this explicitly in case the big blind is short
            }
            else
            {
                firstBettorPlayerNum = GetNextActivePlayer(_dealerPlayerNum);
                callLevel = _potMan.MaxContributions();
            }

            var currBettor = firstBettorPlayerNum;
            lastToAct = GetPrevActivePlayer(currBettor);

            while (!bDone)
            {
                // dont call GetAction if player is already all in
                var player = _players[currBettor];
                if (player.StackSize > 0)
                {
                    int callAmount;
                    int minRaise;
                    int maxRaise;
                    CalcRequiredBetAmounts(currBettor, callLevel, lastFullPureRaise, out callAmount, out minRaise, out maxRaise);

                    BroadcastAwaitingPlayer(player.PlayerNum);

                    // get the players action
                    ActionType playersAction;
                    int playersBetAmount;
                    int stageMaxBets =  roundBets.Max(kvp => kvp.Value);
                    player.GetAction(stage, stageMaxBets, callAmount, minRaise, maxRaise, raisesRemaining, _potMan.Size(), out playersAction, out playersBetAmount);

                    // *** DO ACTION ***
                    if (playersAction == ActionType.Fold)
                    {
                        // if fold then mark player as inactive
                        player.IsActive = false;
                    }
                    else if ((playersAction == ActionType.Call) || (playersAction == ActionType.Raise))
                    {
                        // if call or raise the take $ from players stack and put in pot
                        TransferMoneyToPot(currBettor, playersBetAmount);
                        roundBets[player.PlayerNum] += playersBetAmount;

                        if (playersAction == ActionType.Raise)
                        {
                            // if raise then update lastToAct to the preceding active player
                            lastToAct = GetPrevActivePlayer(currBettor);
                            if (_maxNumRaisesPerBettingRound > 0)
                            {
                                raisesRemaining--;
                            }

                            // if this raise is less than the minimum (because all in) then we shouldn't count it as a proper raise and shouldn't allow the original raiser to reraise
                            if (playersBetAmount - callAmount > lastFullPureRaise)
                            {
                                lastFullPureRaise = playersBetAmount - callAmount;
                            }

                            if (_potMan.PlayerContributions(currBettor) > callLevel)
                            {
                                callLevel = _potMan.PlayerContributions(currBettor);
                            }
                        }
                    }

                    BroadcastAction(stage, currBettor, playersAction, playersBetAmount, roundBets[player.PlayerNum], callAmount);
                }

                // if this player is last to act or only one active player left then bDone = true
                if ((currBettor == lastToAct) || (GetNumActivePlayers() == 1))
                {
                    bDone = true;
                }
                else
                {
                    currBettor = GetNextActivePlayer(currBettor);
                }
            }
        }

        private void CalcRequiredBetAmounts(int currBettor, int callLevel, int lastFullPureRaise, out int callAmount, out int minRaise, out int maxRaise)
        {
            int currBettorContributions = _potMan.PlayerContributions(currBettor);
            callAmount = callLevel - currBettorContributions;

            // Don't let a player raise more than any other player can call 
            maxRaise =  _players.Where(p => p.IsActive && p.PlayerNum != currBettor).Max(p => p.StackSizeAtStartOfHand) - currBettorContributions;

            if(maxRaise > _players[currBettor].StackSize)
            {
                maxRaise = _players[currBettor].StackSize;
            }

            minRaise = callAmount + lastFullPureRaise;

            if(minRaise > maxRaise)
            {
                minRaise = maxRaise;
            }
        }

        private int GetNumActivePlayers()
        {
            return _players.Count(p => p.IsActive);
        }

        private int GetNumLivePlayers()
        {
            return _players.Count(p => p.IsAlive);
        }

        private int GetNextActivePlayer(int player)
        {
            var i = 0;
            while (i < _numPlayers)
            {
                var playerNum = (i + player + 1)%_numPlayers;

                if (_players[playerNum].IsActive)
                {
                    return playerNum;
                }

                i++;
            }

            // no active players
            return -1;
        }

        private int GetNextLivePlayer(int player)
        {
            var i = 0;
            while (i < _numPlayers)
            {
                var playerNum = (i + player + 1)%_numPlayers;

                if (_players[playerNum].IsAlive)
                {
                    return playerNum;
                }

                i++;
            }

            // no live players
            return -1;
        }


        private int GetPrevActivePlayer(int player)
        {
            var i = 0;
            while (i < _numPlayers)
            {
                var playerNum = (_numPlayers + player - i - 1)%_numPlayers;

                if (_players[playerNum].IsActive)
                {
                    return playerNum;
                }

                i++;
            }

            // no active players
            return -1;
        }

        private void TransferMoneyToPot(int playerId, int amount)
        {
            var player = _players[playerId];
            if (amount > player.StackSize)
            {
                throw new Exception("insufficient chips");
            }

            if (amount > 0)
            {
                player.StackSize -= amount;

                var isAllIn = player.StackSize == 0;

                _potMan.AddPlayersBet(playerId, amount, isAllIn);

                ReconcileCash();
            }
        }


        private void ReconcileCash()
        {
            // Check money still adds up
            var totalPlayersStacks = _players.Sum(p => p.StackSize);
            var potSize = _potMan.Size();

            if (_totalMoneyInGame != potSize + totalPlayersStacks)
            {
                // ViewCash();
                throw new Exception($"money doesn't add up! Money in game = {_totalMoneyInGame}, Stacks = {totalPlayersStacks}, Pots = {potSize}");
            }
        }

        private void Showdown(Card[] board, ref HandRanker handRanker, int lastToAct)
        {
            var firstToAct = GetNextActivePlayer(lastToAct);
            var uncontestedPots = new List<int>();

            for (var potNum = 0; potNum < _potMan.Pots.Count; potNum++)
            {
                uncontestedPots.Add(potNum);
            }

            // evaluate and show hand for first player to act - flag them as winning for now
            var playerBestHand = Hand.FindPlayersBestHand(_players[firstToAct].HoleCards(), board);
            BroadcastPlayerHand(firstToAct, playerBestHand);

            handRanker.AddHand(firstToAct, playerBestHand);
            uncontestedPots = uncontestedPots.Except(_potMan.GetPotsInvolvedIn(firstToAct)).ToList();

            // Loop through other active players 
            var currPlayer = GetNextActivePlayer(firstToAct);

            do
            {
                var player = _players[currPlayer];
                ActionType playersAction;

                // if not first to act then player may fold without showing cards (unless all in)
                // Also don't allow a player to fold if they are involved in a currently uncontested (side) pot
                var potsInvolvedIn = _potMan.GetPotsInvolvedIn(currPlayer);
                if (player.StackSize > 0 && !uncontestedPots.Intersect(potsInvolvedIn).Any())
                {
                    int playersAmount;
                    player.GetAction(Stage.StageShowdown, 0, 0, 0, 0, 0, _potMan.Size(), out playersAction, out playersAmount);
                }
                else
                {
                    playersAction = ActionType.Show;
                }

                if (playersAction == ActionType.Fold)
                {
                    _players[currPlayer].IsActive = false;
                    BroadcastAction(Stage.StageShowdown, currPlayer, playersAction, 0);
                }
                else
                {
                    playerBestHand = Hand.FindPlayersBestHand(player.HoleCards(), board);
                    handRanker.AddHand(currPlayer, playerBestHand);
                    uncontestedPots = uncontestedPots.Except(potsInvolvedIn).ToList();

                    BroadcastPlayerHand(currPlayer, playerBestHand);
                }

                currPlayer = GetNextActivePlayer(currPlayer);
            } while (currPlayer != firstToAct);

            foreach (IDisplay d in _displays)
            {
                d.DisplayShowdown(handRanker, _potMan);
            }
        }

        private void DistributeWinnings(HandRanker handRanker)
        {
            foreach (var pot in _potMan.Pots)
            {
                // get all players who are involved in this pot
                var playersInvolved = pot.ListPlayersInvolved();

                // loop through hand ranks from highest to lowest
                // find highest ranked handRank that includes at least one of these players
                foreach (var hri in handRanker.HandRanks)
                {
                    var playersInRank = hri.Players;

                    var winningPlayers = playersInvolved.Intersect(playersInRank).ToList();

                    if (winningPlayers.Count > 0)
                    {
                        // split pot equally between winning players - then handle remainder, remove cash from pot, add to players stack
                        var amountWon = new Dictionary<int, int>();
                        var potRemaining = pot.Size();
                        var shareOfWinnings = potRemaining/winningPlayers.Count;

                        foreach (var i in winningPlayers)
                        {
                            amountWon[i] = shareOfWinnings;
                            potRemaining -= shareOfWinnings;
                        }

                        // if remainder left in pot then allocate 1 chip at a time starting at player in worst position (closest to small blind)
                        var currPlayer = _dealerPlayerNum;

                        while (potRemaining > 0)
                        {
                            do
                            {
                                currPlayer = GetNextActivePlayer(currPlayer);
                            } while (!winningPlayers.Contains(currPlayer));

                            amountWon[currPlayer]++;
                            potRemaining--;
                        }

                        pot.EmptyPot();

                        // broadcast win
                        foreach (var pair in amountWon)
                        {
                            _players[pair.Key].StackSize += pair.Value;
                            BroadcastAction(Stage.StageShowdown, pair.Key, ActionType.Win, pair.Value);
                        }

                        BroadcastWinnings(amountWon);

                        break;
                    }
                }
            }

            _potMan.EmptyPot();
        }

        private void KillBrokePlayers()
        {
            // Kill off broke players
            foreach (var player in _players)
            {
                if (player.IsAlive && player.StackSize <= 0)
                {
                    // Logger.Log("Player {0} has been eliminated", player.PlayerNum);

                    player.IsAlive = false;
                    player.IsActive = false;
//                    _display.UpdatePlayer(player);
                }
            }
        }

        private void MoveDealerAndBlinds()
        {
            _dealerPlayerNum = _smallBlindPlayerId; // note that this player might not be live if just eliminated
            _smallBlindPlayerId = _bigBlindPlayerNum; // note that this player might not be live if just eliminated
            _bigBlindPlayerNum = GetNextLivePlayer(_bigBlindPlayerNum);
        }

        private void BroadcastInitialisation()
        {
            var playerInfoList = GetListOfPlayerInfos();
            foreach (var handler in _eventHandlers)
            {
                handler.Initialise(playerInfoList);
            }
        }

        private void BroadcastBeginHand()
        {
            foreach (var handler in _eventHandlers)
            {
                handler.BeginHand(_dealerPlayerNum);
            }
        }

        private void BroadcastBlinds(int playerId, int amount)
        {
            foreach (var handler in _eventHandlers)
            {
                handler.TakeBlinds(playerId, amount);
            }
        }

        private void BroadcastHoleCards(int playerId, Card hole1, Card hole2)
        {
            foreach (var handler in _eventHandlers)
            {
                handler.DealHand(playerId, hole1, hole2);
            }
        }

        private void BroadcastBeginStage(Stage stage, Card[] cards)
        {
            foreach (var handler in _eventHandlers)
            {
                handler.BeginStage(stage, cards);
            }
        }

        private void BroadcastAwaitingPlayer(int playerId)
        {
            foreach (var handler in _eventHandlers)
            {
                handler.AwaitingPlayerAction(playerId);
            }
        }

        private void BroadcastPlayerAction(int playerId, int stackSize, ActionType action, int betSize)
        {
            foreach (var handler in _eventHandlers)
            {
                handler.PlayerActionPerformed(playerId, stackSize, action, betSize);
            }
        }

        private void BroadcastEndStage()
        {
            foreach (var handler in _eventHandlers)
            {
                handler.EndStage(_potMan.Pots); // todo: not sure if pots should be retrieved from class field
            }
        }

        private void BroadcastWinnings(IDictionary<int, int> playerWinnings)
        {
            foreach (var handler in _eventHandlers)
            {
                handler.DistributeWinnings(playerWinnings);
            }
        }

        private void BroadcastEndHand()
        {
            foreach (var handler in _eventHandlers)
            {
                handler.EndHand();
            }
        }

        private void BroadcastEndGame()
        {
            foreach (var handler in _eventHandlers)
            {
                handler.EndGame();
            }
        }
    }
}
