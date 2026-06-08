namespace AppTrust.Sdk;

public static class AppConstants
{
    public const string ClientIdentifier = "apptrust-client";
    public const string ServiceIdentifier = "apptrust-service";

    /// <summary>JWT claim binding the token to the mTLS client certificate (SHA-256 thumbprint).</summary>
    public const string CertThumbprintClaimType = "x5t#S256";

    public const string TriggerApiKeyHeaderName = "X-Trigger-Key";
}
