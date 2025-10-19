using Microsoft.EntityFrameworkCore;
using TimeSeriesForecaster.Application.Interfaces;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Repositories;

public class ModelRepository: IModelRepository
{
    private readonly AppDbContext _context;
    public ModelRepository(AppDbContext context)
    {
        _context = context;
    }

    public void CreateModel(Model model) => _context.Models.Add(model);
    public void RemoveModel(Model model) => _context.Models.Remove(model);
    public void UpdateModel(Model model) => _context.Models.Update(model);

    public async Task<Model?> GetActiveModelForDatasetAsync(int datasetId, bool trackChanges)
    {
        IQueryable<Model> query = trackChanges
            ? _context.Models
            : _context.Models.AsNoTracking();

        return await query
            .Where(m => m.DatasetId == datasetId && (m.Status == ModelStatus.Created || m.Status == ModelStatus.Training))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Model>> GetCompletedModelsForDatasetAsync(int datasetId)
    {
        return await _context.Models
            .AsNoTracking()
            .Where(m => m.DatasetId == datasetId && m.Status == ModelStatus.Completed)
            .OrderByDescending(m => m.TrainingCompletedAt)
            .ToListAsync();
    }

    public async Task<Model?> GetModelByIdAsync(int id, bool trackChanges)
    {
        IQueryable<Model> query = trackChanges
            ? _context.Models
            : _context.Models.AsNoTracking();

        return await query.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Model>> GetModelsByStatusAsync(ModelStatus status, bool trackChanges)
    {
        IQueryable<Model> query = trackChanges
            ? _context.Models
            : _context.Models.AsNoTracking();

        return await query
            .Where(m => m.Status == status)
            .ToListAsync();
    }

    public async Task<IEnumerable<Model>> GetModelsForDatasetAsync(int datasetId, bool trackChanges)
    {
        IQueryable<Model> query = trackChanges
            ? _context.Models
            : _context.Models.AsNoTracking();

        return await query
            .Where(m => m.DatasetId == datasetId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Model>> GetModelsForProjectAsync(int projectId, bool trackChanges)
    {
        IQueryable<Model> query = trackChanges
            ? _context.Models
            : _context.Models.AsNoTracking();

        return await query
            .Where(m => m.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<Model?> GetModelWithMetricsAsync(int id, bool trackChanges)
    {
        IQueryable<Model> query = trackChanges
            ? _context.Models
            : _context.Models.AsNoTracking();

        return await query
            .Include(m => m.ModelMetrics)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<bool> ModelExistsAsync(int id)
    {
        return await _context.Models.AnyAsync(m => m.Id == id);
    }

    public async Task<bool> UserOwnsModelAsync(int modelId, int userId)
    {
        return await _context.Models.AnyAsync(m => m.Id == modelId && m.Project!.UserId == userId);
    }
}
