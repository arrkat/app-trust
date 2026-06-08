using Microsoft.AspNetCore.Http;

namespace AppTrust.Sdk;

/// <summary>
/// Composes multiple inbound strategies for <see cref="TrustMode.Both"/>.
/// Every strategy must pass; the first failure short-circuits.
/// All successful strategies must agree on <see cref="TrustResult.CallerId"/>.
/// </summary>
public class InboundTrustStrategyHandler : IInboundTrustStrategy
{
    private readonly IReadOnlyList<IInboundTrustStrategy> _strategies;

    public InboundTrustStrategyHandler(IEnumerable<IInboundTrustStrategy> strategies)
    {
        _strategies = strategies.ToList();
        if (_strategies.Count == 0)
            throw new ArgumentException("At least one strategy is required.", nameof(strategies));
    }

    public async Task<TrustResult> VerifyAsync(HttpContext context)
    {
        TrustResult? firstValid = null;

        foreach (var strategy in _strategies)
        {
            var result = await strategy.VerifyAsync(context);
            if (!result.IsValid)
                return new TrustResult(false, null);

            if (firstValid is null)
                firstValid = result;
            else if (!string.Equals(firstValid.CallerId, result.CallerId, StringComparison.Ordinal))
                return new TrustResult(false, null);
        }

        return firstValid ?? new TrustResult(false, null);
    }
}
