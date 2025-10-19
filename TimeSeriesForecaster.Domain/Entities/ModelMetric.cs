namespace TimeSeriesForecaster.Domain.Entities;

public enum MetricName
    {
        MAE,      // Mean Absolute Error
        RMSE,     // Root Mean Squared Error
        MAPE,     // Mean Absolute Percentage Error
        R2Score,  // R-squared
        MSE       // Mean Squared Error
    }

public class ModelMetric
{
    public int Id { get; set; }
    public int ModelId { get; set; }
    public Model? Model { get; set; } // Navigation property
    public MetricName? MetricName { get; set; }
    public decimal MetricValue { get; set; }
    public DateTime CalculatedAt { get; set; }
}
