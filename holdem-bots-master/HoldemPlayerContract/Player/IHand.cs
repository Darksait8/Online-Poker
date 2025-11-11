using System.Collections.Generic;

namespace HoldemPlayerContract.Player
{
    public interface IHand
    {
        Stage Stage { get; }
        IReadOnlyCollection<Card> CommunityCards { get; }

        int SmallBlind { get; }
        int BigBlind { get; }
    }
}