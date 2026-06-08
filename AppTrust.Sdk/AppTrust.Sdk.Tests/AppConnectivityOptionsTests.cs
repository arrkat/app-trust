using AppTrust.Sdk;

namespace AppTrust.Sdk.Tests;

public class AppConnectivityOptionsTests
{
    [Theory]
    [InlineData(TrustMode.Jwt)]
    [InlineData(TrustMode.Mtls)]
    [InlineData(TrustMode.Both)]
    public void ValidateAppTrustServiceBaseUrlForTrustMode_WhenHttp_Throws(TrustMode mode)
    {
        // Arrange & Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            AppConnectivityOptions.ValidateAppTrustServiceBaseUrlForTrustMode("http://localhost:5278/", mode));

        // Assert
        Assert.Contains("https", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TrustMode.Jwt)]
    [InlineData(TrustMode.Mtls)]
    [InlineData(TrustMode.Both)]
    public void ValidateAppTrustServiceBaseUrlForTrustMode_WhenHttps_DoesNotThrow(TrustMode mode)
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() =>
            AppConnectivityOptions.ValidateAppTrustServiceBaseUrlForTrustMode("https://localhost:7214/", mode));

        Assert.Null(exception);
    }
}
