namespace TimeSeriesForecaster.Application.DTOs;

public class ModelDto
{
    public int ProjectId { get; set; }
    public int DatasetId { get; set; }
    public string? ModelName { get; set; }
    public string? Algorithm { get; set; }
    public string? Hyperparameters { get; set; }
    public string? ModelFilePath { get; set; }
    public ModelStatus? Status { get; set; }
    public DateTime? TrainingStartedAt { get; set; }
    public DateTime? TrainingCompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
