using Microsoft.AspNetCore.Mvc;
using AppA.Infrastructure;

namespace AppA.API.Controllers;

[ApiController]
[Route("api/action")]
public class ActionController : ControllerBase
{
    private readonly AppBClient _client;

    public ActionController(AppBClient client) => _client = client;

    [HttpPost("trigger")]
    public async Task<IActionResult> TriggerSecureAction()
    {
        var result = await _client.CallSecureEndpointAsync();
        if (result == null) return StatusCode(500, "Failed to execute secure action via App B.");
        return Ok(new { result.Message, result.Caller, result.SessionId });
    }
}
