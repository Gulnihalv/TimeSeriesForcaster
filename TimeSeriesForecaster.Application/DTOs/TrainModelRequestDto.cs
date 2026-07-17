namespace TimeSeriesForecaster.Application.DTOs;

public class TrainModelRequestDto
{
    public string Algorithm { get; set; } = "prophet";
    public ProphetHyperparametersDto? Hyperparameters { get; set; }
}