using AppTrust.Sdk;

namespace AppTrust.Client.Infrastructure;

public sealed class AppTrustClientOptions
{
    public const string SectionName = "AppTrustClient";

    /// <summary>PEM file (cert + private key) presented to AppTrust.Service when mTLS is required.</summary>
    public string ClientCertificatePath { get; set; } = "apptrust_client.pem";

    /// <summary>PEM file containing the client's RSA private key for JWT signing.</summary>
    public string PrivateKeyPath { get; set; } = "apptrust_client_private.pem";

    /// <summary>JWT sub/iss and mTLS CN identity for outbound calls.</summary>
    public string CallerId { get; set; } = AppConstants.ClientIdentifier;

    /// <summary>
    /// Required value for the <see cref="AppConstants.TriggerApiKeyHeaderName"/> header on POST /api/action/trigger.
    /// </summary>
    public string? TriggerApiKey { get; set; }

    /// <summary>
    /// When true, the client skips TLS server-certificate validation for AppTrust.Service.
    /// Only honored in Development; ignored in other environments.
    /// </summary>
    public bool AcceptAnyServerCertificate { get; set; }
}
