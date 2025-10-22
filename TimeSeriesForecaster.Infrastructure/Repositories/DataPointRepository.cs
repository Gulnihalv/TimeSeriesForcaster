using Microsoft.EntityFrameworkCore;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Repositories;

public class DataPointRepository : IDataPointRepository
{
    private readonly AppDbContext _context;

    public DataPointRepository(AppDbContext context)
    {
        _context = context;
    }

    // Bu metotlar sadece context'e ekleme/çıkarma/güncelleme yapıyor asıl işlemler serviste olacak.
    public void CreateDataPoint(DataPoint dataPoint) => _context.DataPoints.Add(dataPoint);
    public async Task CreateDataPointsBulkAsync(IEnumerable<DataPoint> dataPoints) => await _context.DataPoints.AddRangeAsync(dataPoints);
    public void RemoveDataPoint(DataPoint dataPoint)  => _context.DataPoints.Remove(dataPoint);
    public void UpdateDataPoint(DataPoint dataPoint) => _context.DataPoints.Update(dataPoint);
    public async Task RemoveDatapointsForDatasetAsync(int datasetId) => 
        await _context.DataPoints
            .Where(d => d.DatasetId == datasetId)
            .ExecuteDeleteAsync();

    public async Task<DataPoint?> GetDataPointByIdAsync(int id, bool trackChanges)
    {
        IQueryable<DataPoint> query = trackChanges 
            ? _context.DataPoints 
            : _context.DataPoints.AsNoTracking();

        return await query.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<DataPoint>> GetDataPointsAsync(int datasetId, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
    {
        var query = _context.DataPoints
            .AsNoTracking()
            .Where(d => d.DatasetId == datasetId);

        if (startDate.HasValue)
        {
            query = query.Where(d => d.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(d => d.Timestamp <= endDate.Value);
        }

        query = query.OrderBy(d => d.Timestamp);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }
    public async Task<IEnumerable<DataPoint>> GetDataPointsPagedAsync(int datasetId, int page, int pageSize)
    {
        return await _context.DataPoints
            .AsNoTracking()
            .Where(d => d.DatasetId == datasetId)
            .OrderBy(d => d.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    public async Task<IEnumerable<DataPoint>> GetOutliersAsync(int datasetId)
    {
        return await _context.DataPoints
            .AsNoTracking()
            .Where(d => d.DatasetId == datasetId && d.IsOutlier)
            .OrderBy(d => d.Timestamp)
            .ToListAsync();
    }
    public Task<int> GetDataPointsCountAsync(int datasetId) => _context.DataPoints.CountAsync(d => d.DatasetId == datasetId);
    public async Task<(DateTime minDate, DateTime maxDate)> GetDateRangeAsync(int datasetId)
    {
        var result = await _context.DataPoints
            .AsNoTracking()
            .Where(d => d.DatasetId == datasetId)
            .GroupBy(d => d.DatasetId)
            .Select(g => new
            {
                MinDate = g.Min(d => d.Timestamp),
                MaxDate = g.Max(d => d.Timestamp)
            })
            .FirstOrDefaultAsync();

        return result != null ? (result.MinDate, result.MaxDate) : (default, default);
    }
    public async Task<(decimal minValue, decimal maxValue)> GetValueRangeAsync(int datasetId)
    {
        var result = await _context.DataPoints
            .AsNoTracking()
            .Where(d => d.DatasetId == datasetId)
            .GroupBy(d => d.DatasetId)
            .Select(g => new
            {
                MinValue = g.Min(d => d.Value),
                MaxValue = g.Max(d => d.Value)
            })
            .FirstOrDefaultAsync();

        return result != null ? (result.MinValue, result.MaxValue) : (default, default);
    }
}
