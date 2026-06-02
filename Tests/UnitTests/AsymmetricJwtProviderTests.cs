using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AppA.Infrastructure;
using Shared.Infrastructure;
 
namespace UnitTests;
 
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
        return new AsymmetricJwtProvider(keyLoader, NullLogger<AsymmetricJwtProvider>.Instance);
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
 
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => jwtProvider.GetTokenAsync());
        Assert.Equal("Cryptographic subsystem initialization failed.", exception.Message);
    }
 
    [Fact]
    public async Task GetTokenAsync_AfterFailedWarmup_LoadsKeyOnDemand()
    {
        // Arrange
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
 
        // Act
        await jwtProvider.WarmupAsync();
        await jwtProvider.GetTokenAsync();
        await jwtProvider.GetTokenAsync();
 
        // Assert
        mockKeyLoader.Verify(x => x.LoadKeyPemAsync(), Times.Once);
    }
}