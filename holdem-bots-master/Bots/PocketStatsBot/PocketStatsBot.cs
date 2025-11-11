using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using HoldemPlayerContract;

namespace PocketStatsBot
{
    public class PocketStatsBot : BaseBot
    {
        private int _playerNum;
        private int _handNum = 0;
        TextWriter _tw;

        private class PocketClassStats
        {
            public string PocketClass;
            public int NumSeen;
            public int NumWinners;

            public PocketClassStats(string pPocketClass)
            {
                PocketClass = pPocketClass;
                NumSeen = 0;
                NumWinners = 0;
            }
        }

        string [] _playerHand;

        Dictionary<string, Dictionary<string, PocketClassStats>> pocketStats = new Dictionary<string, Dictionary<string, PocketClassStats>>();

        private PocketClassStats [] playerPocketCards;

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            // This is called once at the start of the game. playerNum is your unique identifer for the game
            _playerNum = playerNum;
            _playerHand = new string[2];

            _tw = new StreamWriter(".txt", false);

            string sFileName = "logs\\" + gameConfig.OutputBase + "_pocketStats.tab";

            _tw = new StreamWriter(sFileName, false);
        }

        public override string Name
        {
            // return the name of your player
            get
            {
                return "PocketStatsBot";
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
            playerPocketCards = new PocketClassStats[numPlayers];

        }


        public override void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
        {
            // this is called to inform you when any player (including yourself) makes an action (eg puts in blinds, checks, folds, calls, raises, or wins hand)
            if (action == ActionType.Win)
            {
                string firstHand, secondHand;
                bool bFirstHandWon = false;

                if (string.Compare(_playerHand[0],_playerHand[1]) < 0)
                {
                    firstHand = _playerHand[0];
                    secondHand = _playerHand[1];
                    bFirstHandWon = (playerNum == 0);
                    
                }
                else
                {
                    firstHand = _playerHand[1];
                    secondHand = _playerHand[0];
                    bFirstHandWon = (playerNum == 1);
                }

                Dictionary<string, PocketClassStats> dict;

                if(!pocketStats.ContainsKey(firstHand))
                {
                    dict = new Dictionary<string, PocketClassStats>();

                    pocketStats.Add(firstHand, dict);
                }
                else
                {
                    dict = pocketStats[firstHand];
                }

                PocketClassStats pcs;

                if(!dict.ContainsKey(secondHand))
                {
                    pcs = new PocketClassStats(secondHand);
                    dict.Add(secondHand, pcs);
                }
                else
                {
                     pcs = dict[secondHand];
                }

                pcs.NumSeen++;

                if(bFirstHandWon)
                {
                    pcs.NumWinners++;
                }
            }
        }


        private int CardToIndex(Card c)
        {
            return (int)c.Suit * 13 + (int)c.Rank;
        }

        public override void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
            // this is called to inform you of another players hand during the show down. 
            // bestHand is the best hand that they can form with their hole cards and the five board cards
            string sHandClass;

            ClassifyPocketCards(hole1, hole2, out sHandClass);

            _playerHand[playerNum] = sHandClass;
        }

        private void ClassifyPocketCards(Card card1, Card card2, out string handClass)
        {
            bool bIsPair = false;
            bool bIsSuited = false;
            ERankType highRank;
            ERankType lowRank;
            int gap;

            if (card1.Rank == card2.Rank)
            {
                bIsPair = true;
                lowRank = highRank = card1.Rank;
            }
            else if (card1.Rank > card2.Rank)
            {
                highRank = card1.Rank;
                lowRank = card2.Rank;
            }
            else
            {
                highRank = card2.Rank;
                lowRank = card1.Rank;
            }

            gap = highRank - lowRank;

            if (card1.Suit == card2.Suit)
            {
                bIsSuited = true;
            }

            handClass = Card.RankToString(highRank) + Card.RankToString(lowRank);
            if(!bIsPair)
            {
                if (bIsSuited)
                {
                    handClass += "s";
                }
                else
                {
                    handClass += "o";
                }
            }
        }


        public override void EndOfGame(int numPlayers, List<PlayerInfo> players)
        {
            _handNum++;

            foreach (KeyValuePair<string, Dictionary<string, PocketClassStats>> dict in pocketStats)
            {
                foreach (KeyValuePair<string, PocketClassStats> pair in dict.Value)
                {
                    PocketClassStats pcs = pair.Value;
                    _tw.WriteLine("{0}\t{1}\t{2}\t{3}", dict.Key, pcs.PocketClass, pcs.NumWinners, pcs.NumSeen);
                }
            }
            // close the stream
            _tw.Close();
        }
    }
}
