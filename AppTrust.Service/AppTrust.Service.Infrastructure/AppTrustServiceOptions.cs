using AppTrust.Sdk;

namespace AppTrust.Service.Infrastructure;

public sealed class AppTrustServiceOptions
{
    public const string SectionName = "AppTrustService";
    public const string KestrelConfiguredExternallyKey = "AppTrustService:KestrelConfiguredExternally";

    /// <summary>PEM file (cert + private key) for HTTPS when trust mode requires mTLS.</summary>
    public string? ServerCertificatePath { get; set; }

    /// <summary>PEM file containing the client's RSA public key for JWT verification.</summary>
    public string PublicKeyPath { get; set; } = "apptrust_client_public.pem";

    /// <summary>Expected JWT issuer/sub for inbound callers.</summary>
    public string ExpectedJwtCallerId { get; set; } = AppConstants.ClientIdentifier;

    /// <summary>Allowed subject CNs for inbound mTLS client certificates.</summary>
    public string[] AllowedClientCertificateSubjects { get; set; } = [AppConstants.ClientIdentifier];
}
