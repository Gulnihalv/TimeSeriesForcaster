using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ForecastingService> _logger;

    public ForecastingService(IHttpClientFactory httpClientFactory, IModelRepository modelRepository, IPredictionRepository predictionRepository, IUnitOfWork unitOfWork, IServiceScopeFactory serviceScopeFactory, ILogger<ForecastingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _modelRepository = modelRepository;
        _predictionRepository = predictionRepository;
        _unitOfWork = unitOfWork;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task<Result> ProcessForecastAsync(int modelId, int horizon, CancellationToken cancellationToken = default)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["ModelId"] = modelId }))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Forecast generation started. Horizon: {Horizon}", horizon);

            var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: true);
            if (model == null)
            {
                _logger.LogWarning("Model not found for ModelId: {ModelId}", modelId);
                return Result.Failure(ResultErrorType.NotFound, ErrorMessages.ForecastNotFound);
            }

            if (model.Status != ModelStatus.Completed || string.IsNullOrEmpty(model.ModelFilePath))
            {
                _logger.LogWarning("Model is not completed or model file path is missing for ModelId: {ModelId}", modelId);
                return Result.Failure(ResultErrorType.BadRequest, ErrorMessages.ModelNotCompleted);
            }

            model.ForecastStatus = ForecastStatus.Generating;
            model.ForecastStartedAt = DateTime.UtcNow;
            model.ForecastProgressPercentage = 10;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Tahmin üretimi de (model eğitiminde olduğu gibi) tek, bloklayıcı bir
            // HTTP çağrısı - ml-service'ten ara ilerleme sinyali gelmiyor. Bu yüzden
            // burada da zamana dayalı tahmini bir ilerleme simülasyonu kullanıyoruz.
            // Tahmin üretimi eğitimden çok daha hızlı olduğundan süre daha kısa.
            using var progressCts = new CancellationTokenSource();
            var progressTask = SimulateForecastProgressAsync(model.Id, progressCts.Token);

            try
            {
                var requestBody = new { model_path = model.ModelFilePath, horizon };
                var httpClient = _httpClientFactory.CreateClient(MlServiceClients.MlServiceStandard);
                var stringContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse;
                try
                {
                    httpResponse = await httpClient.PostAsync($"predict/{model.Algorithm!.ToLower()}", stringContent, cancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
                {
                    throw new Exception(ErrorMessages.ForecastGenerationFailed, ex);
                }

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new Exception(ErrorMessages.ForecastGenerationFailed);
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
                        throw new Exception(ErrorMessages.InvalidForecastResponse);
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
                    throw new Exception(ErrorMessages.InvalidForecastResponse, ex);
                }

                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    await _predictionRepository.RemovePredictionsForModelAsync(modelId);
                    await _predictionRepository.CreatePredictionsBulkAsync(newPredictions);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return true;
                }, cancellationToken);

                model.ForecastStatus = ForecastStatus.Completed;
                model.ForecastCompletedAt = DateTime.UtcNow;
                model.ForecastProgressPercentage = 100;
                model.ForecastErrorMessage = null;

                _logger.LogInformation("Forecast generation completed successfully. Total predictions: {PredictionCount}", newPredictions.Count);
                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                model.ForecastStatus = ForecastStatus.Cancelled;
                model.ForecastErrorMessage = "Tahmin oluşturma iptal edildi.";
                model.ForecastCompletedAt = DateTime.UtcNow;
                throw;
            }
            catch (Exception ex)
            {
                model.ForecastStatus = ForecastStatus.Failed;
                model.ForecastErrorMessage = ex.Message;
                model.ForecastCompletedAt = DateTime.UtcNow;
                model.ForecastProgressPercentage = 100;
                _logger.LogError(ex, "Forecast generation failed.");
                return Result.Failure(ResultErrorType.InternalServerError, ex.Message);
            }
            finally
            {
                progressCts.Cancel();
                try
                {
                    await progressTask;
                }
                catch (OperationCanceledException)
                {
                    // Beklenen durum: ana iş bitince simülasyon iptal edilir
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Forecast ilerleme simülasyonu beklenmedik şekilde sonlandı. ModelId: {ModelId}", modelId);

                }

                await _unitOfWork.SaveChangesAsync(CancellationToken.None); 

                sw.Stop();
                _logger.LogInformation("Forecast generation completed in {ElapsedMilliseconds} ms.", sw.ElapsedMilliseconds);
            }
        }
    }

    private async Task SimulateForecastProgressAsync(int modelId, CancellationToken ct)
    {
        const int startPct = 10;
        const int capPct = 90;
        const int estimatedDurationSeconds = 8;
        const int ticks = 8;
        var delayPerTick = TimeSpan.FromSeconds(estimatedDurationSeconds / (double)ticks);

        try
        {
            for (var i = 1; i <= ticks; i++)
            {
                await Task.Delay(delayPerTick, ct);
                var pct = startPct + (int)((capPct - startPct) * (i / (double)ticks));

                using var scope = _serviceScopeFactory.CreateScope();
                var modelRepository = scope.ServiceProvider.GetRequiredService<IModelRepository>();
                await modelRepository.UpdateForecastProgressPercentageAsync(modelId, pct);
            }
        }
        catch (OperationCanceledException)
        {
            // beklenen durum: HTTP çağrısı bitince simülasyon iptal edilir
        }
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