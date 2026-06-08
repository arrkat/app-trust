using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using AppTrust.Client.Infrastructure;
using AppTrust.Sdk;

namespace AppTrust.Client.Tests;

public class AsymmetricJwtProviderTests
{
    private static readonly string ValidFakePrivateKeyPem;

    static AsymmetricJwtProviderTests()
    {
        using var rsa = RSA.Create(2048);
        ValidFakePrivateKeyPem = rsa.ExportRSAPrivateKeyPem();
    }

    private static AsymmetricJwtProvider CreateProvider(IKeyLoader keyLoader)
    {
        var keyCache = new CachedRsaKeyLoader(keyLoader, NullLogger<CachedRsaKeyLoader>.Instance);
        var options = Options.Create(new AppTrustClientOptions());
        return new AsymmetricJwtProvider(keyCache, options, NullLogger<AsymmetricJwtProvider>.Instance);
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsNonEmptyJwt()
    {
        // Arrange
        var mockKeyLoader = new Mock<IKeyLoader>();
        mockKeyLoader.Setup(x => x.LoadKeyPemAsync()).ReturnsAsync(ValidFakePrivateKeyPem);
        var jwtProvider = CreateProvider(mockKeyLoader.Object);

        // Act
        var token = await jwtProvider.GetTokenAsync();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task GetTokenAsync_WhenKeyLoaderFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockKeyLoader = new Mock<IKeyLoader>();
        mockKeyLoader.Setup(x => x.LoadKeyPemAsync()).ThrowsAsync(new FileNotFoundException("Key missing."));
        var jwtProvider = CreateProvider(mockKeyLoader.Object);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => jwtProvider.GetTokenAsync());

        // Assert
        Assert.Equal("Cryptographic key load failed.", exception.Message);
    }

    [Fact]
    public async Task GetTokenAsync_AfterFailedWarmup_LoadsKeyOnDemand()
    {
        // Arrange — warmup fails once, but GetTokenAsync should retry the key load
        var mockKeyLoader = new Mock<IKeyLoader>();
        mockKeyLoader.SetupSequence(x => x.LoadKeyPemAsync())
            .ThrowsAsync(new IOException("Transient failure."))
            .ReturnsAsync(ValidFakePrivateKeyPem);

        var jwtProvider = CreateProvider(mockKeyLoader.Object);

        // Act
        await jwtProvider.WarmupAsync();
        var token = await jwtProvider.GetTokenAsync();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
        mockKeyLoader.Verify(x => x.LoadKeyPemAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task GetTokenAsync_AfterWarmup_DoesNotCallKeyLoaderAgain()
    {
        // Arrange
        var mockKeyLoader = new Mock<IKeyLoader>();
        mockKeyLoader.Setup(x => x.LoadKeyPemAsync()).ReturnsAsync(ValidFakePrivateKeyPem);
        var jwtProvider = CreateProvider(mockKeyLoader.Object);

        // Act — warmup caches the key; subsequent token requests must not reload
        await jwtProvider.WarmupAsync();
        await jwtProvider.GetTokenAsync();
        await jwtProvider.GetTokenAsync();

        // Assert
        mockKeyLoader.Verify(x => x.LoadKeyPemAsync(), Times.Once);
    }
}
