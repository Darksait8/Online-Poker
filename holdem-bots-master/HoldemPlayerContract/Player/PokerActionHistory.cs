namespace HoldemPlayerContract.Player
{
    public class PokerActionHistory : PokerAction
    {
        public PokerActionHistory(Stage stage, ActionType action, int amount) : base(action, amount)
        {
            Stage = stage;
        }
        
        public Stage Stage { get; }
    }
}