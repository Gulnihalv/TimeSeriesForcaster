using Microsoft.AspNetCore.Identity;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IAuthService
{
    Task<Result<IdentityResult>> RegisterUserAsync(UserForRegistrationDto userForRegistrationDto);
    Task<Result<string?>> LoginAsync(UserForAuthenticationDto userForAuthenticationDto);
}
