using Microsoft.AspNetCore.Identity;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Application.Interfaces;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

// sonrasında silme güncelleme ve rol ile ilgili methodlar eklenecek.

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IdentityResult> RegisterUserAsync(UserForRegistrationDto userForRegistrationDto)
    {
        var user = new AppUser
        {
            UserName = userForRegistrationDto.Email,
            Email = userForRegistrationDto.Email,
            FirstName = userForRegistrationDto.FirstName,
            LastName = userForRegistrationDto.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
            IsAdmin = false
        };

        var result = await _userManager.CreateAsync(user, userForRegistrationDto.Password!);
        return result;
    }

    public async Task<bool> ValidateUserAsync(UserForAuthenticationDto userForAuthenticationDto)
    {
        var user = await _userManager.FindByEmailAsync(userForAuthenticationDto.Email!);

        if (user == null)
        {
            return false;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, userForAuthenticationDto.Password!, lockoutOnFailure: false);
        return result.Succeeded;
    }
}
