using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AppTrust.Service.Contracts;
using AppTrust.Service.Infrastructure.Strategies;
using Microsoft.AspNetCore.Http;
using Moq;
using AppTrust.Sdk;

namespace AppTrust.Service.Tests;

public class MtlsInboundStrategyTests
{
    [Fact]
    public async Task VerifyAsync_WhenCertificateIsValid_DelegatesToValidatorAndReturnsTrustResult()
    {
        // Arrange
        using var certificate = CreateTestCertificate(AppConstants.ClientIdentifier);

        var mockValidator = new Mock<ICertificateValidator>();
        mockValidator
            .Setup(x => x.ValidateCertificateAsync(certificate))
            .ReturnsAsync(new CertificateValidationResult(true, AppConstants.ClientIdentifier));

        var strategy = new MtlsInboundStrategy(mockValidator.Object);
        var context = new DefaultHttpContext();
        context.Connection.ClientCertificate = certificate;

        // Act
        var result = await strategy.VerifyAsync(context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(AppConstants.ClientIdentifier, result.CallerId);
        mockValidator.Verify(x => x.ValidateCertificateAsync(certificate), Times.Once);
    }

    [Fact]
    public async Task VerifyAsync_WhenCertificateMissing_ReturnsInvalidWithoutCallingValidator()
    {
        // Arrange
        var mockValidator = new Mock<ICertificateValidator>();
        var strategy = new MtlsInboundStrategy(mockValidator.Object);
        var context = new DefaultHttpContext();

        // Act
        var result = await strategy.VerifyAsync(context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.CallerId);
        mockValidator.Verify(
            x => x.ValidateCertificateAsync(It.IsAny<X509Certificate2>()),
            Times.Never);
    }

    private static X509Certificate2 CreateTestCertificate(string commonName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={commonName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
    }
}
