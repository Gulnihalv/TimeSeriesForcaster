namespace TimeSeriesForecaster.Domain.Entities;

public class Dataset
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; } // Navigation property
    public ICollection<DataPoint>? DataPoints { get; set; } // Collection navigation property
    public ICollection<Model>? Models { get; set; } // Collection navigation property
    public string? Name { get; set; }
    public string? OriginalFileName { get; set; }
    public string? FilePath { get; set; }
    public string? Description { get; set; }
    public int RecordCount { get; set; }
    public string? DateColumn { get; set; }
    public string? TargetColumn { get; set; }
    public string? DataFrequency { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ProcessingStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? HangfireJobId { get; set; }
    public bool IsProcessed { get; set; }
}
