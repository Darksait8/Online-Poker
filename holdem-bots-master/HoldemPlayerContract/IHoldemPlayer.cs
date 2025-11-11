using System.Collections.Generic;

namespace HoldemPlayerContract
{
    public interface IHoldemPlayer
    {
        void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings);
        string Name { get; }
        bool IsObserver { get; }
        void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int smallBlindSize, int bigBlindSize);
        void ReceiveHoleCards(Card hole1, Card hole2);
        void SeeAction(Stage stage, int playerNum, ActionType action, int amount);
        void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType yourAction, out int amount);
        void SeeBoardCard(EBoardCardType cardType, Card boardCard);
        void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand);
        void EndOfGame(int numPlayers, List<PlayerInfo> players);
    }
}