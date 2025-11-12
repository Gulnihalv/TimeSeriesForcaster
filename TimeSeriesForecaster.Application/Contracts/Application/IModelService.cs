using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IModelService
{
    Task<ModelDto?> GetModelByIdAsync(int modelId, int userId);
    Task<IEnumerable<ModelDto>> GetAllModelsForDatasetAsync(int datasetId, int userId);
    Task<ModelDto?> TrainModelAsync(int datasetId, int userId, string algorithm);
}
