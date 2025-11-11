using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WonderPokerCore;
using CorePlayer = WonderPokerCore.Player;

public sealed class DecisionProviderRouter : IPlayerDecisionProvider
{
    private readonly Dictionary<CorePlayer, IPlayerDecisionProvider> _providers = new();
    private readonly IPlayerDecisionProvider _fallbackProvider;

    public DecisionProviderRouter(IPlayerDecisionProvider fallbackProvider = null)
    {
        _fallbackProvider = fallbackProvider;
    }

    public void Register(CorePlayer player, IPlayerDecisionProvider provider)
    {
        if (player == null || provider == null)
            throw new ArgumentNullException();

        _providers[player] = provider;
    }

    public void Unregister(CorePlayer player)
    {
        if (player == null)
            return;
        _providers.Remove(player);
    }

    public Task<PlayerDecision> RequestDecisionAsync(DecisionRequest request, CancellationToken cancellationToken)
    {
        if (request == null || request.Player == null)
            throw new ArgumentNullException(nameof(request));

        if (_providers.TryGetValue(request.Player, out var provider))
        {
            return provider.RequestDecisionAsync(request, cancellationToken);
        }

        if (_fallbackProvider != null)
            return _fallbackProvider.RequestDecisionAsync(request, cancellationToken);

        return Task.FromResult(new PlayerDecision(PlayerDecisionType.Fold));
    }
}

