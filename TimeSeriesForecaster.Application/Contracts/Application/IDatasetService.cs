using Microsoft.AspNetCore.Http;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IDatasetService
{
    Task<Result<IEnumerable<DatasetDto>>> GetAllDatasetsForProjectAsync(int projectId, int userId);
    Task<Result<DatasetDto?>> GetDatasetByIdAsync(int datasetId, int userId);
    Task<Result<DatasetDto?>> CreateDatasetFromUploadAsync(int projectId, int userId, string name, IFormFile file, string dateColumnName, string targetColumnName);
    Task<Result<bool>> DeleteDatasetAsync(int datasetId, int userId);
    Task<Result<IEnumerable<DataPointDto>?>> GetDataPointsForDatasetAsync(int datasetId, int userId);
}