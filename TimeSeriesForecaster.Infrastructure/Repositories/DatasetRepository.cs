using System.Data;
using Microsoft.EntityFrameworkCore;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Repositories;

public class DatasetRepository : IDatasetRepository
{
    private readonly AppDbContext _context;

    public DatasetRepository(AppDbContext context)
    {
        _context = context;
    }

    public void CreateDataset(Dataset dataset) => _context.Datasets.Add(dataset);
    public void RemoveDataset(Dataset dataset) => _context.Datasets.Remove(dataset);
    public void UpdateDataset(Dataset dataset) => _context.Datasets.Update(dataset);

    public async Task<bool> DatasetExistsAsync(int id)
    {
        return await _context.Datasets.AnyAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<Dataset>> GetAllDatasetsForProjectAsync(int projectId, bool trackChanges, bool includeUnprocessed = true)
    {
        IQueryable<Dataset> query = trackChanges
            ? _context.Datasets
            : _context.Datasets.AsNoTracking();

        query = query.Where(d => d.ProjectId == projectId);

        if (!includeUnprocessed)
        {
            query = query.Where(d => d.IsProcessed);
        }

        return await query.OrderBy(d => d.Name).ToListAsync();
    }

    public async Task<Dataset?> GetDatasetByIdAsync(int id, bool trackChanges)
    {
        IQueryable<Dataset> query = trackChanges
            ? _context.Datasets
            : _context.Datasets.AsNoTracking();

        return await query.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<int> GetDatasetRecordCountAsync(int id)
    {
        return await _context.DataPoints.CountAsync(d => d.DatasetId == id);
    }

    public async Task<bool> IsDatasetProcessedAsync(int id)
    {
        return await _context.Datasets.AnyAsync(d => d.Id == id && d.IsProcessed);
    }

    public async Task<bool> UserOwnsDatasetAsync(int datasetId, int userId)
    {
        return await _context.Datasets.AnyAsync(d => d.Id == datasetId && d.Project!.UserId == userId);
    }
}
