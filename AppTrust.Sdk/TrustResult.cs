using System.Diagnostics.CodeAnalysis;

namespace AppTrust.Sdk;

public record TrustResult
{
    [MemberNotNullWhen(true, nameof(CallerId))]
    public bool IsValid { get; }

    public string? CallerId { get; }

    public TrustResult(bool isValid, string? callerId)
    {
        if (isValid && string.IsNullOrWhiteSpace(callerId))
            throw new ArgumentException("CallerId is required when trust is valid.", nameof(callerId));

        IsValid = isValid;
        CallerId = isValid ? callerId : null;
    }
}
