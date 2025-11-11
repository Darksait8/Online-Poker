using System;

namespace HoldemPlayerContract
{
    [Serializable]
    public class GameConfig
    {
        public string ConfigFileName  {get; set; }
        public string OutputBase  {get; set; }
        public int SmallBlindSize  {get; set; }
        public int BigBlindSize  {get; set; }
        public int StartingStack  {get; set; }
        public int MaxNumRaisesPerBettingRound  {get; set; }
        public int MaxHands  {get; set; }
        public int DoubleBlindFrequency  {get; set; }
        public int BotTimeOutMilliSeconds  {get; set; }
        public bool RandomDealer  {get; set; }
        public bool RandomSeating  {get; set; }
    }
}
