using Microsoft.AspNetCore.Mvc;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new{ message = "Pong" });
    }
}
