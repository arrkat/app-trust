using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace AppTrust.Client.Infrastructure;

public static class AppTrustServiceHttpClientHandlerFactory
{
    public static HttpMessageHandler Create(
        X509Certificate2? clientCertificate,
        bool acceptAnyServerCertificate)
    {
        var handler = new SocketsHttpHandler();
        handler.SslOptions.ApplicationProtocols = [SslApplicationProtocol.Http11];
        if (clientCertificate is not null)
            handler.SslOptions.ClientCertificates = new X509CertificateCollection { clientCertificate };

        if (acceptAnyServerCertificate)
        {
            handler.SslOptions.RemoteCertificateValidationCallback =
                static (_, _, _, _) => true;
        }

        return handler;
    }
}
