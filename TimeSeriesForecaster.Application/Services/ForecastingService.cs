using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Configuration;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

public class ForecastingService : IForecastingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IModelRepository _modelRepository;
    private readonly IPredictionRepository _predictionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MlServiceSettings _mlServiceSettings;

    public ForecastingService(IHttpClientFactory httpClientFactory, IModelRepository modelRepository, IPredictionRepository predictionRepository, IUnitOfWork unitOfWork, IOptions<MlServiceSettings> mlServiceSettings)
    {
        _httpClientFactory = httpClientFactory;
        _modelRepository = modelRepository;
        _predictionRepository = predictionRepository;
        _unitOfWork = unitOfWork;
        _mlServiceSettings = mlServiceSettings.Value;
    }

    public async Task<Result> ProcessForecastAsync(int modelId, int horizon, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: false);
        if (model == null)
        {
            return Result.Failure(ResultErrorType.NotFound, ErrorMessages.ForecastNotFound);
        }

        if (model.Status != ModelStatus.Completed || string.IsNullOrEmpty(model.ModelFilePath))
        {
            return Result.Failure(ResultErrorType.BadRequest, ErrorMessages.ModelNotCompleted);
        }

        var requestBody = new { model_path = model.ModelFilePath, horizon };
        var httpClient = _httpClientFactory.CreateClient();
        var stringContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await httpClient.PostAsync($"{_mlServiceSettings.BaseUrl}/predict/{model.Algorithm!.ToLower()}", stringContent, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result.Failure(ResultErrorType.InternalServerError, ErrorMessages.ForecastGenerationFailed);
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            return Result.Failure(ResultErrorType.InternalServerError, ErrorMessages.ForecastGenerationFailed);
        }

        List<Prediction> newPredictions;
        try
        {
            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            var predictResult = JsonSerializer.Deserialize<PythonPredictResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower // Python "yhat_lower"/"yhat_upper" -> C# eşlemesi için
            });

            if (predictResult?.Predictions == null)
            {
                return Result.Failure(ResultErrorType.InternalServerError, ErrorMessages.InvalidForecastResponse);
            }

            var createdAt = DateTime.UtcNow;
            newPredictions = predictResult.Predictions.Select(p => new Prediction
            {
                ModelId = modelId,
                PredictionDate = DateTime.SpecifyKind(DateTime.Parse(p.Ds), DateTimeKind.Utc),
                PredictedValue = (decimal)p.Yhat,
                ConfidenceLower = (decimal)p.YhatLower,
                ConfidenceUpper = (decimal)p.YhatUpper,
                ActualValue = null, // gelecek tarihli tahmin, henüz gerçekleşmedi
                IsAnomaly = false,
                CreatedAt = createdAt
            }).ToList();
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return Result.Failure(ResultErrorType.InternalServerError, ErrorMessages.InvalidForecastResponse);
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _predictionRepository.RemovePredictionsForModelAsync(modelId);
            await _predictionRepository.CreatePredictionsBulkAsync(newPredictions);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
        return Result.Success();
    }

    private class PythonPredictResponse
    {
        public List<PythonPredictionPoint>? Predictions { get; set; }
    }

    private class PythonPredictionPoint
    {
        public string Ds { get; set; } = string.Empty;
        public double Yhat { get; set; }
        public double YhatLower { get; set; }
        public double YhatUpper { get; set; }
    }
}