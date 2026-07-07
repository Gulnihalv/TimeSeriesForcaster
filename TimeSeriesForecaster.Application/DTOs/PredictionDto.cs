namespace TimeSeriesForecaster.Application.DTOs;

public class PredictionDto
{
    public long Id { get; set; }
    public DateTime PredictionDate { get; set; }
    public decimal PredictedValue { get; set; }
    public decimal ConfidenceLower { get; set; }
    public decimal ConfidenceUpper { get; set; }
    public decimal? ActualValue { get; set; }
}