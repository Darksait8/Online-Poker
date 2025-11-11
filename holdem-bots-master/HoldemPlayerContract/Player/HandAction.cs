namespace HoldemPlayerContract.Player
{
    public class HandAction
    {
        public HandAction(PlayerInfo player, PokerActionHistory action)
        {
            Player = player;
            Action = action;
        }

        public PlayerInfo Player { get; }
        public PokerActionHistory Action { get; }
    }
}