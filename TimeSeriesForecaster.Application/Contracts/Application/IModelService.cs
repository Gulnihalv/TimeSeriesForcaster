using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IModelService
{
    Task<ModelDto?> GetModelByIdAsync(int modelId, int userId);
    Task<ModelDetailDto?> GetModelDetailByIdAsync(int modelId, int userId);
    Task<IEnumerable<ModelDto>> GetAllModelsForDatasetAsync(int datasetId, int userId);
    Task<ModelDto?> TrainModelAsync(int datasetId, int userId, string algorithm, ProphetHyperparametersDto? hyperparameters = null);
    Task<bool> GenerateForecastAsync(int modelId, int userId, int horizon);
    Task<bool> DeleteModelAsync(int modelId, int userId);
    Task<ModelComponentsDto?> GetModelComponentsAsync(int modelId, int userId);
}