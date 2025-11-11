using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using HoldemPlayerContract;

namespace GMachine
{
    class RiverCalculator : StageCalculator
    {
        //public override ActionType GetAction(GameState gameState, out int amount )
        //{
        //    amount = gameState.CallAmount;
        //    return ActionType.Call;
        //}

        public override double EstProbWinningShowdown(GameState gameState)
        {
            return EstProbWinningShowdownNewCalc(gameState);

/*
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            double oldCalcEstimate = EstProbWinningShowdownOldCalc(gameState);
            stopWatch.Stop();
            TimeSpan oldCalcTime = stopWatch.Elapsed;

            Stopwatch stopWatch2 = new Stopwatch();
            stopWatch2.Start();
            double newCalcEstimate = EstProbWinningShowdownNewCalc(gameState);
            stopWatch2.Stop();
            TimeSpan newCalcTime = stopWatch2.Elapsed;

            if(Math.Abs(oldCalcEstimate - newCalcEstimate) > 0.01)
            {
                throw new Exception("estimates don't match");
            }

            return newCalcEstimate;
*/
        }

        public double EstProbWinningShowdownNewCalc(GameState gameState)
        {
            int numUnseen = gameState.UnseenCards.Count;

            Dictionary<string, double> opponentHands = new Dictionary<string, double>();

            Card[] opponentHoleCards = new Card[2];
            int[] results = new int[3];

            for (int i = 0; i < numUnseen; i++)
            {
                opponentHoleCards[0] = gameState.UnseenCards[i];

                for (int j = i + 1; j < numUnseen; j++)
                {
                    opponentHoleCards[1] = gameState.UnseenCards[j];
                    Hand opponentBestHand = Hand.FindPlayersBestHand(opponentHoleCards, gameState.Board);
                    int result = gameState.MyHandAndBoard.CurrentBestHand.Compare(opponentBestHand);

                    double probBeatingOpponentHand;

                    if (result < 0)
                    {
                        probBeatingOpponentHand = 0;
                    }
                    else if (result > 0)
                    {
                        probBeatingOpponentHand = 1;
                    }
                    else
                    {
                        // ties count as half a win
                        probBeatingOpponentHand = 0.5;
                    }

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

        public double EstProbWinningShowdownOldCalc(GameState gameState)
        {
            int numUnseen = gameState.UnseenCards.Count;

            Card[] opponentHoleCards = new Card[2];
            int[] results = new int[3];
            int[] opponentHandTypeCount = new int[9];

            for (int i = 0; i < numUnseen; i++)
            {
                opponentHoleCards[0] = gameState.UnseenCards[i];

                for (int j = i + 1; j < numUnseen; j++)
                {
                    opponentHoleCards[1] = gameState.UnseenCards[j];
                    Hand opponentBestHand = Hand.FindPlayersBestHand(opponentHoleCards, gameState.Board);
                    results[gameState.MyHandAndBoard.CurrentBestHand.Compare(opponentBestHand) + 1]++;
                    opponentHandTypeCount[(int)opponentBestHand.HandRank()]++;
                }
            }

            double estimate = 1;

            // This works for 1 opponent - need to raise to power of num active opponents?
            // !!! This also assume opponent is equally likely to hold any of the possible pocket cards - needs to adjust cards based on prob opponent holds cards that can beat me
            estimate = (results[2] + results[1] / 2.0) / (results[0] + results[1] + results[2]);

            int numActiveOpponents = gameState.NumActiveOpponents();

            estimate = Math.Pow(estimate, numActiveOpponents);

            return estimate;
        }

    }
}
