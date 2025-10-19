namespace TimeSeriesForecaster.Domain.Entities;

public class DataPoint
{
    public long Id { get; set; }
    public int DatasetId { get; set; }
    public Dataset? Dataset { get; set; } // Navigation property
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public bool IsOutlier { get; set; }
    public DateTime CreatedAt { get; set; }
}
