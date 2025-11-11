using System;
using System.Collections.Generic;

namespace HoldemPlayerContract.Player
{
    public abstract class PokerPlayer : MarshalByRefObject, IHoldemPlayer
    {
        private HandState _hand;
        private PlayerHandState _playerPosition;

        protected PokerPlayer(string name)
        {
            Name = name;
        }
        
        public void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            PlayerId = playerNum;
        }

        public string Name { get; }
        public bool IsObserver => false;

        protected IHand CurrentHand => _hand;
        protected IPlayerHandState PlayerPosition => _playerPosition;
        protected int PlayerId { get; private set; }

        protected abstract PokerAction PerformAction(int betSize, int callAmount, int minRaise, int potSize);
        
        public void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int smallBlindSize, int bigBlind)
        {
            _hand = new HandState(players, smallBlindSize, bigBlind);
            _playerPosition = _hand.GetPlayer(PlayerId);
        }

        public void ReceiveHoleCards(Card hole1, Card hole2)
        {
            _playerPosition.SetCards(hole1, hole2);
        }

        public void SeeAction(Stage stage, int playerId, ActionType action, int amount)
        {
            _hand.SetAction(stage, playerId, action, amount);
        }

        public void GetAction(Stage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out ActionType action, out int amount)
        {
            // todo: not sure if any of this will be good info to pass through. perhaps callAmount, minRaise, maxRaise, raisesRemaing, potSize?
            // potsize shoudl be in the handState
            // perhaps most of this should be in the handstate?
            // again we shoudl validate the stage?
            var result = PerformAction(betSize, callAmount, minRaise, potSize);

            amount = result.Amount;
            action = result.Action;
        }

        public void SeeBoardCard(EBoardCardType cardType, Card communityCard)
        {
            _hand.SetCommunityCard(communityCard, cardType);
        }

        public virtual void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
        }

        public void EndOfGame(int numPlayers, List<PlayerInfo> players)
        {
        }
    }
}