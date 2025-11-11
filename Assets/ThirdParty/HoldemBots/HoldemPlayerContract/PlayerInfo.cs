using System;

namespace HoldemPlayerContract
{
    [Serializable]
    public class PlayerInfo
    {
        public PlayerInfo(int playerNum, string name, bool isAlive, int stackSize)
        {
            PlayerNum = playerNum;
            Name = name;
            IsAlive = isAlive;
            StackSize = stackSize;
        }

        public int PlayerNum { get; }
        public string Name { get; }
        public bool IsAlive { get; }
        public int StackSize { get; }
    }
}

