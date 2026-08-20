namespace TimeSeriesForecaster.Application.DTOs;

public class TrainModelRequestDto
{
    public string Algorithm { get; set; } = "prophet";
    public ProphetHyperparametersDto? Hyperparameters { get; set; }
    public TimeResolution? TrainingResolution { get; set; }
    public AggregationFunction? TrainingAggregation { get; set; }
}