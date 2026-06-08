using AppTrust.Client.Contracts;
using AppTrust.Client.Infrastructure.Strategies;
using Moq;

namespace AppTrust.Client.Tests;

public class JwtOutboundStrategyTests
{
    [Fact]
    public async Task ApplyAsync_SetsBearerAuthorizationHeader()
    {
        // Arrange
        const string expectedToken = "signed-jwt-token";

        var mockTokenProvider = new Mock<ITokenProvider>();
        mockTokenProvider
            .Setup(x => x.GetTokenAsync(It.IsAny<string?>()))
            .ReturnsAsync(expectedToken);

        var strategy = new JwtOutboundStrategy(mockTokenProvider.Object, bindCertificateThumbprint: false);
        var request = new HttpRequestMessage(HttpMethod.Post, "api/secure-action");

        // Act
        await strategy.ApplyAsync(request);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal(expectedToken, request.Headers.Authorization.Parameter);
        mockTokenProvider.Verify(x => x.GetTokenAsync(null), Times.Once);
    }
}
