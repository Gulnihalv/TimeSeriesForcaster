namespace TimeSeriesForecaster.Domain.Entities;

public class Prediction
{
    public long Id { get; set; }
    public int ModelId { get; set; }
    public Model? Model { get; set; } // Navigation property
    public DateTime PredictionDate { get; set; }
    public decimal PredictedValue { get; set; }
    public decimal ConfidenceLower { get; set; }
    public decimal ConfidenceUpper { get; set; }
    public decimal ActualValue { get; set; }
    public bool IsAnomaly { get; set; }
    public DateTime CreatedAt { get; set; }
}
