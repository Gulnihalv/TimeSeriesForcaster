namespace TimeSeriesForecaster.Application.Models;

public record DatasetStatistics
{
    public decimal? Mean { get; init; }
    public decimal? Median { get; init; }
    public decimal? StdDev { get; init; }
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public DateTime? MinDate { get; init; }
    public DateTime? MaxDate { get; init; }
    public decimal? CoefficientOfVariation { get; init; }
}
