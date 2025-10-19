using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TimeSeriesForecaster.Application.Configuration;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Application.Interfaces;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

// sonrasında silme güncelleme ve rol ile ilgili methodlar eklenecek.

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IOptions<JwtSettings> jwtSettingsOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettingsOptions.Value;
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

    private async Task<AppUser?> GetAuthenticatedUserAsync(UserForAuthenticationDto userForAuthenticationDto)
    {
        var user = await _userManager.FindByEmailAsync(userForAuthenticationDto.Email!);

        if (user == null)
        {
            return null;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, userForAuthenticationDto.Password!, lockoutOnFailure: false);
        return result.Succeeded ? user : null;
    }

    public async Task<string?> LoginAsync(UserForAuthenticationDto userForAuthenticationDto)
    {
        var user = await GetAuthenticatedUserAsync(userForAuthenticationDto);

        if (user == null)
        {
            return null; // belki hata verilebilir.
        }

        var jwtSecret = _jwtSettings.Secret;
        var issuer = _jwtSettings.Issuer;
        var audience = _jwtSettings.Audience;
        var durationInMinutes = Convert.ToInt32(_jwtSettings.DurationInMinutes);

        var claims = new[] // sonradan rol id gibi kısımlar eklenilebilir.
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim("firstname", user.FirstName!),
            new Claim("lastname", user.LastName!)
        };

        var symmetricSecurityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret!));

        var algorithms = SecurityAlgorithms.HmacSha256Signature;
        var credentials = new SigningCredentials(symmetricSecurityKey, algorithms);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(durationInMinutes),
            signingCredentials: credentials);

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);

        return tokenString;
    }
     
}
