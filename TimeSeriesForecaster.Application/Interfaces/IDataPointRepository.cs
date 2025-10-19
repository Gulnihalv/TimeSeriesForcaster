using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Interfaces;

public interface IDataPointRepository
{
    Task<DataPoint?> GetDataPointByIdAsync(int id, bool trackChanges);
    Task<IEnumerable<DataPoint>> GetDataPointsPagedAsync(int datasetId, int page, int pageSize);
    Task<IEnumerable<DataPoint>> GetDataPointsAsync(int datasetId, DateTime? startDate = null, DateTime? endDate = null, int? limit = null);
    void CreateDataPoint(DataPoint dataPoint);
    Task CreateDataPointsBulkAsync(IEnumerable<DataPoint> dataPoints);
    void UpdateDataPoint(DataPoint dataPoint);
    void RemoveDataPoint(DataPoint dataPoint);
    Task RemoveDatapointsForDatasetAsync(int datasetId);
    Task<int> GetDataPointsCountAsync(int datasetId);
    Task<(DateTime minDate, DateTime maxDate)> GetDateRangeAsync(int datasetId);
    Task<(decimal minValue, decimal maxValue)> GetValueRangeAsync(int datasetId);
    Task<IEnumerable<DataPoint>> GetOutliersAsync(int datasetId);
}
