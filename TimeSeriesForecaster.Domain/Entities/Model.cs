namespace TimeSeriesForecaster.Domain.Entities;

public class Model
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; } // Navigation property
    public int DatasetId { get; set; }
    public Dataset? Dataset { get; set; } // Navigation property
    public ICollection<Prediction>? Predictions { get; set; } // Collection navigation property
    public ICollection<ModelMetric>? ModelMetrics { get; set; } // Collection navigation property
    public string? ModelName { get; set; }
    public string? ModelType { get; set; }
    public string? Algorithm { get; set; }
    public string? Hyperparameters { get; set; } // burası daha sonra jsonb olarak değişebilir.
    public string? ModelFilePath { get; set; }
    public ModelStatus? Status { get; set; }
    public string? HangfireJobId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? TrainingStartedAt { get; set; }
    public DateTime? TrainingCompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

}
