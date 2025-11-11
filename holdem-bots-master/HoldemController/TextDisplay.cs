using System.Collections.Generic;
using System.Linq;
using HoldemController.Logging;
using HoldemPlayerContract;

namespace HoldemController
{
    internal class TextDisplay : IDisplay
    {
        int _numPlayers;

        public void SetWriteToConsole(bool writeToConsole)
        {
            Logger.SetWriteToConsole(writeToConsole);
        }

        public void SetLogFileName(string fileName)
        {
            Logger.Initialize(fileName);
        }

        public void Initialise(GameConfig gameConfig, int numPlayers, int sleepAfterActionMilliSeconds)
        {
            _numPlayers = numPlayers;
        }

        public void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize)
        {
            Logger.Log("");
            Logger.Log("---------*** HAND {0} ***----------", handNum);
            Logger.Log("Num\tName\tIsAlive\tStackSize\tIsDealer");

            foreach(PlayerInfo p in players)
            {
                Logger.Log("{0}\t{1}\t{2}\t{3}\t{4}", p.PlayerNum, p.Name, p.IsAlive, p.StackSize, p.PlayerNum == dealerId);
            }
        }

        public void DisplayHoleCards(int playerId, Card hole1, Card hole2)
        {
            Logger.Log("Player {0} hole cards {1} {2}", playerId, hole1.ValueStr(), hole2.ValueStr());
        }

        public void DisplayAction(Stage stage, int playerId, ActionType action, int totalAmount, int betSize, int callAmount, int raiseAmount, bool isAllIn, PotManager potMan)
        {
            string sLogMsg = "";

            sLogMsg += string.Format("Player {0} {1} {2}", playerId, action, totalAmount);

            if (action == ActionType.Raise)
            {
                sLogMsg += string.Format(" ({0} call + {1} raise)", callAmount, raiseAmount);
            }

            if (isAllIn)
            {
                sLogMsg += " *ALL IN*";
            }

            Logger.Log(sLogMsg);
        }

        public void DisplayBoardCard(EBoardCardType cardType, Card boardCard)
        {
            Logger.Log("{0} {1}", cardType, boardCard.ValueStr());
        }

        public void DisplayPlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
            Logger.Log("Player {0} Best Hand =  {1} {2}", playerNum, bestHand.HandValueStr(), bestHand.HandRankStr());
        }

        public void DisplayShowdown(HandRanker handRanker, PotManager potMan)
        {
            int i;

            // show pots
            Logger.Log("");

            string sLogMsg = "Pots     \t";

            for(i=0; i<potMan.Pots.Count(); i++)
            {
                sLogMsg += i + "\t";
            }

            sLogMsg += "Total";
            Logger.Log(sLogMsg);

            // !!! only show live players?
            for(i=0; i<_numPlayers; i++)
            {
                sLogMsg = "Player " + i + "\t";

                // !!! show stack size for player here?

                foreach (var p in potMan.Pots)
                {
                    int value = 0;

                    if(p.PlayerContributions.ContainsKey(i))
                    {
                        value = p.PlayerContributions[i];
                    }

                    sLogMsg += value + "\t";
                }

                sLogMsg += potMan.PlayerContributions(i);
                Logger.Log(sLogMsg);
            }

            sLogMsg = "Total   ";

            foreach (var p in potMan.Pots)
            {
                sLogMsg += "\t" + p.Size();
            }

            Logger.Log(sLogMsg);

            // Show hand ranks
            Logger.Log("");
            Logger.Log("--- Hand Ranks ---");

            foreach (var hri in handRanker.HandRanks)
            {
                var hand = hri.Hand;

                sLogMsg = hand.HandRankStr() + " ";

                for (i = 0; i < hand.NumSubRanks(); i++)
                {
                    sLogMsg += hand.SubRank(i) + " ";
                }

                sLogMsg += "Players (" + string.Join(",", hri.Players) + ")";
                Logger.Log(sLogMsg);
            }

            Logger.Log("");
        }

        public void DisplayEndOfGame(int numPlayers, List<PlayerInfo> players)
        {
            Logger.Log("");
            Logger.Log("---------*** GAME OVER ***----------");
            Logger.Log("Num\tName\tIsAlive\tStackSize");

            foreach(PlayerInfo p in players)
            {
                Logger.Log("{0}\t{1}\t{2}\t{3}", p.PlayerNum, p.Name, p.IsAlive, p.StackSize);
            }

            Logger.Log("---------------");
        }
    }
}
