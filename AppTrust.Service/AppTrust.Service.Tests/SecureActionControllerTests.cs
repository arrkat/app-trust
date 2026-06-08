using AppTrust.Service.API.Controllers;
using AppTrust.Service.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AppTrust.Sdk;

namespace AppTrust.Service.Tests;

public class SecureActionControllerTests
{
    [Fact]
    public async Task ExecuteAction_WhenTokenIsValid_ReturnsCorrelationId()
    {
        // Arrange
        var expectedCorrelationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var mockTrustStrategy = new Mock<IInboundTrustStrategy>();
        mockTrustStrategy
            .Setup(x => x.VerifyAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new TrustResult(true, AppConstants.ClientIdentifier));

        var mockCorrelationIdGenerator = new Mock<ICorrelationIdGenerator>();
        mockCorrelationIdGenerator
            .Setup(x => x.Generate())
            .Returns(expectedCorrelationId);

        var controller = new SecureActionController(
            mockTrustStrategy.Object,
            mockCorrelationIdGenerator.Object,
            NullLogger<SecureActionController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
        var result = await controller.ExecuteAction();

        // Assert — controller returns an anonymous object; serialize to inspect properties
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<System.Text.Json.JsonElement>(
            System.Text.Json.JsonSerializer.SerializeToElement(ok.Value));

        Assert.Equal("Secure action executed successfully!", body.GetProperty("Message").GetString());
        Assert.Equal(AppConstants.ClientIdentifier, body.GetProperty("Caller").GetString());
        Assert.Equal(expectedCorrelationId, body.GetProperty("CorrelationId").GetGuid());

        mockCorrelationIdGenerator.Verify(x => x.Generate(), Times.Once);
    }
}
