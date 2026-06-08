using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppTrust.Sdk;

namespace AppTrust.Client.Infrastructure;

public sealed class TriggerApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppTrustClientOptions _options;
    private readonly ILogger<TriggerApiKeyMiddleware> _logger;

    public TriggerApiKeyMiddleware(
        RequestDelegate next,
        IOptions<AppTrustClientOptions> options,
        ILogger<TriggerApiKeyMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsTriggerPath(context.Request))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.TriggerApiKey))
        {
            _logger.LogCritical("AppTrustClient:TriggerApiKey is not configured; rejecting trigger request.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        if (!context.Request.Headers.TryGetValue(AppConstants.TriggerApiKeyHeaderName, out var providedKey)
            || !string.Equals(providedKey.ToString(), _options.TriggerApiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Unauthorized trigger attempt: missing or invalid API key.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private static bool IsTriggerPath(HttpRequest request) =>
        request.Path.StartsWithSegments("/api/action/trigger", StringComparison.OrdinalIgnoreCase)
        && HttpMethods.IsPost(request.Method);
}
