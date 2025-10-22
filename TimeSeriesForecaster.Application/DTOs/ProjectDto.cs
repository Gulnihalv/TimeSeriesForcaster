using System.ComponentModel.DataAnnotations;

namespace TimeSeriesForecaster.Application.DTOs;

public class ProjectDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Product name is required")]
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}
