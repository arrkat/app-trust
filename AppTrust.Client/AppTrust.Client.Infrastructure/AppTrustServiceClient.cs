using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using AppTrust.Service.Contracts;
using AppTrust.Sdk;

namespace AppTrust.Client.Infrastructure;

public class AppTrustServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IOutboundTrustStrategy _trustStrategy;
    private readonly ILogger<AppTrustServiceClient> _logger;

    public AppTrustServiceClient(HttpClient httpClient, IOutboundTrustStrategy trustStrategy, ILogger<AppTrustServiceClient> logger)
    {
        _httpClient = httpClient;
        _trustStrategy = trustStrategy;
        _logger = logger;
    }

    public async Task<SecureActionResult> CallSecureEndpointAsync()
    {
        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/secure-action");
            await _trustStrategy.ApplyAsync(request);
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network or unexpected failure while calling AppTrust.Service.");
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("AppTrust.Service rejected request with status: {StatusCode}. Details: {Details}", response.StatusCode, error);
            throw new HttpRequestException(
                $"AppTrust.Service returned {(int)response.StatusCode} {response.StatusCode}: {error}",
                inner: null,
                statusCode: response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<SecureActionResult>()
            ?? throw new InvalidOperationException("AppTrust.Service returned an empty response body.");
    }
}
