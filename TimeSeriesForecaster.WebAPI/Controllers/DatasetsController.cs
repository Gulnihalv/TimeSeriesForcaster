using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.WebAPI.Extensions;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/datasets")]
public class DatasetsController : ControllerBase
{
    private readonly IDatasetService _datasetService;

    public DatasetsController(IDatasetService datasetService)
    {
        _datasetService = datasetService;
    }

    [HttpPost("/api/projects/{projectId}/datasets/upload")]
    public async Task<IActionResult> CreateDatasetFromUpload(int projectId, [FromForm] string name, IFormFile file)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var result = await _datasetService.CreateDatasetFromUploadAsync(projectId, userId.Value, name, file);
        if (result == null)
        {
            return Forbid("Yetkin Yok!");
        }

        return CreatedAtAction(nameof(GetDatasetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}", Name = "GetDatasetById")]
    public async Task<IActionResult> GetDatasetById(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var dataset = await _datasetService.GetDatasetByIdAsync(datasetId: id, userId: userId.Value);
        if (dataset == null)
        {
            return NotFound("Dataset bulunamadı veya bu kullanıcıya ait değil.");
        }

        return Ok(dataset);
    }
    
    [HttpGet("/api/projects/{projectId}/datasets")]
    public async Task<IActionResult> GetAllDatasetsForProject(int projectId)
    {
         var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var results = await _datasetService.GetAllDatasetsForProjectAsync(projectId: projectId, userId: userId.Value);
        return Ok(results);
    }
}
