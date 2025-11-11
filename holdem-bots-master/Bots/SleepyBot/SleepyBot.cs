using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace SleepyBot
{
    // this bot will sleep for X seconds before calling. This is used to test that the controller can timeout bots if they are taking too long
    // to respond, rather than having one bot blocking the game for an indefinite amount of time
    public class SleepyBot : BaseBot
    {
        private int _sleepMilliSeconds = 5000;

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            // This is called once at the start of the game. playerNum is your unique identifer for the game
//            System.Threading.Thread.Sleep(_sleepMilliSeconds);
            if (playerConfigSettings.ContainsKey("sleepMilliSeconds"))
            {
                _sleepMilliSeconds = Convert.ToInt32(playerConfigSettings["sleepMilliSeconds"]);
            }
        }

        public override string Name
        {
            // return the name of your player
            get
            {
//                System.Threading.Thread.Sleep(_sleepMilliSeconds);
                return "SleepyBot";
            }
        }

        public override void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int smallBlindSize, int bigBlindSize)
        {
            // this is called at the start of every hand and tells you the current status of all players (e.g. if is alive and stack size and who is dealer)
            System.Threading.Thread.Sleep(_sleepMilliSeconds);
        }

        public override void ReceiveHoleCards(Card hole1, Card hole2)
        {
            // receive your hole cards for this hand
            System.Threading.Thread.Sleep(_sleepMilliSeconds);
        }

        public override void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
        {
            // this is called to inform you when any player (including yourself) makes an action (eg puts in blinds, checks, folds, calls, raises, or wins hand)
            System.Threading.Thread.Sleep(_sleepMilliSeconds);
        }

        public override void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            // This is the bit where you need to put the AI (mostly likely based on info you receive in other methods)

            System.Threading.Thread.Sleep(_sleepMilliSeconds);

            yourAction = ActionType.Call;
            amount = callAmount;
        }

        public override void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            // this is called to inform you of the board cards (3 flop cards, turn and river)
            System.Threading.Thread.Sleep(_sleepMilliSeconds);
        }

        public override void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
            // this is called to inform you of another players hand during the show down. 
            // bestHand is the best hand that they can form with their hole cards and the five board cards
            System.Threading.Thread.Sleep(_sleepMilliSeconds);
        }

        public override void EndOfGame(int numPlayers, List<PlayerInfo> players)
        {
            System.Threading.Thread.Sleep(_sleepMilliSeconds);
        }
    }
}
