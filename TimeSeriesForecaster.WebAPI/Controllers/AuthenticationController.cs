using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ApiControllerBase
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

        if (!result.IsSuccess || !result.Value!.Succeeded)
        {
            return BadRequest(result.IsSuccess ? result.Value!.Errors : result.Error);
        }

        return CreatedAtAction(nameof(Register), new { email = userForRegistrationDto.Email }, userForRegistrationDto);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserForAuthenticationDto userForAuthDto)
    {
        var result = await _authService.LoginAsync(userForAuthDto);
        return ToActionResult(result, token => Ok(new { Token = token }));
    }
}
