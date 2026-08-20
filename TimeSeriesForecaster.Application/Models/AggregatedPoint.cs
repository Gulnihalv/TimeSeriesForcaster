namespace TimeSeriesForecaster.Application.Models;

public record AggregatedPoint
{
    public DateTime Timestamp { get; init; }
    public decimal Value { get; init; }
}

