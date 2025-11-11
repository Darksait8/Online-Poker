using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HoldemPlayerContract;
using System.Diagnostics;

namespace GMachine
{
    class TurnCalculator : StageCalculator
    {

        //public override ActionType GetAction(GameState gameState, out int amount)
        //{
        //    amount = gameState.CallAmount;
        //    return ActionType.Call;
        //}

        public override double EstProbWinningShowdown(GameState gameState)
        {
            return EstProbWinningShowdownFastMethod(gameState);

/*
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            double bruteForceEstimate = EstProbWinningShowdownBruteForce(gameState);
            stopWatch.Stop();
            TimeSpan bruteForceTime = stopWatch.Elapsed;

            Stopwatch stopWatch2 = new Stopwatch();
            stopWatch2.Start();
            double fastEstimate = EstProbWinningShowdownFastMethod(gameState);
            stopWatch2.Stop();
            TimeSpan fastMethodTime = stopWatch2.Elapsed;

            if(Math.Abs(bruteForceEstimate - fastEstimate) > 0.01)
            {
                throw new Exception("estimates don't match");
            }

            return fastEstimate;
*/
        }

        public double EstProbWinningShowdownFastMethod(GameState gameState)
        {
            int numUnseen = gameState.UnseenCards.Count;
            HandAndBoard myHandAndBoard = gameState.MyHandAndBoard;

            Card[] opponentHoleCards = new Card[2];
            Dictionary<string, double> opponentHands = new Dictionary<string, double>();
            Dictionary<string, Hand> myBestHandForRiver = new Dictionary<string, Hand>();

            // Loop through all possible hands for opponent
            for (int i = 0; i < numUnseen; i++)
            {
                opponentHoleCards[0] = gameState.UnseenCards[i];

                for (int j = i + 1; j < numUnseen; j++)
                {
                    // Evaluate opponents hand
                    opponentHoleCards[1] = gameState.UnseenCards[j];
                    PocketCards opponentPocketCards = new PocketCards(opponentHoleCards[0], opponentHoleCards[1]);

                    HandAndBoard oppHandAndBoard = new HandAndBoard();
                    oppHandAndBoard.HoleCards = opponentPocketCards;
                    oppHandAndBoard.Board = gameState.Board;
                    oppHandAndBoard.Evaluate();

                    int[] results = new int[3];
                    int result;
                    HandAndBoard lowHand, highHand;

                    // Work out who is winning at the moment (before the river card is drawn)
                    result = myHandAndBoard.CurrentBestHand.Compare(oppHandAndBoard.CurrentBestHand);

                    if( result < 0)
                    {
                        // opponent is currently winning
                        lowHand = myHandAndBoard;
                        highHand = oppHandAndBoard;
                    }
                    else
                    {
                        // I'm currently winning (or tie)
                        lowHand = oppHandAndBoard;
                        highHand = myHandAndBoard;
                    }

                    // Get a list of cards that could change the result (ie low hand outdraws the high hand). Still need to check this list as high hand could improve as well.
                    List<Card> unseenList = gameState.UnseenCards.Where(p => p.ValueStr() != opponentHoleCards[0].ValueStr() && p.ValueStr() != opponentHoleCards[1].ValueStr()).ToList();
                    List<Card> riverCardsToCheck = GetCardsThatCouldChangeResult(unseenList, lowHand, highHand);

                    // award wins/tie for all river cards that don't change result
                    results[result + 1] = unseenList.Count - riverCardsToCheck.Count;

                    // for each possible rivercard that could change the result check what the result would be
                    foreach(Card river in riverCardsToCheck)
                    {
                        List<Card> riverBoard = new List<Card>(gameState.Board);
                        riverBoard.Add(river);

                        Hand myBestHand;
                        string riverCardValue = river.ValueStr();

                        if(myBestHandForRiver.ContainsKey(riverCardValue))
                        {
                            myBestHand = myBestHandForRiver[riverCardValue];
                        }
                        else
                        {
                            myBestHand = Hand.FindPlayersBestHand(gameState.MyCards.CardList, riverBoard);
                            myBestHandForRiver.Add(riverCardValue, myBestHand);
                        }
                        
                        Hand opponentBestHand = Hand.FindPlayersBestHand(opponentHoleCards, riverBoard);
                        results[myBestHand.Compare(opponentBestHand) + 1]++;
                    }

                    double probBeatingOpponentHand = (results[2] + results[1] / 2.0) / (results[0] + results[1] + results[2]);
                    string opponentHandString = opponentHoleCards[0].ValueStr() + opponentHoleCards[1].ValueStr();
                    opponentHands.Add(opponentHandString, probBeatingOpponentHand);
                }
            }

            // !!! This bit is common to preflop/flop/turn/river - should not be duplicated
            double estimate = 1;

            foreach(PlayerProfile p in gameState.ActiveOpponents())
            {
                double probBeatingThisOpponent = 0;

                foreach(KeyValuePair<string,double> kvp in opponentHands)
                {
                    probBeatingThisOpponent += kvp.Value * p.PointsForPossibleHands.PointsForHand(kvp.Key);
                }

                probBeatingThisOpponent /=  p.PointsForPossibleHands.TotalPoints;

                estimate *= probBeatingThisOpponent;
            }

            return estimate;
        }


        public double EstProbWinningShowdownBruteForce(GameState gameState)
        {
            // Calc possible opponent hands - !!! move this to SeeBoardCard?
            int numUnseen = gameState.UnseenCards.Count;
            double estimate = 0;

            Card[] opponentHoleCards = new Card[2];
            Dictionary<string, double> opponentHands = new Dictionary<string, double>();

            for (int i = 0; i < numUnseen; i++)
            {
                opponentHoleCards[0] = gameState.UnseenCards[i];

                for (int j = i + 1; j < numUnseen; j++)
                {
                    opponentHoleCards[1] = gameState.UnseenCards[j];

                    int[] results = new int[3];

                    // for each possible rivercard
                    for(int riverId = 0; riverId < numUnseen; riverId++)
                    {
                        if(riverId != i && riverId != j)
                        {
                            List<Card> riverBoard = new List<Card>(gameState.Board);
                            riverBoard.Add(gameState.UnseenCards[riverId]);
                        
                            Hand myBestHand = Hand.FindPlayersBestHand(gameState.MyCards.CardList, riverBoard);
                            Hand opponentBestHand = Hand.FindPlayersBestHand(opponentHoleCards, riverBoard);
                            results[myBestHand.Compare(opponentBestHand) + 1]++;
                        }
                    }

                    double probBeatingOpponentHand = (results[2] + results[1] / 2.0) / (results[0] + results[1] + results[2]);
                    string opponentHandString = opponentHoleCards[0].ValueStr() + opponentHoleCards[1].ValueStr();
                    opponentHands.Add(opponentHandString, probBeatingOpponentHand);

                    estimate += probBeatingOpponentHand;
                }
            }

            // for each active opponent
            //  for each possible hand
            //      estimate += prob they hold it * prob it beats me

            estimate = estimate / opponentHands.Count;

            int numActiveOpponents = gameState.NumActiveOpponents();

            estimate = Math.Pow(estimate, numActiveOpponents);

            return estimate;

        }

    }
}
