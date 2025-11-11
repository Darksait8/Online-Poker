using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HoldemPlayerContract;

namespace GMachine
{
    class FlopCalculator : StageCalculator
    {
        //public override ActionType GetAction(GameState gameState, out int amount)
        //{
        //    amount = gameState.CallAmount;
        //    return ActionType.Call;
        //}

        // This doesn't try to calc every possible combination of turn and river cards (as too many and too slow)
        // Instead it looks at turn card and then calcs two chances to get this . It doesn't consider drawing two cards to improve hand as these are unlikely and unlikely to change probs much
        public override double EstProbWinningShowdown(GameState gameState)
        {
            int numUnseen = gameState.UnseenCards.Count;
            HandAndBoard myHandAndBoard = gameState.MyHandAndBoard;

            Card[] opponentHoleCards = new Card[2];
            Dictionary<string, double> opponentHands = new Dictionary<string, double>();
            Dictionary<string, Hand> myBestHandAfterDraw = new Dictionary<string, Hand>();

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

                    // Work out who is winning at the moment (before any card is drawn)
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
                    List<Card> drawCardsToCheck = GetCardsThatCouldChangeResult(unseenList, lowHand, highHand);

                    // award wins/tie for all draw cards that don't change result
                    results[result + 1] = unseenList.Count - drawCardsToCheck.Count;

                    // for each possible card that could change the result check what the result would be
                    foreach(Card draw in drawCardsToCheck)
                    {
                        List<Card> drawBoard = new List<Card>(gameState.Board);
                        drawBoard.Add(draw);

                        Hand myBestHand;
                        string drawCardValue = draw.ValueStr();

                        if(myBestHandAfterDraw.ContainsKey(drawCardValue))
                        {
                            myBestHand = myBestHandAfterDraw[drawCardValue];
                        }
                        else
                        {
                            myBestHand = Hand.FindPlayersBestHand(gameState.MyCards.CardList, drawBoard);
                            myBestHandAfterDraw.Add(drawCardValue, myBestHand);
                        }
                        
                        Hand opponentBestHand = Hand.FindPlayersBestHand(opponentHoleCards, drawBoard);
                        results[myBestHand.Compare(opponentBestHand) + 1]++;
                    }

                    double probBeatingOpponentHand = 0;

                    // double probBeatingOpponentHand = (results[2] + results[1] / 2.0) / (results[0] + results[1] + results[2]);
                    if(result < 0)
                    {
                        // opponent currently winning. I need to hit draw card in one of next two cards
                        // or opponent needs to hit two cards that are not ones that win it for me

                        // !!! is this the correct way to handle ties?
                        double winsForOpponent = results[0] + results[1] / 2.0;
                        double cardsRemaining = unseenList.Count;

                        probBeatingOpponentHand = 1 - ((winsForOpponent/cardsRemaining) * ((winsForOpponent - 1) / (cardsRemaining - 1)));
                    }
                    else
                    {
                        // tie or I am currently winning. Opponent needs to hit draw card in one of next two cards
                        // or I need to hit two cards that are not ones that win it for opponent
                        double winsForMe = results[2] + results[1] / 2.0;
                        double cardsRemaining = unseenList.Count;

                        probBeatingOpponentHand = (winsForMe/cardsRemaining) * ((winsForMe - 1) / (cardsRemaining - 1));
                    }

                    string opponentHandString = opponentHoleCards[0].ValueStr() + opponentHoleCards[1].ValueStr();
                    opponentHands.Add(opponentHandString, probBeatingOpponentHand);
                }
            }

            // !!! This bit is common to flop/turn/river - should not be duplicated
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
    }
}
