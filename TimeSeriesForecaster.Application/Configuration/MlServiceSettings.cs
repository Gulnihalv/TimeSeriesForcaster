namespace TimeSeriesForecaster.Application.Configuration;

public class MlServiceSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutTrainingMinutes { get; set; } = 15;
    public int TimeoutForecastingMinutes { get; set; } = 10;
}