using TimeSeriesForecaster.Application.Common;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IModelProcessingService
{
    Task<Result> ProcessModelAsync(int modelId, TimeResolution resolution, AggregationFunction aggregation = AggregationFunction.None, CancellationToken cancellationToken = default);
}
