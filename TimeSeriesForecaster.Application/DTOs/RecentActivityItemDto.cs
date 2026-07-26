namespace TimeSeriesForecaster.Application.DTOs;

public class RecentActivityItemDto
{
    public string EntityType { get; set; } = string.Empty; // "Dataset" | "Model"
    public int EntityId { get; set; }
    public int ProjectId { get; set; }
    public int? DatasetId { get; set; } // null for Dataset items, set for Model items
    public string? Name { get; set; }
    public DashboardActivityStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
