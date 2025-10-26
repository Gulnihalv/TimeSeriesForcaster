using Microsoft.AspNetCore.Http;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IDatasetService
{
    Task<DatasetDto?> GetDatasetByIdAsync(int datasetId, int userId);
    Task<IEnumerable<DatasetDto>> GetAllDatasetsForProjectAsync(int projectId, int userId);
    Task<DatasetDto?> CreateDatasetFromUploadAsync(int projectId, int userId, string name, IFormFile file);
}
