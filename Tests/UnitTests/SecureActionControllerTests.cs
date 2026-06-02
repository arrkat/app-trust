using AppB.API.Controllers;
using AppB.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Infrastructure;

namespace UnitTests;

public class SecureActionControllerTests
{
    [Fact]
    public async Task ExecuteAction_WhenTokenIsValid_CreatesSessionAndReturnsSessionId()
    {
        var expectedSessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var mockValidator = new Mock<ITokenValidator>();
        mockValidator
            .Setup(x => x.ValidateTokenAsync("valid-token"))
            .ReturnsAsync(new TokenValidationResult(true, AppConstants.AppAIdentifier));

        var mockSessionService = new Mock<ISessionService>();
        mockSessionService
            .Setup(x => x.CreateSession(AppConstants.AppAIdentifier))
            .Returns(expectedSessionId);

        var controller = new SecureActionController(
            mockValidator.Object,
            mockSessionService.Object,
            NullLogger<SecureActionController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Authorization = "Bearer valid-token";

        var result = await controller.ExecuteAction();

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<System.Text.Json.JsonElement>(
            System.Text.Json.JsonSerializer.SerializeToElement(ok.Value));

        Assert.Equal("Secure action executed successfully!", body.GetProperty("message").GetString());
        Assert.Equal(AppConstants.AppAIdentifier, body.GetProperty("caller").GetString());
        Assert.Equal(expectedSessionId, body.GetProperty("sessionId").GetGuid());

        mockSessionService.Verify(x => x.CreateSession(AppConstants.AppAIdentifier), Times.Once);
    }
}
