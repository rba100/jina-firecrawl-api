using Microsoft.AspNetCore.Mvc;

namespace JinaFirecrawlApi.Controllers;

[ApiController]
[Route("")]
public class RootController : ControllerBase
{
    [HttpGet]
    [Route("")]
    [Route("/")]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }
}
