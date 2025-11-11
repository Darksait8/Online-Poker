using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using HoldemPlayerContract;

namespace SmartBot
{
    class PlayerProfile
    {
        public int PlayerNum;
        public string Name;
        public bool IsAlive;
        public bool IsActive;
        public int StackSize;
        public int BetsThisHand;

        public PlayerProfile(int pPlayerNum, string pName, bool pIsAlive, int pStackSize)
        {
            PlayerNum = pPlayerNum;
            Name = pName;
            IsAlive = pIsAlive;
            IsActive = pIsAlive;
            StackSize = pStackSize;
            BetsThisHand = 0;
        }
    }


    public class SmartBot : BaseBot
    {
        private int _playerNum;
        private Card [] _holeCards;
        private List<Card> _board;

        private List<Card> _cards = new List<Card>();

        private int _handNum = 0;
        private int _numActivePlayers = 0;
        private int _pocketRank = 0;
        private PlayerProfile [] _players;
        private Hand _myBestHand;
        // private HashSet<Card> _unseenCards;
        private List<Card> _unseenList;

        private Dictionary<string, int> pocketRanks = new Dictionary<string, int>();

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            // This is called once at the start of the game. playerNum is your unique identifer for the game
            _playerNum = playerNum;
            _holeCards = new Card[2];
            _board = new List<Card>();
//            _unseenCards = new HashSet<Card>();
            _unseenList = new List<Card>();

