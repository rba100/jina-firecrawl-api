using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JinaFirecrawlApi.Controllers;

[ApiController]
[Route("")]
public class RootController : ControllerBase
{
    [HttpGet("")]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }
}
