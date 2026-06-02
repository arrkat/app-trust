using Microsoft.AspNetCore.Mvc;
using AppB.Contracts;

namespace AppB.API.Controllers;

[ApiController]
[Route("api/secure-action")]
public class SecureActionController : ControllerBase
{
    private readonly ITokenValidator _tokenValidator;
    private readonly ISessionService _sessionService;
    private readonly ILogger<SecureActionController> _logger;

    public SecureActionController(
        ITokenValidator tokenValidator,
        ISessionService sessionService,
        ILogger<SecureActionController> logger)
    {
        _tokenValidator = tokenValidator;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ExecuteAction()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Unauthorized attempt: Missing Authorization header.");
            return Unauthorized();
        }

        var token = authHeader["Bearer ".Length..];
        var validationResult = await _tokenValidator.ValidateTokenAsync(token);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Unauthorized attempt: Token validation failed.");
            return Unauthorized();
        }

        var sessionId = _sessionService.CreateSession(validationResult.CallerId!);
        
        _logger.LogInformation("Secure action executed successfully for caller: {Caller} with session ID: {SessionId}", validationResult.CallerId, sessionId);
        return Ok(new { message = "Secure action executed successfully!", caller = validationResult.CallerId, sessionId });
    }
}