            pocketRanks.Add("AA", 1);
            pocketRanks.Add("KK", 2);
            pocketRanks.Add("QQ", 3);
            pocketRanks.Add("AKs", 4);
            pocketRanks.Add("JJ", 5);
            pocketRanks.Add("AQs", 6);
            pocketRanks.Add("KQs", 7);
            pocketRanks.Add("AJs", 8);
            pocketRanks.Add("KJs", 9);
            pocketRanks.Add("TT", 10);
            pocketRanks.Add("AKo", 11);
            pocketRanks.Add("ATs", 12);
            pocketRanks.Add("QJs", 13);
            pocketRanks.Add("KTs", 14);
            pocketRanks.Add("QTs", 15);
            pocketRanks.Add("JTs", 16);
            pocketRanks.Add("99", 17);
            pocketRanks.Add("AQo", 18);
            pocketRanks.Add("A9s", 19);
            pocketRanks.Add("KQo", 20);
            pocketRanks.Add("88", 21);
            pocketRanks.Add("K9s", 22);
            pocketRanks.Add("T9s", 23);
            pocketRanks.Add("A8s", 24);
            pocketRanks.Add("Q9s", 25);
            pocketRanks.Add("J9s", 26);
            pocketRanks.Add("AJo", 27);
            pocketRanks.Add("A5s", 28);
            pocketRanks.Add("77", 29);
            pocketRanks.Add("A7s", 30);
            pocketRanks.Add("KJo", 31);
            pocketRanks.Add("A4s", 32);
            pocketRanks.Add("A3s", 33);
            pocketRanks.Add("A6s", 34);
            pocketRanks.Add("QJo", 35);
            pocketRanks.Add("66", 36);
            pocketRanks.Add("K8s", 37);
            pocketRanks.Add("T8s", 38);
            pocketRanks.Add("A2s", 39);
            pocketRanks.Add("98s", 40);
            pocketRanks.Add("J8s", 41);
            pocketRanks.Add("ATo", 42);
            pocketRanks.Add("Q8s", 43);
            pocketRanks.Add("K7s", 44);
            pocketRanks.Add("KTo", 45);
            pocketRanks.Add("55", 46);
            pocketRanks.Add("JTo", 47);
            pocketRanks.Add("87s", 48);
            pocketRanks.Add("QTo", 49);
            pocketRanks.Add("44", 50);
            pocketRanks.Add("22", 51);
            pocketRanks.Add("33", 52);
            pocketRanks.Add("K6s", 53);
            pocketRanks.Add("97s", 54);
            pocketRanks.Add("K5s", 55);
            pocketRanks.Add("76s", 56);
            pocketRanks.Add("T7s", 57);
            pocketRanks.Add("K4s", 58);
            pocketRanks.Add("K2s", 59);
            pocketRanks.Add("K3s", 60);
            pocketRanks.Add("Q7s", 61);
            pocketRanks.Add("86s", 62);
            pocketRanks.Add("65s", 63);
            pocketRanks.Add("J7s", 64);
            pocketRanks.Add("54s", 65);
            pocketRanks.Add("Q6s", 66);
            pocketRanks.Add("75s", 67);
            pocketRanks.Add("96s", 68);
            pocketRanks.Add("Q5s", 69);
            pocketRanks.Add("64s", 70);
            pocketRanks.Add("Q4s", 71);
            pocketRanks.Add("Q3s", 72);
            pocketRanks.Add("T9o", 73);
            pocketRanks.Add("T6s", 74);
            pocketRanks.Add("Q2s", 75);
            pocketRanks.Add("A9o", 76);
            pocketRanks.Add("53s", 77);
            pocketRanks.Add("85s", 78);
            pocketRanks.Add("J6s", 79);
            pocketRanks.Add("J9o", 80);
            pocketRanks.Add("K9o", 81);
            pocketRanks.Add("J5s", 82);
            pocketRanks.Add("Q9o", 83);
            pocketRanks.Add("43s", 84);
            pocketRanks.Add("74s", 85);
            pocketRanks.Add("J4s", 86);
            pocketRanks.Add("J3s", 87);
            pocketRanks.Add("95s", 88);
            pocketRanks.Add("J2s", 89);
            pocketRanks.Add("63s", 90);
            pocketRanks.Add("A8o", 91);
            pocketRanks.Add("52s", 92);
            pocketRanks.Add("T5s", 93);
            pocketRanks.Add("84s", 94);
            pocketRanks.Add("T4s", 95);
            pocketRanks.Add("T3s", 96);
            pocketRanks.Add("42s", 97);
            pocketRanks.Add("T2s", 98);
            pocketRanks.Add("98o", 99);
            pocketRanks.Add("T8o", 100);
            pocketRanks.Add("A5o", 101);
            pocketRanks.Add("A7o", 102);
            pocketRanks.Add("73s", 103);
            pocketRanks.Add("A4o", 104);
            pocketRanks.Add("32s", 105);
            pocketRanks.Add("94s", 106);
            pocketRanks.Add("93s", 107);
            pocketRanks.Add("J8o", 108);
            pocketRanks.Add("A3o", 109);
            pocketRanks.Add("62s", 110);
            pocketRanks.Add("92s", 111);
            pocketRanks.Add("K8o", 112);
            pocketRanks.Add("A6o", 113);
            pocketRanks.Add("87o", 114);
            pocketRanks.Add("Q8o", 115);
            pocketRanks.Add("83s", 116);
            pocketRanks.Add("A2o", 117);
            pocketRanks.Add("82s", 118);
            pocketRanks.Add("97o", 119);
            pocketRanks.Add("72s", 120);
            pocketRanks.Add("76o", 121);
            pocketRanks.Add("K7o", 122);
            pocketRanks.Add("65o", 123);
            pocketRanks.Add("T7o", 124);
            pocketRanks.Add("K6o", 125);
            pocketRanks.Add("86o", 126);
            pocketRanks.Add("54o", 127);
            pocketRanks.Add("K5o", 128);
            pocketRanks.Add("J7o", 129);
            pocketRanks.Add("75o", 130);
            pocketRanks.Add("Q7o", 131);
            pocketRanks.Add("K4o", 132);
            pocketRanks.Add("K3o", 133);
            pocketRanks.Add("96o", 134);
            pocketRanks.Add("K2o", 135);
            pocketRanks.Add("64o", 136);
            pocketRanks.Add("Q6o", 137);
            pocketRanks.Add("53o", 138);
            pocketRanks.Add("85o", 139);
            pocketRanks.Add("T6o", 140);
            pocketRanks.Add("Q5o", 141);
            pocketRanks.Add("43o", 142);
            pocketRanks.Add("Q4o", 143);
            pocketRanks.Add("Q3o", 144);
            pocketRanks.Add("74o", 145);
            pocketRanks.Add("Q2o", 146);
            pocketRanks.Add("J6o", 147);
            pocketRanks.Add("63o", 148);
            pocketRanks.Add("J5o", 149);
            pocketRanks.Add("95o", 150);
            pocketRanks.Add("52o", 151);
            pocketRanks.Add("J4o", 152);
            pocketRanks.Add("J3o", 153);
            pocketRanks.Add("42o", 154);
            pocketRanks.Add("J2o", 155);
            pocketRanks.Add("84o", 156);
            pocketRanks.Add("T5o", 157);
            pocketRanks.Add("T4o", 158);
            pocketRanks.Add("32o", 159);
            pocketRanks.Add("T3o", 160);
            pocketRanks.Add("73o", 161);
            pocketRanks.Add("T2o", 162);
            pocketRanks.Add("62o", 163);
            pocketRanks.Add("94o", 164);
            pocketRanks.Add("93o", 165);
            pocketRanks.Add("92o", 166);
            pocketRanks.Add("83o", 167);
            pocketRanks.Add("82o", 168);
            pocketRanks.Add("72o", 169);
        }

