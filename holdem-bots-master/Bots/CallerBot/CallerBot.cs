
using System;
using System.IO;
using System.Collections.Generic;
using HoldemPlayerContract;

namespace CallerBot
{
    // this bots always calls. basic bot to test yours against and good starting template for building a better bot
    public class CallerBot : BaseBot
    {
        public override string Name
        {
            // return the name of your player
            get
            {
                return "CallerBot";
            }
        }

        public override void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount)
        {
            // This is the bit where you need to put the AI (mostly likely based on info you receive in other methods)

            if (stage == Stage.StageShowdown)
            {
                // if stage is the showdown then choose whether to show your hand or fold
                yourAction = ActionType.Show;
                amount = 0;
            }
            else
            {
                // stage is preflop, flop, turn or river
                // choose whether to fold, check, call or raise
                // the controller will validate your action and try to honour your action if possible but may change it (e.g. it won't let you fold if checking is possible)
                // amount only matters if you are raising (if calling the controller will use the correct amount). 
                // If raising, minRaise and maxRaise are the total amount required to put into the pot (i.e. it includes the call amount)
                // Side pots are now implemented so you can go all in and call or raise even if you have less than minimum
                yourAction = ActionType.Call;
                amount = callAmount;
            }
        }
    }
}
