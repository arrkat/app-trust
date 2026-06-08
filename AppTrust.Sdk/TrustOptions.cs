namespace AppTrust.Sdk;

public sealed class TrustOptions
{
    public const string SectionName = "Trust";

    public TrustMode Mode { get; set; } = TrustMode.Jwt;
}
