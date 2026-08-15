using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.WebAPI.Constants;
using TimeSeriesForecaster.WebAPI.Extensions;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/datasets")]
public class DatasetsController : ApiControllerBase
{
    private readonly IDatasetService _datasetService;

    public DatasetsController(IDatasetService datasetService)
    {
        _datasetService = datasetService;
    }

    [HttpPost("/api/projects/{projectId}/datasets/upload")]
    [RequestSizeLimit(524_288_000)] // ~500 MB - Kestrel'in varsayılan 30 MB sınırını bu endpoint için gevşetir
    [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)] // form/multipart okuma katmanının kendi ayrı sınırı
    public async Task<IActionResult> CreateDatasetFromUpload(int projectId, [FromForm] string name, IFormFile file, [FromForm] string dateColumnName, [FromForm] string targetColumnName)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _datasetService.CreateDatasetFromUploadAsync(projectId, userId.Value, name, file, dateColumnName, targetColumnName);
        return ToActionResult(result, value => CreatedAtAction(nameof(GetDatasetById), new { id = value!.Id }, value));
    }

    [HttpGet("{id}", Name = "GetDatasetById")]
    public async Task<IActionResult> GetDatasetById(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _datasetService.GetDatasetByIdAsync(datasetId: id, userId: userId.Value);
        return ToActionResult(result, value => Ok(value));
    }
    
    [HttpGet("/api/projects/{projectId}/datasets")]
    public async Task<IActionResult> GetAllDatasetsForProject(int projectId)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _datasetService.GetAllDatasetsForProjectAsync(projectId: projectId, userId: userId.Value);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDataset(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _datasetService.DeleteDatasetAsync(id, userId.Value);
        return ToActionResult(result);
    }

    [HttpGet("{id}/datapoints")]
    public async Task<IActionResult> GetDataPoints(int id, [FromQuery] int maxPoints = 2000)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _datasetService.GetDataPointsForDatasetAsync(id, userId.Value, maxPoints);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> GetStatistics(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _datasetService.GetDatasetStatisticsAsync(id, userId.Value);
        return ToActionResult(result, value => Ok(value));
    }
}