using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace GMachine
{
    // stuff to do:
    // store history of actions
    // num times raised
    // num times folded after raise (or showed bad hand)- to est num times bluffing
    // num times called
    // num times check-raised

    // track possible pocket cards (1225 possibilities pre flop) and prob they hold them
    // to estimate hand strength
    // record prob as points (eg 0 - max) instead of dealing with float. eg every possiblity starts with 1000 points. adjust to 0 if impossible, shift points from hands to other hands (should always add up to max)

    // classify pocket cards into categories , eg big pairs, suited high card low gap etc.
    // depending on opponent action and what they show, generalise actions to all other hands in same category.
    //est probs for each category as starting probs for the hand. distribute probs across all hands in the category. adjust as hand progresses
    // or have prob curve for pairs, suited and unsuited?

    // work out if opponent is aggressive, loose-passive, tight etc.

    // record decision points for each player.
    // What decision was made? What can be inferred from each decision
    // Fold - bad cards / pot odds not enough to draw
    // Call - Drawing hand or sand bagging
    // Raise - made hand or bluffing. If 1st raise - last board card may have made their hand. What is it?

    // record when opponent decision doesn't match expected behaviour (according to profile). Then adjust profile
    // Start with default profile for average player - eg mostly plays only good hands, occassionally tries to bluff etc.

    // this class should be base class that just records known facts about players 
    // create derived classes to infer things about opponents. different derived classes could use different algorithms to infer playertype and possible hands


    class PossibleHands
    {
        public int [,] HandPoints;
        public int TotalPoints;
        public PossibleHands()
        {
            Clear();
        }

        public void Clear()
        {
            TotalPoints = 0;
            HandPoints = new int [52,52];

            for(int i=0; i<52; i++)
            {
                for(int j=0; j<52; j++)
                {
                    if(i > j)
                    {
                        HandPoints[i,j] = 1000;
                        TotalPoints += 1000;
                    }
                    else
                    {
                        HandPoints[i,j] = 0;
                    }
                }
            }
        }

        public int PointsForHand(string HandDesc)
        {
            // !!! fix this
            return 1000;
        }


        public void RemoveHandsContaining(Card c)
        {
            int cardIndex = CardToIndex(c);

            for(int i=0; i<52; i++)
            {
                if(i>cardIndex)
                {
                    TotalPoints -= HandPoints[i,cardIndex];
                    HandPoints[i,cardIndex] = 0;
                }
                else
                {
                    TotalPoints -= HandPoints[cardIndex,i];
                    HandPoints[cardIndex,i] = 0;
                }
            }
        }

        public void IncrementHandsContainingRank(ERankType rank, int incr)
        {
            IncrementHandsContainingCardIndex(CardToIndex(new Card(rank,ESuitType.SuitClubs)), incr);
            IncrementHandsContainingCardIndex(CardToIndex(new Card(rank,ESuitType.SuitHearts)), incr);
            IncrementHandsContainingCardIndex(CardToIndex(new Card(rank,ESuitType.SuitSpades)), incr);
            IncrementHandsContainingCardIndex(CardToIndex(new Card(rank,ESuitType.SuitDiamonds)), incr);
        }

        public void IncrementHandsContainingCardIndex(int cardIndex, int incr)
        {
            int x,y;
            for(int i=0; i<52; i++)
            {
                if(i>cardIndex)
                {
                    x = i;
                    y = cardIndex;

                }
                else
                {
                    x = cardIndex;
                    y = i;
                }

                if(HandPoints[x,y] > 0)
                {
                    TotalPoints += incr;
                    HandPoints[x,y] += incr;
                }
            }
        }

        private int CardToIndex(Card c)
        {
            return (int)c.Suit * 13 + (int)c.Rank;
        }
    }

    class PlayerProfile
    {
        public int PlayerNum;
        public string Name;
        public bool IsAlive;
        public bool IsActive;
        public int StackSize;
        public int BetsThisHand;
        public PossibleHands PointsForPossibleHands;
        public int NumRaisesThisHand;
        public int NumRaisesThisStage;
        public int NumActionsThisStage;

        public PlayerProfile(int pPlayerNum, string pName, bool pIsAlive, int pStackSize)
        {
            PlayerNum = pPlayerNum;
            Name = pName;
            IsAlive = pIsAlive;
            IsActive = pIsAlive;
            StackSize = pStackSize;
            BetsThisHand = 0;
            PointsForPossibleHands = new PossibleHands();
        }

        public void InitHand(bool pIsAlive, int pStackSize)
        {
            IsAlive = pIsAlive;
            IsActive = pIsAlive;  // if alive then player is active at start of hand
            StackSize = pStackSize; // these should already be identical, but update our stack size with the source of truth in case we have missed something
            BetsThisHand = 0;
            NumActionsThisStage = 0;
            NumRaisesThisStage = 0;
            NumRaisesThisHand = 0;
            PointsForPossibleHands.Clear();
        }

        public void InitStage()
        {
            NumActionsThisStage = 0;
            NumRaisesThisStage = 0;
        }

        public void SeeAction(GameState gameState, Stage stage, ActionType action, int amount)
        {
            NumActionsThisStage++;

            if(action == ActionType.Raise)
            {
                NumRaisesThisHand++;
                NumRaisesThisStage++;
            }

/*
            if(NumRaisesThisHand == 1)
            {
                if(stage == Stage.StagePreflop)
                {
                    // assume opponent has good pocket cards
                }
                else if(stage == Stage.StageFlop)
                {
                    // assume one of flop cards hit opponent
                    PointsForPossibleHands.IncrementHandsContainingRank(gameState.Board[0].Rank, 1000);
                    PointsForPossibleHands.IncrementHandsContainingRank(gameState.Board[1].Rank, 1000);
                    PointsForPossibleHands.IncrementHandsContainingRank(gameState.Board[2].Rank, 1000);
                }
                else if(stage == Stage.StageTurn)
                {
                    // assume turn card hit opponent
                    PointsForPossibleHands.IncrementHandsContainingRank(gameState.Board[3].Rank, 1000);
                }
                else if(stage == Stage.StageRiver)
                {
                    // assume river card hit opponent
                    PointsForPossibleHands.IncrementHandsContainingRank(gameState.Board[4].Rank, 1000);
                }

            }
*/
        }

        // give this game state and if I make this bet, what is the probability that this opponent will fold?
        public virtual double EstProbFold(GameState gameState, int betAmount)
        {
            // !!! do stuff here
            // estimate this based on:
            // amount required to call
            // estimated hand strength
            // size of pot and pot odds / expected return for them
            // num opponents
            // their estimate of my and opponents hands
            // actions so far this hand
            // their history / player type - loose / tight, do they often fold?
            return 0.0;
        }
    }
}
