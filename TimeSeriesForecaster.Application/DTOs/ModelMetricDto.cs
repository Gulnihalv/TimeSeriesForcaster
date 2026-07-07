namespace TimeSeriesForecaster.Application.DTOs;

public class ModelMetricDto
{
    public MetricName? MetricName { get; set; }
    public decimal MetricValue { get; set; }
    public DateTime CalculatedAt { get; set; }
}