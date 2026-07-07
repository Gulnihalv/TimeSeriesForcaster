namespace TimeSeriesForecaster.Application.DTOs;

public class GenerateForecastRequestDto
{
    public int Horizon { get; set; } = 30;
}