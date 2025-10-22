using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Contracts.Persistence;

public interface IPredictionRepository
{
    Task<Prediction?> GetPredictionByIdAsync(int id, bool trackChanges);
    Task<IEnumerable<Prediction>> GetPredictionsForModelAsync(int modelId, bool trackChanges);
    Task<IEnumerable<Prediction>> GetPredictionsPagedAsync(int modelId, int page, int pageSize);
    void CreatePrediction(Prediction prediction);
    Task CreatePredictionsBulkAsync(IEnumerable<Prediction> predictions);
    void UpdatePrediction(Prediction prediction);
    void RemovePrediction(Prediction prediction);
    Task RemovePredictionsForModelAsync(int modelId);
    Task<int> GetPredictionsCountAsync(int modelId);
    Task<IEnumerable<Prediction>> GetAnomaliesForModelAsync(int modelId);
    Task<IEnumerable<Prediction>> GetPredictionsWithActualsAsync(int modelId); // accuracy comparison için
    public Task<bool> UserOwnsPredictionAsync(int predictionId, int userId);
}
