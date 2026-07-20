using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.WebAPI.Extensions;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/models")]
public class ModelController : ControllerBase
{
    private readonly IModelService _modelService;

    public ModelController(IModelService modelService)
    {
        _modelService = modelService;
    }

    [HttpPost("/api/datasets/{datasetId}/models")]
    public async Task<IActionResult> TrainModel(int datasetId, [FromBody] TrainModelRequestDto request)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        try
        {
            var result = await _modelService.TrainModelAsync(datasetId, userId.Value, request.Algorithm, request.Hyperparameters);
            if (result == null)
            {
                return Forbid();
            }

            return CreatedAtAction(nameof(GetModelById), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    [HttpGet("/api/datasets/{datasetId}/models")]
    public async Task<IActionResult> GetAllModelsForDataset(int datasetId)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var results = await _modelService.GetAllModelsForDatasetAsync(datasetId: datasetId, userId: userId.Value);
        return Ok(results);
    }

    [HttpGet("{id}", Name = "GetModelById")]
    public async Task<IActionResult> GetModelById(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var model = await _modelService.GetModelDetailByIdAsync(modelId: id, userId: userId.Value);
        if (model == null)
        {
            return NotFound("Model bulunamadı veya bu kullanıcıya ait değil.");
        }

        return Ok(model);
    }

    [HttpPost("{id}/forecast")]
    public async Task<IActionResult> GenerateForecast(int id, [FromBody] GenerateForecastRequestDto request)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        try
        {
            await _modelService.GenerateForecastAsync(modelId: id, userId: userId.Value, horizon: request.Horizon);
            return Accepted(); // arka planda Hangfire job'ı işleyecek, henüz sonuç hazır değil
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteModel(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var deleted = await _modelService.DeleteModelAsync(id, userId.Value);
        if (!deleted)
        {
            return NotFound("Model bulunamadı veya bu kullanıcıya ait değil.");
        }

        return NoContent();
    }

    [HttpGet("{id}/components")]
    public async Task<IActionResult> GetModelComponents(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        try
        {
            var components = await _modelService.GetModelComponentsAsync(id, userId.Value);
            if (components == null)
            {
                return NotFound("Model bulunamadı veya bu kullanıcıya ait değil.");
            }

            return Ok(components);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}