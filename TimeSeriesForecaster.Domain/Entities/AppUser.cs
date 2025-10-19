using Microsoft.AspNetCore.Identity;

namespace TimeSeriesForecaster.Domain.Entities;

public class AppUser : IdentityUser<int>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Project>? Projects { get; set; } // Collection navigation property
    public ICollection<BackgroundJob>? BackgroundJobs { get; set; } // Collection navigation property
    public bool IsAdmin { get; set; }
}
