using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using HoldemPlayerContract;

// strategy ideas:
// plays based on positions and tailors actions to opponents
// target weak players?
// early in game learn about other players. take actions to learn what they will do.
// Take actions to gain info from opponents. Narrow down probable hands. Determine how they will react in certain situations
// stay alive until end of game, learn other players weakness. Then win end game
// if board card makes flush or straight possible or pairs up, bet as though you have made a hand (if others show weakness)

// Cards only matter at the showdown (if hand goes that far). The rest of the game is about
// perceived cards. How to manipulate your opponents to fold/call/raise so you get correct pot odds
// (and thus correct expected return) for your hand?

// Test cases:
// 7 callerbots
// 6 caller, 1 raiser with unlimited raises (don't get caught in re-raise loop)
// 7 allin bots (staggered)
// bots betting big - don't just fold each round
// 7 better bots
// 7 smart bots
// bots that fold most of the time-  only plays good hands
// bots that fold all game then go all in at end (when heads up)
// bots that fold in early pos, call in mid pos, raise in late pos
// vs GMachine v1
// vs me

namespace GMachine
{
    public class GMachine : BaseBot
    {
        GameState _gameState;
        TextWriter _tw;
        bool _bDoLogging = false;
        StageCalculator [] _stageCalculators;

        public override string Name
        {
            // return the name of your player
            get
            {
                return "GMachine";
            }
        }

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            string sValue;
            bool isTrusted = false;
            if(playerConfigSettings.TryGetValue("trusted", out sValue))
            {
                Boolean.TryParse(sValue, out isTrusted);
            }

            if(isTrusted)
            {
                _bDoLogging = true;
                string sFileName = "logs\\" + gameConfig.OutputBase + "_GMachine" + playerNum + ".csv";
                _tw = new StreamWriter(sFileName, false);
                _tw.WriteLine("HandNum, CurrentStage, betAmount, prAllFold, PotSize, prBestHand, estimatedfutureContributions, expectedReturn");
            }

            _gameState = new GameState();
            _gameState.InitPlayer(playerNum);

            _stageCalculators = new StageCalculator[4];

            _stageCalculators[0] = new PreFlopCalculator();
            _stageCalculators[1] = new FlopCalculator();
            _stageCalculators[2] = new TurnCalculator();
            _stageCalculators[3] = new RiverCalculator();
        }

        public override void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize)
        {
            _gameState.InitHand(handNum, numPlayers, players, dealerId, littleBlindSize, bigBlindSize);

            // !!! calculate position relative to dealer

        }

        public override void ReceiveHoleCards(Card hole1, Card hole2)
        {
            _gameState.ReceiveHoleCards(hole1, hole2);
        }

        public override void SeeAction(Stage stage, int playerId, ActionType action, int amount)
        {
            _gameState.SeeAction(stage, playerId, action, amount);
        }

        public override void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            if(stage == Stage.StageShowdown)
            {
                yourAction = ActionType.Show;
                amount = 0;
            }
            else
            {
                _gameState.CallAmount = callAmount;
                _gameState.MinRaise = minRaise;

                StageCalculator stageCalc = _stageCalculators[(int)stage];

                if(_bDoLogging)
                {
                    stageCalc.SetTextWriter(_tw);
                }

                yourAction = stageCalc.GetAction(_gameState, out amount);
            }
        }

        public override void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            _gameState.SeeBoardCard(cardType, boardCard);
        }

        public override void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
            // !!! update player profile
        }

        public override void EndOfGame(int numPlayers, List<PlayerInfo> players)
        {
            if(_bDoLogging)
            {
                // close the stream
                _tw.Close();
            }
        }

    }
}
