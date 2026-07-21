using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IModelService
{
    Task<Result<ModelDto?>> GetModelByIdAsync(int modelId, int userId);
    Task<Result<ModelDetailDto?>> GetModelDetailByIdAsync(int modelId, int userId);
    Task<Result<IEnumerable<ModelDto>>> GetAllModelsForDatasetAsync(int datasetId, int userId);
    Task<Result<ModelDto?>> TrainModelAsync(int datasetId, int userId, string algorithm, ProphetHyperparametersDto? hyperparameters = null);
    Task<Result> GenerateForecastAsync(int modelId, int userId, int horizon);
    Task<Result> DeleteModelAsync(int modelId, int userId);
    Task<Result<ModelComponentsDto?>> GetModelComponentsAsync(int modelId, int userId);
}