namespace TimeSeriesForecaster.Application.DTOs;

public class ComponentPointDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

public class ModelComponentsDto
{
    public List<ComponentPointDto> Trend { get; set; } = new();
    // Az veri noktalı dataset'lerde Prophet bu bileşenleri hiç üretmeyebilir - null bu yüzden normal bir durum.
    public List<ComponentPointDto>? Weekly { get; set; }
    public List<ComponentPointDto>? Yearly { get; set; }
}