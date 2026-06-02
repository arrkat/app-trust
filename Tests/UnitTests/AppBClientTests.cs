using System.Net;
using System.Net.Http.Json;
using AppA.Infrastructure;
using AppA.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Infrastructure;

namespace UnitTests;

public class AppBClientTests
{
    [Fact]
    public async Task CallSecureEndpointAsync_DeserializesSessionIdFromAppBResponse()
    {
        var sessionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                message = "Secure action executed successfully!",
                caller = AppConstants.AppAIdentifier,
                sessionId
            })
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5278/") };

        var mockTokenProvider = new Mock<ITokenProvider>();
        mockTokenProvider.Setup(x => x.GetTokenAsync()).ReturnsAsync("fake-jwt");

        var client = new AppBClient(httpClient, mockTokenProvider.Object, NullLogger<AppBClient>.Instance);

        var result = await client.CallSecureEndpointAsync();

        Assert.NotNull(result);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(AppConstants.AppAIdentifier, result.Caller);
        Assert.Equal("Secure action executed successfully!", result.Message);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHttpMessageHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_response);
    }
}
