using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Application.Interfaces;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthenticationController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserForRegistrationDto userForRegistrationDto)
    {
        var result = await _authService.RegisterUserAsync(userForRegistrationDto);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(Register), new { email = userForRegistrationDto.Email }, userForRegistrationDto);
    }
}
