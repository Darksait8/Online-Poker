using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace GMachine
{
    internal class GameState
    {

        public int MyPlayerId {get; set; }

        private int _handNum;
        private Stage _currentStage;
        private int _dealerId;
        private int _smallBlindSize;
        private int _bigBlindSize;
        private int _potSize;

        public int HandNum {get {return _handNum; } }
        public Stage CurrentStage {get {return _currentStage; } }
        public int DealerId {get {return _dealerId; } }
        public int SmallBlindSize {get {return _smallBlindSize; } }
        public int BigBlindSize {get {return _bigBlindSize; } }
        public int PotSize {get {return _potSize; } }
        public int CallAmount {get; set; }
        public int MinRaise {get; set; }

        // !!! how to make these readonly?
        public PocketCards MyCards {get; set; }
        public List<Card> Board;
        public List<Card> UnseenCards;
        public PlayerProfile [] Players;
        public HandAndBoard MyHandAndBoard {get; set; }

        public void InitPlayer(int playerId)
        {
            MyPlayerId = playerId;
            Board = new List<Card>();
            UnseenCards = new List<Card>();
        }

        public void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize)
        {
            _handNum = handNum;
            _dealerId = dealerId;
            // !!! Calc my position relative to the dealer? base strategy on position?

            _smallBlindSize = littleBlindSize;
            _bigBlindSize = bigBlindSize;
            _potSize = 0;

            UnseenCards.Clear();
            Board.Clear();

            // initialise unseen cards with entire decks
            int i;

            for (i = 0; i < 52; i++)
            {
                var rank = (ERankType)(i % 13);
                var suit = (ESuitType)(i / 13);
                var card = new Card(rank, suit);
                UnseenCards.Add(card);
            }

            // create or update player profiles - !!! create should happen in InitPlayer but we don't currently have this info
            if (handNum == 1)
            {
                Players = new PlayerProfile[numPlayers];

                foreach (PlayerInfo p in players)
                {
                    Players[p.PlayerNum] = new PlayerProfile(p.PlayerNum, p.Name, p.IsAlive, p.StackSize);
                }
            }
            else
            {
                foreach (PlayerInfo p in players)
                {
                    Players[p.PlayerNum].InitHand(p.IsAlive, p.StackSize);
                }
            }

            InitStage(Stage.StagePreflop);
        }

        public void ReceiveHoleCards(Card hole1, Card hole2)
        {
            MyCards = new PocketCards(hole1, hole2);

            // !!! could do calcs here based on hole cards and position???

            // update unseen card list
            UnseenCards.Remove(hole1);
            UnseenCards.Remove(hole2);

            foreach(PlayerProfile p  in ActiveOpponents())
            {
                p.PointsForPossibleHands.RemoveHandsContaining(hole1);
                p.PointsForPossibleHands.RemoveHandsContaining(hole2);
            }
        }

        public void SeeAction(Stage stage, int playerNum, ActionType action, int amount)
        {
            _currentStage = stage;

            PlayerProfile player = Players[playerNum];

            player.SeeAction(this, stage, action, amount);
            // !!! update player profile
            // !!! update probable hands for opponent - estimate players hand strength based on size of their bet?

            // update opponent stacksize and potsize
            if (action == ActionType.Fold)
            {
                player.IsActive = false;
            }
            else if (action == ActionType.Check)
            {
                // update playerProfile stats
            }
            else if (action == ActionType.Blind || action == ActionType.Call || action == ActionType.Raise)
            {
                player.StackSize -= amount;
                player.BetsThisHand += amount;
                _potSize += amount;
            }
            else if (action == ActionType.Win)
            {
                player.StackSize += amount;
                _potSize -= amount;
            }
        }

        private void InitStage(Stage stage)
        {
            _currentStage = stage;

            foreach(PlayerProfile p in Players)
            {
                p.InitStage();
            }
        }

        public void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            // update unseen card list
            UnseenCards.Remove(boardCard);
            Board.Add(boardCard);

            foreach(PlayerProfile p  in ActiveOpponents())
            {
                p.PointsForPossibleHands.RemoveHandsContaining(boardCard);
            }

            if(cardType == EBoardCardType.BoardFlop3)
            {
                InitStage(Stage.StageFlop);
            }
            else if(cardType == EBoardCardType.BoardTurn)
            {
                InitStage(Stage.StageTurn);
            }
            else if(cardType == EBoardCardType.BoardRiver)
            {
                InitStage(Stage.StageRiver);
            }

            // !!! eliminate possibilities for opponent hands

            // if you are not alive then you don't have a hand
            if (Players[MyPlayerId].IsAlive && 
                   (cardType == EBoardCardType.BoardFlop3 ||
                    cardType == EBoardCardType.BoardTurn ||
                    cardType == EBoardCardType.BoardRiver)
                )
            {
                //// !!! this probably should be in the PocketCards class
                //List<Card> myCards = new List<Card>();
                //myCards.Add(MyCards.HighCard);
                //myCards.Add(MyCards.LowCard);

                // determine your best hand
//                MyBestHand = Hand.FindPlayersBestHand(MyCards.CardList, Board);

                // !!! move this bit to SeeBoardCard?
                MyHandAndBoard = new HandAndBoard();
                MyHandAndBoard.HoleCards = MyCards;
                MyHandAndBoard.Board = Board;
                MyHandAndBoard.Evaluate();


                // !!! determine nuts. Don't bet if your hand is far from nuts (unless bluff)?
                // !!! determine possible best hands for opponents (also include straight draws and flush draws)
                // for each possible hand ranks (that beats mine) - highest to lowest
                //      work out which pocket cards could make these (exclude cards already used on higher ranked hands)

                // for each opponent
                //      what is prob opponent hold them?

                // How to factor in draws to calc prob of winning showdown?
                // How to factor in hands of equal rank to mine?
                // Need to work through examples

                // !!! work out cards I am concerned opponent holds.
                // !!! work out good and bad cards for turn /river (cards that help me or cards that hurt me)
                // !!! calc your prob of winning showdown
            }
        }

        public int NumActivePlayers()
        {
            return Players.Count(p => p.IsActive);
        }
        public int NumActiveOpponents()
        {
            return Players.Count(p => p.IsActive && p.PlayerNum != MyPlayerId);
        }

        public List<PlayerProfile> ActiveOpponents()
        {
            return Players.Where(p => p.IsActive && p.PlayerNum != MyPlayerId).ToList();
        }

        // !!! add NumActiveOpponents, NumLiveOpponents
    }
}
