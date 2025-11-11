
using System;

namespace HoldemPlayerContract
{
    [Serializable]
    public class PlayerInfo
    {
        public PlayerInfo(int pPlayerNum, string pName, bool pIsAlive, int pStackSize)
        {
            PlayerNum = pPlayerNum;
            Name = pName;
            IsAlive = pIsAlive;
            StackSize = pStackSize;
        }

        public int PlayerNum { get; }
        public string Name { get; }
        public bool IsAlive { get; }
        public int StackSize { get; }
    }
}