using Microsoft.AspNetCore.Http;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IDatasetService
{
    Task<IEnumerable<DatasetDto>> GetAllDatasetsForProjectAsync(int projectId, int userId);
    Task<DatasetDto?> GetDatasetByIdAsync(int datasetId, int userId);
    Task<DatasetDto?> CreateDatasetFromUploadAsync(int projectId, int userId, string name, IFormFile file, string dateColumnName, string targetColumnName);
    Task<bool> DeleteDatasetAsync(int datasetId, int userId);
    Task<IEnumerable<DataPointDto>?> GetDataPointsForDatasetAsync(int datasetId, int userId);
}