using TimeSeriesForecaster.Application.Models;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Contracts.Persistence;

public interface IDataPointRepository
{
    Task<DataPoint?> GetDataPointByIdAsync(int id, bool trackChanges);
    Task<IEnumerable<DataPoint>> GetDataPointsPagedAsync(int datasetId, int page, int pageSize);
    Task<IEnumerable<DataPoint>> GetDataPointsAsync(int datasetId, DateTime? startDate = null, DateTime? endDate = null, int? limit = null, int? maxPoints = null);
    void CreateDataPoint(DataPoint dataPoint);
    void UpdateDataPoint(DataPoint dataPoint);
    void RemoveDataPoint(DataPoint dataPoint);
    Task<int> GetDataPointsCountAsync(int datasetId);
    Task<IEnumerable<DataPoint>> GetOutliersAsync(int datasetId);
    Task RemoveDataPointsForDatasetAsync(int datasetId, CancellationToken cancellationToken = default);
    Task BulkCopyDataPointsAsync(IEnumerable<DataPoint> dataPoints, CancellationToken cancellationToken = default);
    Task<DatasetStatistics> GetStatisticsAsync(int datasetId, CancellationToken cancellationToken = default);
}
