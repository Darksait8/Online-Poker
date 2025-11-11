using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace GMachine
{
    internal class StageCalculatorFactory
    {
        public StageCalculator CreateStageCalculator(Stage stage)
        {
            switch(stage)
            {
                case Stage.StagePreflop:
                    return new PreFlopCalculator();
                case Stage.StageFlop:
                    return new FlopCalculator();
                case Stage.StageTurn:
                    return new TurnCalculator();
                case Stage.StageRiver:
                    return new RiverCalculator();
                default:
                    throw new Exception("Unrecognised stage");

            }
        }
    }
}
