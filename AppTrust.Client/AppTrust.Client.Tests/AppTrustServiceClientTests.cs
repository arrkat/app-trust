using System.Net;
using System.Net.Http.Json;
using AppTrust.Client.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AppTrust.Sdk;

namespace AppTrust.Client.Tests;

public class AppTrustServiceClientTests
{
    [Fact]
    public async Task CallSecureEndpointAsync_DeserializesCorrelationIdFromAppTrustServiceResponse()
    {
        // Arrange
        var correlationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                message = "Secure action executed successfully!",
                caller = AppConstants.ClientIdentifier,
                correlationId
            })
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5278/") };

        var mockTrustStrategy = new Mock<IOutboundTrustStrategy>();
        mockTrustStrategy
            .Setup(x => x.ApplyAsync(It.IsAny<HttpRequestMessage>()))
            .Returns(Task.CompletedTask);

        var client = new AppTrustServiceClient(httpClient, mockTrustStrategy.Object, NullLogger<AppTrustServiceClient>.Instance);

        // Act
        var result = await client.CallSecureEndpointAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(correlationId, result.CorrelationId);
        Assert.Equal(AppConstants.ClientIdentifier, result.Caller);
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
