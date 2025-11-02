namespace TimeSeriesForecaster.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; } // Navigation property
    public string? Name { get; set; }
    public ICollection<Dataset>? Datasets { get; set; } // Collection navigation property
    public ICollection<Model>? Models { get; set; } // Collection navigation property
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
