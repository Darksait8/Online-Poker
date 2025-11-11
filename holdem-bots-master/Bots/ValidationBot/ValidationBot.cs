using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using HoldemPlayerContract;

namespace ValidationBot
{
    // purpose is to monitor the game and ensure rules are enforced correctly and the controller doesn't make any errors
    // If error then this should log error and enough info to be able to reproduce the hand for debugging.
    // How to check that methods are called in correct order? each method record the next expected method?

    public class ValidationBot : BaseBot
    {
        private int _playerNum;
        private Card [] _board;
//        TextWriter _tw;

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            // This is called once at the start of the game. playerNum is your unique identifer for the game
            _playerNum = playerNum;
            _board = new Card[5];
        }

        public override string Name
        {
            // return the name of your player
            get
            {
                return "ValidationBot";
            }
        }

        public override bool IsObserver
        {
            get
            {
                return true;
            }
        }

        public override void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize)
        {
            // this is called at the start of every hand and tells you the current status of all players (e.g. if is alive and stack size and who is dealer)
            // create a writer and open the file

            // check all info matches our records, i.e.
            // check stack size are correct
            // check correct player is dealer
            // check correct players are live/dead 
        }

        public override void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
        {
            // this is called to inform you when any player (including yourself) makes an action (eg puts in blinds, checks, folds, calls, raises, or wins hand)

            // check that player is acting in turn
            // check that player has not made an illegal action

            // check that correct blinds have been made
            // check that correct player has won and been given correct amount
        }

        public override void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            // this is called to inform you of the board cards (3 flop cards, turn and river)

            // check these cards have not already been seen this hand
        }

        public override void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
            // this is called to inform you of another players hand during the show down. 
            // bestHand is the best hand that they can form with their hole cards and the five board cards

            // check player is still active
            // check bestHand is correct base on hole and board cards
            // check cards have not already been seen this game
        }

        public override void EndOfGame(int numPlayers, List<PlayerInfo> players)
        {
            // same validation as initHand
        }
    }
}
