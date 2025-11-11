using System;
using System.IO;
using System.Collections.Generic;
using HoldemPlayerContract;

namespace ObserverBot
{
    // this bot is not an active player, but simply watches the game,  records stats and writes them to a log file -  playerinfo.txt
    public class ObserverBot : BaseBot
    {
        private class PlayerStats
        {
            public int PlayerNum;
            public int NumHandsPlayed;
            public int NumPreFlopsFolded;
            public int NumFlopsFolded;
            public int NumTurnsFolded;
            public int NumRiversFolded;
            public int NumShowdowns;
            public int NumHandsWon;
            public bool bWonThisHand;

            public PlayerStats(int pPlayerNum)
            {
                PlayerNum = pPlayerNum;
                NumHandsPlayed = 0;
                NumPreFlopsFolded = 0;
                NumFlopsFolded = 0;
                NumTurnsFolded = 0;
                NumRiversFolded = 0;
                NumShowdowns = 0;
                NumHandsWon = 0;
                bWonThisHand = false;
            }
        }

        private int _playerNum;
        private Card [] _board;
        private int _handNum = 0;
        TextWriter _tw;
        private PlayerStats [] playerStats;

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            // This is called once at the start of the game. playerNum is your unique identifer for the game
            _playerNum = playerNum;
            _board = new Card[5];

            string sFileName = "logs\\" + gameConfig.OutputBase + "_playerinfo.tab";

            _tw = new StreamWriter(sFileName, false);
        }

        public override string Name
        {
            // return the name of your player
            get
            {
                return "ObserverBot";
            }
        }

        public override bool IsObserver
        {
            get
            {
                return true;
            }
        }

        public override void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int smallBlindSize, int bigBlindSize)
        {
            // this is called at the start of every hand and tells you the current status of all players (e.g. if is alive and stack size and who is dealer)
            // create a writer and open the file
            _handNum++;

            if(_handNum == 1)
            {
                playerStats = new PlayerStats[numPlayers];

                string sHeader = "HandNum\t";

                foreach (PlayerInfo p in players)
                {
                    playerStats[p.PlayerNum] = new PlayerStats(p.PlayerNum);
                    sHeader += p.PlayerNum + "-" + p.Name + "\t";
                }

                // write a line of text to the file
                _tw.WriteLine(sHeader);
            }

            string sStackSizes = _handNum + "\t";

            foreach (PlayerInfo p in players)
            {
                sStackSizes += p.StackSize + "\t";

                if (p.IsAlive)
                {
                    playerStats[p.PlayerNum].NumHandsPlayed++;
                    playerStats[p.PlayerNum].bWonThisHand = false;
                }
            }

            // write a line of text to the file
            _tw.WriteLine(sStackSizes);
        }

        public override void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
        {
            // this is called to inform you when any player (including yourself) makes an action (eg puts in blinds, checks, folds, calls, raises, or wins hand)
            if (action == ActionType.Fold)
            {
                switch (stage)
                {
                    case Stage.StagePreflop:
                        playerStats[playerNum].NumPreFlopsFolded++;
                        break;
                    case Stage.StageFlop:
                        playerStats[playerNum].NumFlopsFolded++;
                        break;
                    case Stage.StageTurn:
                        playerStats[playerNum].NumTurnsFolded++;
                        break;
                    case Stage.StageRiver:
                        playerStats[playerNum].NumRiversFolded++;
                        break;
                }
            }
            else if (action == ActionType.Win)
            {
                // be careful not to double count here. This can be call multiple times for the same player for the same hand if they win more than one side pot
                if(!playerStats[playerNum].bWonThisHand)
                {
                    playerStats[playerNum].NumHandsWon++;
                    playerStats[playerNum].bWonThisHand = true;
                }
            }

        }

        public override void EndOfGame(int numPlayers, List<PlayerInfo> players)
        {
            _handNum++;

            string sStackSizes = _handNum + "\t";

            foreach (PlayerInfo p in players)
            {
                sStackSizes += p.StackSize + "\t";
            }

            // write a line of text to the file
            _tw.WriteLine(sStackSizes);

            _tw.WriteLine("");

            _tw.WriteLine("PlayerNum\tNumHandsPlayed\tNumPreFlopsFolded\tNumFlopsFolded\tNumTurnsFolded\tNumRiversFolded\tNumShowdowns\tNumHandsWon");

            foreach (PlayerStats p in playerStats)
            {
                p.NumShowdowns = p.NumHandsPlayed - (p.NumPreFlopsFolded + p.NumFlopsFolded + p.NumTurnsFolded + p.NumRiversFolded);

                _tw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                        p.PlayerNum,
                        p.NumHandsPlayed,
                        p.NumPreFlopsFolded,
                        p.NumFlopsFolded,
                        p.NumTurnsFolded,
                        p.NumRiversFolded,
                        p.NumShowdowns,
                        p.NumHandsWon
                    );
            }

            // close the stream
            _tw.Close();
        }
    }
}
