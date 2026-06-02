using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AppB.Infrastructure;
using Shared.Infrastructure;
 
namespace UnitTests;
 
public class JwtTokenValidatorTests
{
    private static readonly string ValidFakePublicKeyPem;
 
    static JwtTokenValidatorTests()
    {
        using var rsa = RSA.Create(2048);
        ValidFakePublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
    }
 
    // Token with valid (Header.Payload.Signature) structure to prevent .NET from stopping on a FormatException in the Cache test
    private const string StructurallyValidToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhcHBsaWNhdGlvbi1hIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjQ4MTYyMzkwMjJ9.X";
 
    private static JwtTokenValidator CreateValidator(IKeyLoader keyLoader)
    {
        return new JwtTokenValidator(keyLoader, NullLogger<JwtTokenValidator>.Instance);
    }
 
    [Fact]
    public async Task ValidateTokenAsync_WhenTokenIsInvalid_ReturnsInvalidResult()
    {
        // Arrange
        var mockKeyLoader = new Mock<IKeyLoader>();
        mockKeyLoader.Setup(x => x.LoadKeyPemAsync()).ReturnsAsync(ValidFakePublicKeyPem);
        var tokenValidator = CreateValidator(mockKeyLoader.Object);
 
        // Act
        var result = await tokenValidator.ValidateTokenAsync("completely-invalid-token-string");
 
        // Assert
        Assert.False(result.IsValid);
    }
 
    [Fact]
    public async Task ValidateTokenAsync_WhenKeyLoaderFails_ReturnsInvalidResult()
    {
        // Arrange
        var mockKeyLoader = new Mock<IKeyLoader>();
        mockKeyLoader.Setup(x => x.LoadKeyPemAsync()).ThrowsAsync(new FileNotFoundException("Public key missing."));
        var tokenValidator = CreateValidator(mockKeyLoader.Object);
 
        // Act
        var result = await tokenValidator.ValidateTokenAsync(StructurallyValidToken);
 
        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.CallerId);
    }
 
    [Fact]
    public async Task ValidateTokenAsync_AfterFailedWarmup_LoadsKeyOnDemand()
    {
        // Arrange
        var mockKeyLoader = new Mock<IKeyLoader>();
        mockKeyLoader.SetupSequence(x => x.LoadKeyPemAsync())
            .ThrowsAsync(new IOException("Transient disk error."))
            .ReturnsAsync(ValidFakePublicKeyPem);
        
        var tokenValidator = CreateValidator(mockKeyLoader.Object);
 
        // Act
        await tokenValidator.WarmupAsync(); 
        var result = await tokenValidator.ValidateTokenAsync(StructurallyValidToken);
 
        // Assert
        _ = result.IsValid;
        mockKeyLoader.Verify(x => x.LoadKeyPemAsync(), Times.Exactly(2));
    }
 
    [Fact]
    public async Task ValidateTokenAsync_AfterWarmup_DoesNotCallKeyLoaderAgain()
    {
        // Arrange
        var mockKeyLoader = new Mock<IKeyLoader>();
        mockKeyLoader.Setup(x => x.LoadKeyPemAsync()).ReturnsAsync(ValidFakePublicKeyPem);
        var tokenValidator = CreateValidator(mockKeyLoader.Object);
 
        // Act
        await tokenValidator.WarmupAsync();
        await tokenValidator.ValidateTokenAsync(StructurallyValidToken);
        await tokenValidator.ValidateTokenAsync(StructurallyValidToken);
 
        // Assert
        mockKeyLoader.Verify(x => x.LoadKeyPemAsync(), Times.Once);
    }
}
 