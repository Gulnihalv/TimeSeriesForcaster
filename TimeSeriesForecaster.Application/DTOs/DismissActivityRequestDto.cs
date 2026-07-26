namespace TimeSeriesForecaster.Application.DTOs;

public class DismissActivityRequestDto
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
}
