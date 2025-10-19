using System.ComponentModel.DataAnnotations;

namespace TimeSeriesForecaster.Application.DTOs;

public class UserForAuthenticationDto
{
    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "Email is required")]
    public string? Email { get; init; }

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Password is required")]
    public string? Password { get; init; }

}
