using TimeSeriesForecaster.Application.Interfaces;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TimeSeriesForecaster.Infrastructure.Repositories;

public class PredictionRepository : IPredictionRepository
{
    private readonly AppDbContext _context;
    public PredictionRepository(AppDbContext context)
    {
        _context = context;
    }

    public void CreatePrediction(Prediction prediction) => _context.Predictions.Add(prediction);
    public async Task CreatePredictionsBulkAsync(IEnumerable<Prediction> predictions) => await _context.Predictions.AddRangeAsync(predictions);
    public void RemovePrediction(Prediction prediction) => _context.Predictions.Remove(prediction);
    public void UpdatePrediction(Prediction prediction) => _context.Predictions.Update(prediction);

    public async Task<IEnumerable<Prediction>> GetAnomaliesForModelAsync(int modelId)
    {
        return await _context.Predictions
            .AsNoTracking()
            .Where(p => p.ModelId == modelId && p.IsAnomaly)
            .ToListAsync();
    }

    public async Task<Prediction?> GetPredictionByIdAsync(int id, bool trackChanges)
    {
        IQueryable<Prediction> query = trackChanges ? _context.Predictions : _context.Predictions.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public Task<int> GetPredictionsCountAsync(int modelId)
    {
        return _context.Predictions.CountAsync(p => p.ModelId == modelId);
    }

    public async Task<IEnumerable<Prediction>> GetPredictionsForModelAsync(int modelId, bool trackChanges)
    {
        IQueryable<Prediction> query = _context.Predictions;

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query
            .Where(p => p.ModelId == modelId)
            .OrderBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Prediction>> GetPredictionsPagedAsync(int modelId, int page, int pageSize)
    {
        return await _context.Predictions
            .AsNoTracking()
            .Where(p => p.ModelId == modelId)
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Prediction>> GetPredictionsWithActualsAsync(int modelId) // accuracy comparison için
    {
        return await _context.Predictions
            .AsNoTracking()
            .Where(p => p.ModelId == modelId && p.ActualValue != 0) // ActualValue 0 değilse, yani gerçek değer varsa
            .ToListAsync();
    }

    public async Task RemovePredictionsForModelAsync(int modelId)
    {
        await _context.Predictions
        .Where(p => p.ModelId == modelId)
        .ExecuteDeleteAsync();
    }

    public async Task<bool> UserOwnsPredictionAsync(int predictionId, int userId)
    {
        return await _context.Predictions
            .AnyAsync(p => p.Id == predictionId && p.Model!.Project!.UserId == userId);
    }
}
