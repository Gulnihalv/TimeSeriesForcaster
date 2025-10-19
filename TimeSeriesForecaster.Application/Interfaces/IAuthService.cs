using Microsoft.AspNetCore.Identity;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Interfaces;

public interface IAuthService
{
    Task<IdentityResult> RegisterUserAsync(UserForRegistrationDto userForRegistrationDto);
    Task<string?> LoginAsync(UserForAuthenticationDto userForAuthenticationDto);
}
