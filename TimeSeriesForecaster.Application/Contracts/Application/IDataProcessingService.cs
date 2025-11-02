namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IDataProcessingService
{
    Task ProcessDatasetAsync(int datasetId, CancellationToken cancellationToken = default);
}
