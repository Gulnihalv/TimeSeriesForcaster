
namespace TimeSeriesForecaster.Application.DTOs;
public class DatasetStatisticsDto
{
    public decimal? Mean { get; set; }
    public decimal? Median { get; set; }
    public decimal? StdDev { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public decimal? CoefficientOfVariation { get; set; }
}
