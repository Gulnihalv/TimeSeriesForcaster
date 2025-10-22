using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Contracts.Persistence;

public interface IModelMetricRepository
{
    Task<ModelMetric?> GetMetricByIdAsync(int id, bool trackChanges);
    Task<IEnumerable<ModelMetric>> GetMetricsForModelAsync(int modelId, bool trackChanges);
    Task<ModelMetric?> GetLatestMetricForModelAsync(int modelId, MetricName metricName);
    Task<IEnumerable<ModelMetric>> GetMetricsByNameAsync(MetricName metricName, bool trackChanges);
    void CreateMetric(ModelMetric metric);
    Task CreateMetricsAsync(IEnumerable<ModelMetric> metrics); // Bulk insert
    void RemoveMetric(ModelMetric metric);
    Task RemoveMetricsForModelAsync(int modelId);
    void UpdateMetric(ModelMetric metric);
    Task<bool> MetricExistsAsync(int id); // Belirli bir metrik var mı
    Task<Dictionary<MetricName, decimal>> GetAllMetricsForModelAsync(int modelId); // Key-Value pairs
    Task<IEnumerable<ModelMetric>> GetMetricsForComparisonAsync(IEnumerable<int> modelIds, MetricName metricName);
    public Task<bool> UserOwnsMetricAsync(int metricId, int userId);
}
