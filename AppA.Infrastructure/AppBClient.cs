using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using AppA.Contracts;
using AppB.Contracts;

namespace AppA.Infrastructure;

public class AppBClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenProvider _tokenProvider;
    private readonly ILogger<AppBClient> _logger;

    public AppBClient(HttpClient httpClient, ITokenProvider tokenProvider, ILogger<AppBClient> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<SecureActionResult?> CallSecureEndpointAsync()
    {
        try
        {
            var token = await _tokenProvider.GetTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, "api/secure-action");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("App B rejected request with status: {StatusCode}. Details: {Details}", response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SecureActionResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network or unexpected failure while calling App B.");
            throw;
        }
    }
}
