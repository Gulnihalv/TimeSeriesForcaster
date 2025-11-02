namespace TimeSeriesForecaster.Domain.Entities;

public class ModelMetric
{
    public int Id { get; set; }
    public int ModelId { get; set; }
    public Model? Model { get; set; } // Navigation property
    public MetricName? MetricName { get; set; }
    public decimal MetricValue { get; set; }
    public DateTime CalculatedAt { get; set; }
}
