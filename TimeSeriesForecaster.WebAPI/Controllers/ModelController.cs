using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.WebAPI.Extensions;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/models")]
public class ModelController : ApiControllerBase
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

        var result = await _modelService.TrainModelAsync(datasetId, userId.Value, request.Algorithm, request.Hyperparameters);
        return ToActionResult(result, value => CreatedAtAction(nameof(GetModelById), new { id = value!.Id }, value));
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

        var result = await _modelService.GenerateForecastAsync(modelId: id, userId: userId.Value, horizon: request.Horizon);
        return ToActionResult(result, () => Accepted());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteModel(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var result = await _modelService.DeleteModelAsync(id, userId.Value);
        return ToActionResult(result);
    }

    [HttpGet("{id}/components")]
    public async Task<IActionResult> GetModelComponents(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var result = await _modelService.GetModelComponentsAsync(id, userId.Value);
        return ToActionResult(result);
    }
}