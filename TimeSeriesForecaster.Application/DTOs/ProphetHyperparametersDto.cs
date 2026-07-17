namespace TimeSeriesForecaster.Application.DTOs;

// Prophet Modeli için hiperparametreler, ileride diğer modeller gelince adında değişikliğe gidilebilir
public class ProphetHyperparametersDto
{
    public string? SeasonalityMode { get; set; }
    public double? ChangepointPriorScale { get; set; }
    public double? SeasonalityPriorScale { get; set; }
    public double? ChangepointRange { get; set; }
}