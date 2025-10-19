using Microsoft.EntityFrameworkCore;
using TimeSeriesForecaster.Application.Interfaces;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Repositories;

public class ModelMetricRepository : IModelMetricRepository
{
    private readonly AppDbContext _context;
    public ModelMetricRepository(AppDbContext context)
    {
        _context = context;
    }

    public void CreateMetric(ModelMetric metric) => _context.ModelMetrics.Add(metric);
    public Task CreateMetricsAsync(IEnumerable<ModelMetric> metrics) => _context.ModelMetrics.AddRangeAsync(metrics);
    public void RemoveMetric(ModelMetric metric) => _context.ModelMetrics.Remove(metric);
    public void UpdateMetric(ModelMetric metric) => _context.ModelMetrics.Update(metric);
    public async Task RemoveMetricsForModelAsync(int modelId)
    {
        await _context.ModelMetrics
        .Where(p => p.ModelId == modelId)
        .ExecuteDeleteAsync();
    }

    public async Task<Dictionary<MetricName, decimal>> GetAllMetricsForModelAsync(int modelId)
    {
        var metrics = await _context.ModelMetrics
            .AsNoTracking()
            .Where(m => m.ModelId == modelId && m.MetricName != null)
            .ToListAsync();

        var dict = metrics.ToDictionary(m => m.MetricName!.Value, m => m.MetricValue);

        return dict;
    }

    public async Task<ModelMetric?> GetLatestMetricForModelAsync(int modelId, MetricName metricName)
    {
        return await _context.ModelMetrics
            .AsNoTracking()
            .Where(m => m.ModelId == modelId && m.MetricName == metricName)
            .OrderByDescending(m => m.CalculatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<ModelMetric?> GetMetricByIdAsync(int id, bool trackChanges)
    {
        IQueryable<ModelMetric> query = trackChanges
            ? _context.ModelMetrics
            : _context.ModelMetrics.AsNoTracking();

        return await query.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<ModelMetric>> GetMetricsByNameAsync(MetricName metricName, bool trackChanges)
    {
        IQueryable<ModelMetric> query = trackChanges
            ? _context.ModelMetrics
            : _context.ModelMetrics.AsNoTracking();

        return await query.Where(m => m.MetricName == metricName).ToListAsync();
    }

    public async Task<IEnumerable<ModelMetric>> GetMetricsForComparisonAsync(IEnumerable<int> modelIds, MetricName metricName)
    {
        return await _context.ModelMetrics
        .AsNoTracking()
        .Where(m => m.MetricName == metricName && modelIds.Contains(m.ModelId))
        .ToListAsync();
    }

    public async Task<IEnumerable<ModelMetric>> GetMetricsForModelAsync(int modelId, bool trackChanges)
    {
        IQueryable<ModelMetric> query = trackChanges
            ? _context.ModelMetrics
            : _context.ModelMetrics.AsNoTracking();

        return await query.Where(m => m.ModelId == modelId).ToListAsync();
    }

    public async Task<bool> MetricExistsAsync(int id)
    {
        return await _context.ModelMetrics.AnyAsync(m => m.Id == id);
    }

    public async Task<bool> UserOwnsMetricAsync(int metricId, int userId)
    {
        return await _context.ModelMetrics
            .AnyAsync(m => m.Id == metricId && m.Model!.Project!.UserId == userId);
    }
}
