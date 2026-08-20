namespace TimeSeriesForecaster.Application.DTOs;

public class ResolutionOptionDto
{
    public TimeResolution Resolution { get; set; }
    public int EstimatedPoints { get; set; }
    public bool IsAllowed { get; set; }
    public bool IsRecommended { get; set; }
}
