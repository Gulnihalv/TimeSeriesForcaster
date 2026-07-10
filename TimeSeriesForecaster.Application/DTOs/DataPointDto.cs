namespace TimeSeriesForecaster.Application.DTOs;

public class DataPointDto
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public bool IsOutlier { get; set; }
}