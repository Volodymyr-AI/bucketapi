using Microsoft.AspNetCore.Mvc;

namespace Order.WebApi.Controllers;

[ApiController]
[Route("debug")]
public class DebugController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DebugController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("connection-string")]
    public IActionResult GetConnectionString()
    {
        var connStr = _configuration.GetConnectionString("DefaultConnection");
        return Ok(new { ConnectionString = connStr });
    }
}