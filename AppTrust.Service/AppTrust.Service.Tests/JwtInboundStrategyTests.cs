using AppTrust.Service.Contracts;
using AppTrust.Service.Infrastructure.Strategies;
using Microsoft.AspNetCore.Http;
using Moq;
using AppTrust.Sdk;

namespace AppTrust.Service.Tests;

public class JwtInboundStrategyTests
{
    [Fact]
    public async Task VerifyAsync_WhenTokenIsValid_DelegatesToValidatorAndReturnsTrustResult()
    {
        // Arrange
        var mockValidator = new Mock<ITokenValidator>();
        mockValidator
            .Setup(x => x.ValidateTokenAsync("valid-token", It.IsAny<string?>()))
            .ReturnsAsync(new TokenValidationResult(true, AppConstants.ClientIdentifier));

        var strategy = new JwtInboundStrategy(mockValidator.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer valid-token";

        // Act
        var result = await strategy.VerifyAsync(context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(AppConstants.ClientIdentifier, result.CallerId);
        mockValidator.Verify(x => x.ValidateTokenAsync("valid-token", null), Times.Once);
    }

    [Fact]
    public async Task VerifyAsync_WhenBearerSchemeIsLowerCase_StillAcceptsToken()
    {
        // Arrange — RFC 7235 scheme matching is case-insensitive
        var mockValidator = new Mock<ITokenValidator>();
        mockValidator
            .Setup(x => x.ValidateTokenAsync("valid-token", It.IsAny<string?>()))
            .ReturnsAsync(new TokenValidationResult(true, AppConstants.ClientIdentifier));

        var strategy = new JwtInboundStrategy(mockValidator.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "bearer valid-token";

        // Act
        var result = await strategy.VerifyAsync(context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(AppConstants.ClientIdentifier, result.CallerId);
    }

    [Fact]
    public async Task VerifyAsync_WhenAuthorizationHeaderMissing_ReturnsInvalidWithoutCallingValidator()
    {
        // Arrange
        var mockValidator = new Mock<ITokenValidator>();
        var strategy = new JwtInboundStrategy(mockValidator.Object);
        var context = new DefaultHttpContext();

        // Act
        var result = await strategy.VerifyAsync(context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.CallerId);
        mockValidator.Verify(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
}
