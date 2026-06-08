using Microsoft.AspNetCore.Http;

namespace AppTrust.Sdk;

public interface IInboundTrustStrategy
{
    Task<TrustResult> VerifyAsync(HttpContext context);
}
