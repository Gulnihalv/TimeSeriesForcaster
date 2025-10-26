
using System.ComponentModel.DataAnnotations;

namespace TimeSeriesForecaster.Application.DTOs;

public class DatasetDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Dataset name is required")]
    public string? Name { get; set; }

    public string? OriginalFileName { get; set; }
    public string? FilePath { get; set; }
    public int RecordCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsProcessed { get; set; }
}
