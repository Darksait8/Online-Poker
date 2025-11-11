using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using HoldemPlayerContract;

// add logging of candidate actions, prFold, prWin, est pot size, pot odds, expected return
// possible opponent best hands
// prob opponent cards / hands

namespace GMachine
{
    abstract class StageCalculator
    {
        // !!! get gameState (includeing player profiles?) in constructor and record in member var?
        TextWriter _logTextWriter;

        public void SetTextWriter(TextWriter tw)
        {
            _logTextWriter = tw;
        }

        public void Log(string sMessage, params object[] arg)
        {
            if (_logTextWriter != null)
            {
                _logTextWriter.WriteLine(sMessage, arg);
            }
        }


        public virtual ActionType GetAction(GameState gameState, out int amount )
        {
            ActionType yourAction = ActionType.Fold;
            List<int> candidateBetAmounts =  new List<int>();
            int bestBetAmount = 0;
            double bestExpectedReturn = -1;


            PlayerProfile myPlayerProfile = gameState.Players[gameState.MyPlayerId];

            if(myPlayerProfile.NumActionsThisStage > 3)
            {
                // this is to try to avoid continually calling if two other players are caught in a loop re-raising each other
                amount = 0;
                return ActionType.Fold;
            }

            int callAmount = gameState.CallAmount;

            // To simplify calcs, if all opponents don't fold then assume worst case that we have to beat all of them. dont try to calc every scenario of some opponents folding, some not.
            double prBestHand = EstProbWinningShowdown(gameState);
            double prAllFold = 1; 

            candidateBetAmounts = DetermineCandidateBetAmounts(gameState, prBestHand);

            int yourContribution = myPlayerProfile.BetsThisHand;

            // !!! this way tries to maximise expected return for each bet, but doesn't consider overall game strategy. i.e. sometimes it may be better to do a non-optimal thing now to setup an optimal play in the future. should it?

            foreach (int betAmount in candidateBetAmounts)
            {
                // if at least one caller estimate the future contributions to the pot
                // if we assume all remaining active players will call then get max contribution (inc your raise) - for each active player (excluding yourself) get amount required to call (max contribution -player contribution)
                int estimatedfutureContributions = 0;

                foreach (PlayerProfile p in gameState.ActiveOpponents())
                {
                    double prFold = p.EstProbFold(gameState, betAmount);
                    prAllFold *= prFold;
                    estimatedfutureContributions += (int)((1.0 - prFold) * ((yourContribution + betAmount) - p.BetsThisHand));
                }

                double expectedReturn = CalcExpectedReturnForBetAmount(prAllFold, betAmount, gameState.PotSize, prBestHand, estimatedfutureContributions);
                Log("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", gameState.HandNum, gameState.CurrentStage, betAmount, prAllFold, gameState.PotSize, prBestHand, estimatedfutureContributions, expectedReturn);

                if (expectedReturn > bestExpectedReturn)
                {
                    bestExpectedReturn = expectedReturn;
                    bestBetAmount = betAmount;
                }
            }

            if (bestExpectedReturn < 0)
            {
                if(callAmount == 0)
                {
                    yourAction = ActionType.Check;
                    amount = 0;
                }
                else
                {
                    yourAction = ActionType.Fold;
                    amount = 0;
                }
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

            return yourAction;
        }

        protected virtual List<int> DetermineCandidateBetAmounts(GameState gameState, double prBestHand)
        {
            List<int> candidateBetAmounts =  new List<int>();
            PlayerProfile myPlayerProfile = gameState.Players[gameState.MyPlayerId];

            int callAmount = gameState.CallAmount;
            int minRaise = gameState.MinRaise;
            int maxRaise = myPlayerProfile.StackSize;

            int raiseAmount = minRaise - callAmount;

            if(callAmount > 0)
            {
                candidateBetAmounts.Add(callAmount);
            }

            // !!! work out candidate bet amount according to Kelly criterion?
            // !!! don't include candidate that are bigger than you are willing to risk at this stage of the game?
            // !!! also need to check if stacksize is less than any of these amounts
            // !!! include amount that would force short stack player to go all-in , i.e. try to knock them out?
            // !!! if you think opponent is drawing to a hand, make bet amount big enough to make their pot odds unfavourable
            // !!! could bet X time big blind, see who folds, then adjust X up or down to work out under what conditions which opponents fold

            // Need to make sure we don't get caught in a raising loop (unless we want to). For now just allow one raise per stage
            if(myPlayerProfile.NumRaisesThisStage == 0)
            {
                candidateBetAmounts.Add(minRaise);
                candidateBetAmounts.Add(callAmount + (2 * raiseAmount));
                candidateBetAmounts.Add(callAmount + (3 * raiseAmount));
                //            candidateBetAmounts.Add(maxRaise);

                if(gameState.CurrentStage > Stage.StagePreflop)
                {
                    // Use modified Kelly Criterion to calc bet amount
                    int maxBankToRisk = 100 * gameState.BigBlindSize;

                    if(maxBankToRisk > myPlayerProfile.StackSize)
                    {
                        maxBankToRisk = myPlayerProfile.StackSize;
                    }

                    int numOpponents = gameState.NumActiveOpponents();
                    double odds = numOpponents; // ???
                    double fractionOfBankToBet = (prBestHand * (odds + 1) - 1) / odds;
                    double betAmount = fractionOfBankToBet * maxBankToRisk;

                    // !!! fudge - reduce bet for early stages
                    betAmount /= (4 - (int)gameState.CurrentStage);

                    if(betAmount > minRaise)
                    {
                        candidateBetAmounts.Add((int)betAmount);
                    }
                }
            }

            return candidateBetAmounts;
        }

        // This doesn't need to calculate exact probs, just need to estimate if expected return will be positive.
        public virtual double CalcExpectedReturnForBetAmount(double prAllFold, int betAmount, int potSize, double prBestHand, double estimatedfutureContributions)
        {
            double expectedReturn = (prAllFold * potSize) + (1 - prAllFold) * ((prBestHand * (potSize + estimatedfutureContributions)) - (1 - prBestHand) * betAmount);

            return expectedReturn;
        }

        public virtual double EstProbWinningShowdown(GameState gameState)
        {
            // override this !!!
            return 1.0;
        }

        public virtual List<Card> GetCardsThatCouldChangeResult(List<Card> unseenCards, HandAndBoard lowHand, HandAndBoard highHand)
        {
            List<Card> cards = new List<Card>();
            PocketCards lowPocketCards = lowHand.HoleCards;
            Hand lowCurrBestHand = lowHand.CurrentBestHand;
            Hand highCurrBestHand = lowHand.CurrentBestHand;
            EHandType lowCurrHandType = lowCurrBestHand.HandRank();
            EHandType highCurrHandType = highCurrBestHand.HandRank();

            // if low hand has a straight draw and high hand <= straight then add cards that would give the low hand a straight
            if(lowHand.IsStraightDraw && highHand.CurrentBestHand.HandRank() <= EHandType.HandStraight )
            {
                foreach(ERankType rank in lowHand.RanksToMakeStraight)
                {
                    cards.AddRange(unseenCards.Where(p => p.Rank == rank));
                }
            }

            // if low hand has a flush draw and high hand <= flush then add cards that would give the low hand a flush
            if(lowHand.IsFlushDraw && highHand.CurrentBestHand.HandRank() <= EHandType.HandFlush )
            {
                cards.AddRange(unseenCards.Where(p => p.Suit == lowHand.SuitToMakeFlush));
            }

/*
            // This should be covered by above (!!! unless high hand > flush)
            // if low hand has a straight flush draw then add cards that would give the low hand a straight flush
            if(lowHand.IsStraightFlushDraw)
            {
                foreach(Card c in lowHand.CardsToMakeStraightFlush)
                {
                    if(unseenCards.Contains(c))
                    {
                        cards.Add(c);
                    }
                }
            }
*/        

            // Check for drawing dead
            if(highCurrHandType == EHandType.HandFours && lowCurrHandType <= EHandType.HandTwoPair)
            {
                return cards;
            }

            if(highCurrHandType == EHandType.HandFullHouse && lowCurrHandType <= EHandType.HandPair)
            {
                return cards;
            }

            if(highCurrHandType >= EHandType.HandTwoPair && lowCurrHandType == EHandType.HandRunt)
            {
                return cards;
            }

            // simple check - add any card of same rank as low hand pocket cards. 
            cards.AddRange(unseenCards.Where(p => p.Rank == lowPocketCards.LowCard.Rank));
            if(!lowPocketCards.IsPair)
            {
                cards.AddRange(unseenCards.Where(p => p.Rank == lowPocketCards.HighCard.Rank));
            }

            // Also need to consider edge cases where a card pairs with board card and helps low hand but not high hand - eg high hand has flush, river card pairs with board 
            // gives low hand (but not high hand) a full house.
            // Another edge case is where high hand has two pair (no pocket pair, but two cards paired with cards on board) and low hand has a pocket pair that is higher than highest of two pair
            // A drawn card that pairs with a board would give you a higher two pair.
            // Always checking all board cards is too slow!
            
/*
            foreach(Card c in lowHand.Board)
            { 
                cards.AddRange(unseenCards.Where(p => p.Rank == c.Rank));
            }
*/

            return cards.Distinct().ToList();
        }
    }
}
