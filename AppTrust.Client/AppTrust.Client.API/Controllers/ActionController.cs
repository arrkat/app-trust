using Microsoft.AspNetCore.Mvc;
using AppTrust.Client.Infrastructure;

namespace AppTrust.Client.API.Controllers;

[ApiController]
[Route("api/action")]
public class ActionController : ControllerBase
{
    private readonly AppTrustServiceClient _client;
    private readonly ILogger<ActionController> _logger;

    public ActionController(AppTrustServiceClient client, ILogger<ActionController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpPost("trigger")]
    public async Task<IActionResult> TriggerSecureAction()
    {
        try
        {
            var result = await _client.CallSecureEndpointAsync();
            return Ok(new { result.Message, result.Caller, result.CorrelationId });
        }
        catch (HttpRequestException ex) when (ex.StatusCode is not null)
        {
            return StatusCode((int)ex.StatusCode.Value, "Secure action failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Secure action failed before or during the AppTrust.Service call.");
            return StatusCode(500, "Failed to execute secure action via AppTrust.Service.");
        }
    }
}
