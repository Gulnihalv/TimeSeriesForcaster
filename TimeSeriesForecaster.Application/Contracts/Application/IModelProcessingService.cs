namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IModelProcessingService
{
    Task ProcessModelAsync(int modelId, CancellationToken cancellationToken = default);
}
