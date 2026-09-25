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

                List<(DateTime Date, decimal Value, decimal Lower, decimal Upper)> forecastPoints;
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

                    forecastPoints = predictResult.Predictions.Select(p => (
                        Date: DateTime.SpecifyKind(DateTime.Parse(p.Ds), DateTimeKind.Utc),
                        Value: (decimal)p.Yhat,
                        Lower: (decimal)p.YhatLower,
                        Upper: (decimal)p.YhatUpper)).ToList();
                }
                catch (Exception ex) when (ex is JsonException or FormatException)
                {
                    throw new Exception(ErrorMessages.InvalidForecastResponse, ex);
                }

                var createdAt = DateTime.UtcNow;

                var predictionCount = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Retry'da blok baştan çalışır; entity'ler her denemede taze üretilmeli,
                    // aksi halde önceki denemeden kalan takip durumu/Id'lerle çakışırlar.
                    var newPredictions = forecastPoints.Select(p => new Prediction
                    {
                        ModelId = modelId,
                        PredictionDate = p.Date,
                        PredictedValue = p.Value,
                        ConfidenceLower = p.Lower,
                        ConfidenceUpper = p.Upper,
                        ActualValue = null, // gelecek tarihli tahmin, henüz gerçekleşmedi
                        IsAnomaly = false,
                        CreatedAt = createdAt
                    }).ToList();

                    await _predictionRepository.RemovePredictionsForModelAsync(modelId);
                    await _predictionRepository.CreatePredictionsBulkAsync(newPredictions);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return newPredictions.Count;
                }, cancellationToken);

                model.ForecastStatus = ForecastStatus.Completed;
                model.ForecastCompletedAt = DateTime.UtcNow;
                model.ForecastProgressPercentage = 100;
                model.ForecastErrorMessage = null;

                // Simülasyon durdurulmadan kaydedersek yüzdeyi 100'ün altına geri yazabilir.
                await StopProgressSimulationAsync(progressCts, progressTask, modelId);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);

                _logger.LogInformation("Forecast generation completed successfully. Total predictions: {PredictionCount}", predictionCount);
                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                await StopProgressSimulationAsync(progressCts, progressTask, modelId);
                await PersistTerminalStateAsync(modelId, ForecastStatus.Cancelled, "Tahmin oluşturma iptal edildi.", progressPercentage: null);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forecast generation failed.");
                await StopProgressSimulationAsync(progressCts, progressTask, modelId);
                await PersistTerminalStateAsync(modelId, ForecastStatus.Failed, ex.Message, progressPercentage: 100);
                return Result.Failure(ResultErrorType.InternalServerError, ex.Message);
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Forecast generation completed in {ElapsedMilliseconds} ms.", sw.ElapsedMilliseconds);
            }
        }
    }

    // Başarısız bir SaveChanges'ten context'te 'Added' olarak kalan entity'ler sonraki kayıtla
    // transaction dışında yazılmasın diye önce tracker temizlenir. Bu, takipteki model nesnesini de
    // düşürdüğünden, terminal durum yeniden yüklenen modele yazılır.
    private async Task PersistTerminalStateAsync(int modelId, ForecastStatus status, string errorMessage, int? progressPercentage)
    {
        _unitOfWork.DiscardPendingChanges();

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: true);
        if (model == null)
        {
            _logger.LogWarning("Model not found while persisting forecast state {Status} for ModelId: {ModelId}", status, modelId);
            return;
        }

        model.ForecastStatus = status;
        model.ForecastErrorMessage = errorMessage;
        model.ForecastCompletedAt = DateTime.UtcNow;
        if (progressPercentage.HasValue)
        {
            model.ForecastProgressPercentage = progressPercentage.Value;
        }

        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private async Task StopProgressSimulationAsync(CancellationTokenSource progressCts, Task progressTask, int modelId)
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