        public override string Name
        {
            // return the name of your player
            get
            {
                return "SmartBot";
            }
        }

        public override void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize)
        {
            // this is called at the start of every hand and tells you the current status of all players (e.g. if is alive and stack size and who is dealer)
            // create a writer and open the file
            _handNum++;
            _cards.Clear();
            _board.Clear();

            // initialise unseen cards with entire decks
            //_unseenCards.Clear();
            _unseenList.Clear();

            int i;

            for (i = 0; i < 52; i++)
            {
                var rank = (ERankType)(i % 13);
                var suit = (ESuitType)(i / 13);

                var card = new Card(rank, suit);

                //_unseenCards.Add(card);
                _unseenList.Add(card);
            }

            // create or update player profiles
            if (_handNum == 1)
            {
                _players = new PlayerProfile[numPlayers];

                foreach (PlayerInfo p in players)
                {
                    _players[p.PlayerNum] = new PlayerProfile(p.PlayerNum, p.Name, p.IsAlive, p.StackSize);
                }
            }
            else
            {
                foreach (PlayerInfo p in players)
                {
                    _players[p.PlayerNum].IsAlive = p.IsAlive;
                    _players[p.PlayerNum].IsActive = p.IsAlive; // if alive then player is active at start of hand
                    _players[p.PlayerNum].BetsThisHand = 0;

                    if (_players[p.PlayerNum].StackSize != p.StackSize)
                    {
                        // we should be tracking this correctly
                        throw new Exception("stacksize doesn't match");
                    }
                }
            }

            _numActivePlayers = players.Count(p => (p.IsAlive == true));

        }

        public override void ReceiveHoleCards(Card hole1, Card hole2)
        {
            // receive your hole cards for this hand
            _holeCards[0] = hole1;
            _holeCards[1] = hole2;

            //_unseenCards.Remove(hole1);
            //_unseenCards.Remove(hole2);
            _unseenList.Remove(hole1);
            _unseenList.Remove(hole2);

            _cards.Add(hole1);
            _cards.Add(hole2);

            string sHandClass;
            ClassifyPocketCards(hole1, hole2, out sHandClass);

            _pocketRank = pocketRanks[sHandClass];
        }

        public override void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
        {
            // this is called to inform you when any player (including yourself) makes an action (eg puts in blinds, checks, folds, calls, raises, or wins hand)

            // !!! need to move this stuff into a separate base / utility class to track state etc. ???
            if (action == ActionType.Fold)
            {
                _players[playerNum].IsActive = false;
                _numActivePlayers--;
            }
            else if (action == ActionType.Blind || action == ActionType.Call || action == ActionType.Raise)
            {
                _players[playerNum].StackSize -= amount;
                _players[playerNum].BetsThisHand += amount;
                // !!! also increment our tracking of potsize
            }
            else if (action == ActionType.Win)
            {
                _players[playerNum].StackSize += amount;
                // !!! also decrement our potsize
            }
        }

        public override void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            amount = 0;
            yourAction = ActionType.Fold;

            if (stage == Stage.StageShowdown)
            {
                GetShowdownAction(callAmount, minRaise, maxRaise, raisesRemaining, potSize, out yourAction, out amount);
            }
            else
            {
                List<int> candidateBetAmounts =  new List<int>();
                int bestBetAmount = 0;
                double bestExpectedReturn = -1;

                // need to check if stacksize is less than any of these amounts
                int raiseAmount = minRaise - callAmount;

                candidateBetAmounts.Add(callAmount);
                candidateBetAmounts.Add(minRaise);
                candidateBetAmounts.Add(callAmount + (2 * raiseAmount));
                candidateBetAmounts.Add(callAmount + (3 * raiseAmount));
//                candidateBetAmounts.Add(maxRaise);

                // !!! just calc once for now,  although this could change depending on betAmount if you can get a better hand than yours to fold
                double prBestHand = EstProbBestHand(stage);
                int yourContribution = _players[_playerNum].BetsThisHand;

                foreach (int betAmount in candidateBetAmounts)
                {
					// !!! for each candidate bet amount estimate probabilty of all others folding, and if not - estimate the number of callers.
                    double prAllFold = 0;

                    if (betAmount > callAmount)
                    {
                        prAllFold = EstProbAllFold(stage, betAmount);
                    }

                    // if at least one caller estimate the future contributions to the pot
                    // if we assume all remaining active players will call then get max contribution (inc your raise) - for each active player (excluding yourself) get amount required to call (max contribution -player contribution)
                    int estimatedfutureContributions = 0;
                    foreach (PlayerProfile p in _players)
                    {
                        if (p.IsActive && (p.PlayerNum != _playerNum))
                        {
                            estimatedfutureContributions += (yourContribution + betAmount) - p.BetsThisHand;
                        }
                    }

                    // !!! should this be divide by betAmount?
                    double expectedReturn = (prAllFold * potSize)
                        + (1 - prAllFold) * ((prBestHand * (potSize + estimatedfutureContributions)) - (1 - prBestHand) * betAmount);

                    if (expectedReturn > bestExpectedReturn)
                    {
                        bestExpectedReturn = expectedReturn;
                        bestBetAmount = betAmount;
                    }
                }

                if (bestExpectedReturn < 0)
                {
                    yourAction = ActionType.Fold;
                    amount = 0;
                }
                else if (bestBetAmount == callAmount)
                {
                    yourAction = ActionType.Call;
                    amount = callAmount;
                }
                else
                {
                    yourAction = ActionType.Raise;
                    amount = bestBetAmount;
                }
            }
        }

        private double EstProbAllFold(Stage stage, int betAmount)
        {
            // estimate the probability of all remaining opponents folding (and therefore you win without showdown occurring)
            return 0.0;
        }
        private double EstProbBestHand(Stage stage)
        {
            double estimate = 0;

            // estimate the probability of you having the best hand at showdown
            switch (stage)
            {
                case Stage.StagePreflop:
                    estimate = EstProbBestHandPreFlop();
                    break;
                case Stage.StageFlop:
                    estimate = EstProbBestHandFlop();
                    break;
                case Stage.StageTurn:
                    estimate = EstProbBestHandTurn();
                    break;
                case Stage.StageRiver:
                    estimate = EstProbBestHandRiver();
                    break;
            }
            return estimate;
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
            if (bIsSuited)
            {
                handClass += "s";
            }
            else if(!bIsPair)
            {
                handClass += "o";
            }
        }


        private double EstProbBestHandPreFlop()
        {
            // based on your 2 hole cards, opponents profiles and betting this hand - estimate the probability you will have the best hand at showdown (assuming all currently active opponents are active at showdown)
            int numOpponents = _numActivePlayers  - 1;

            // !!! improve this
            double bestHandBeating1 = 0.84; // prob best hand (pocket Aces) beating 1 opponent
            double bestHandBeating9 = 0.31; // prob best hand (pocket Aces) beating 9 opponents

            double worstHandBeating1 = 0.35; // prob worst hand (7-2 off suit) beating 1 opponent
            double worstHandBeating9 = 0.04; // prob worst hand (7-2 off suit) beating 9 opponents

            // do some interpolation to come up with a rough estimate of probability of my hand beating X number of opponents (assuming that they are playing all hands - still need to adjust if they only player good hands)
            double bestHandWinning = bestHandBeating1 - numOpponents * (bestHandBeating1 - bestHandBeating9) / 8.0;
            double worstHandWinning = worstHandBeating1 - numOpponents * (worstHandBeating1 - worstHandBeating9) / 8.0;

            double estimate = bestHandWinning - (_pocketRank - 1) * (bestHandWinning - worstHandWinning) / 168.0;

            return estimate;
        }

        private double EstProbBestHandFlop()
        {
            return EstProbBestHandPostFlop();
        }

        private double EstProbBestHandPostFlop()
        { 
            double estimate = 1;
            foreach(PlayerProfile p in _players.Where(p => p.IsActive))
            {
                estimate *= CalcProbBeatingOpponent(p.PlayerNum);
            }
            return estimate;
        }

        private double EstProbBestHandTurn()
        {
            // based on your 2 hole cards and 4 board cards, opponents profiles and betting this hand - estimate the probability you will have the best hand at showdown (assuming all currently active opponents are active at showdown)
            return EstProbBestHandPostFlop();
        }
        private double EstProbBestHandRiver()
        {
            // based on your 2 hole cards and 5 board cards, opponents profiles and betting this hand - estimate the probability you will have the best hand at showdown (assuming all currently active opponents are active at showdown)
            return EstProbBestHandPostFlop();
        }

        private void GetShowdownAction(int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            // !!! muck hand if a better hand is already shown (and not advertising)
            yourAction = ActionType.Show;
            amount = 0;
        }

        private double CalcProbBeatingOpponent(int playerNum)
        {
            // calculate probability of having best hand at the moment
            // look at all possible opponent hands. which ones beat mine, tie or lose to my hand?
            int numUnseen = _unseenList.Count;

            Card[] opponentHoleCards = new Card[2];
            int[] results = new int[3];
            int[] opponentHandTypeCount = new int[9];

            for (int i = 0; i < numUnseen; i++)
            {
                opponentHoleCards[0] = _unseenList[i];

                for (int j = i + 1; j < numUnseen; j++)
                {
                    opponentHoleCards[1] = _unseenList[j];
                    Hand opponentBestHand = Hand.FindPlayersBestHand(opponentHoleCards, _board);
                    results[_myBestHand.Compare(opponentBestHand) + 1]++;
                    opponentHandTypeCount[(int)opponentBestHand.HandRank()]++;
                }
            }

            return (results[2] + results[1] / 2.0) / (results[0] + results[1] + results[2]);
        }

        public override void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            // this is called to inform you of the board cards (3 flop cards, turn and river)
            _board.Add(boardCard);

            // _unseenCards.Remove(boardCard);
            _unseenList.Remove(boardCard);
            _cards.Add(boardCard);

            // if you are not alive then you don't have a hand
            if (_players[_playerNum].IsAlive && (cardType == EBoardCardType.BoardFlop3 ||
                cardType == EBoardCardType.BoardTurn ||
                cardType == EBoardCardType.BoardRiver))
            {
                _myBestHand = Hand.FindPlayersBestHand(_cards);

/*
                // Convert set of unseen cards to a list so that I can random access it
                List<Card> unseenList = new List<Card>();

                foreach (Card c in _unseenCards)
                {
                    unseenList.Add(c);
                }
*/

                if (cardType == EBoardCardType.BoardFlop3)
                {
                    Card[] possibleBoard = new Card[5];
                    int[] handTypeCount = new int[9];
                    int[] results = new int[3];

                    possibleBoard[0] = _board[0];
                    possibleBoard[1] = _board[1];
                    possibleBoard[2] = _board[2];

                    int numUnseen = _unseenList.Count;

                    // look at all possibilities for turn and river - work out my best hands
                    for (int i=0; i<numUnseen; i++)
                    {
                        possibleBoard[3] = _unseenList[i];

                        for (int j=i+1; j<numUnseen; j++)
                        {
                            possibleBoard[4] = _unseenList[j];
                            Hand bestHand = Hand.FindPlayersBestHand(_holeCards, possibleBoard);
                            handTypeCount[(int)bestHand.HandRank()]++;

                            // for each possible board, look at all possible hands for an opponent. Work out which hands beat yours, tie or lose to your hand
/*
                            for(int k=0; k<numUnseen;k++)
                            {
                                if(k!=i && k!=j)
                                {
                                    Card[] opponentHoleCards = new Card[2];

                                    opponentHoleCards[0] = unseenList[k];

                                    for(int l=k+1; l<numUnseen; l++)
                                    {
                                        if(l!=i && l!=j)
                                        {
                                            opponentHoleCards[1] = unseenList[l];
                                            Hand opponentBestHand = Hand.FindPlayersBestHand(opponentHoleCards, possibleBoard);
                                            results[bestHand.Compare(opponentBestHand) + 1]++;
                                        }
                                    }
                                }
                            }
*/
                        }
                    }
                }
                else if (cardType == EBoardCardType.BoardTurn)
                {
                    Card[] possibleBoard = new Card[5];
                    int[] handTypeCount = new int[9];

                    possibleBoard[0] = _board[0];
                    possibleBoard[1] = _board[1];
                    possibleBoard[2] = _board[2];
                    possibleBoard[3] = _board[3];

                    int numUnseen = _unseenList.Count;

                    // look at all possibilities for river - work out my best hands
                    for (int i = 0; i < numUnseen; i++)
                    {
                        possibleBoard[4] = _unseenList[i];
                        Hand bestHand = Hand.FindPlayersBestHand(_holeCards, possibleBoard);
                        handTypeCount[(int)bestHand.HandRank()]++;
                    }
                }
            }
        }
    }
}
