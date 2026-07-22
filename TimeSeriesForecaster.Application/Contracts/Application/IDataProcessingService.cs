using TimeSeriesForecaster.Application.Common;
namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IDataProcessingService
{
    Task<Result> ProcessDatasetAsync(int datasetId, CancellationToken cancellationToken = default);
}
