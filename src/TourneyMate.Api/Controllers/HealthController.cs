using Microsoft.AspNetCore.Mvc;

namespace TourneyMate.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { ok = true, utc = DateTimeOffset.UtcNow });
}
