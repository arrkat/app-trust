using Microsoft.AspNetCore.Mvc;
using AppTrust.Service.Contracts;
using AppTrust.Sdk;

namespace AppTrust.Service.API.Controllers;

[ApiController]
[Route("api/secure-action")]
public class SecureActionController : ControllerBase
{
    private readonly IInboundTrustStrategy _trustStrategy;
    private readonly ICorrelationIdGenerator _correlationIdGenerator;
    private readonly ILogger<SecureActionController> _logger;

    public SecureActionController(
        IInboundTrustStrategy trustStrategy,
        ICorrelationIdGenerator correlationIdGenerator,
        ILogger<SecureActionController> logger)
    {
        _trustStrategy = trustStrategy;
        _correlationIdGenerator = correlationIdGenerator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ExecuteAction()
    {
        var trustResult = await _trustStrategy.VerifyAsync(HttpContext);

        if (!trustResult.IsValid)
        {
            _logger.LogWarning("Unauthorized attempt: Trust verification failed.");
            return Unauthorized();
        }

        var correlationId = _correlationIdGenerator.Generate();

        _logger.LogInformation(
            "Secure action executed successfully for caller: {Caller} with correlation ID: {CorrelationId}",
            trustResult.CallerId,
            correlationId);
        return Ok(new SecureActionResult("Secure action executed successfully!", trustResult.CallerId, correlationId));
    }
}
