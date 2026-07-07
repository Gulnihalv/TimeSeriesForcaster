namespace TimeSeriesForecaster.Application.DTOs;

public class ModelDetailDto : ModelDto
{
    public IEnumerable<PredictionDto> Predictions { get; set; } = new List<PredictionDto>();
    public IEnumerable<ModelMetricDto> Metrics { get; set; } = new List<ModelMetricDto>();
}