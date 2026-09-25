using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Configuration;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

public class ModelProcessingService : IModelProcessingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataPointRepository _dataPointRepository;
    private readonly IModelRepository _modelRepository;
    private readonly IModelMetricRepository _modelMetricRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ModelProcessingService> _logger;

    public ModelProcessingService(IHttpClientFactory httpClientFactory, IDataPointRepository dataPointRepository, IModelRepository modelRepository, IModelMetricRepository modelMetricRepository, IProjectRepository projectRepository, INotificationService notificationService, IUnitOfWork unitOfWork, IServiceScopeFactory serviceScopeFactory, ILogger<ModelProcessingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _dataPointRepository = dataPointRepository;
        _modelRepository = modelRepository;
        _modelMetricRepository = modelMetricRepository;
        _projectRepository = projectRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task<Result> ProcessModelAsync(int modelId, TimeResolution resolution, AggregationFunction aggregation, CancellationToken cancellationToken = default)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["ModelId"] = modelId }))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Model processing started.");

            var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: true);
            if (model == null) return Result.Failure(ResultErrorType.NotFound, ErrorMessages.ModelNotFound);

            model.Status = ModelStatus.Training;
            model.TrainingStartedAt = DateTime.UtcNow;
            model.ProgressPercentage = 5;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            using var progressCts = new CancellationTokenSource();
            var progressTask = SimulateTrainingProgressAsync(model.Id, progressCts.Token);

            try
            {
                var dataPoints = Enumerable.Empty<dynamic>(); //inşallah çalışır
                if (resolution == TimeResolution.Raw)
                {
                    dataPoints = await _dataPointRepository.GetDataPointsAsync(datasetId: model.DatasetId);
                }
                else
                {
                    dataPoints = await _dataPointRepository.GetAggregatedDataPointsAsync(model.DatasetId, resolution, aggregation, cancellationToken);
                }
                
                if (dataPoints == null || !dataPoints.Any())
                {
                    _logger.LogWarning("No data points found for DatasetId: {DatasetId}. Model training cannot proceed.", model.DatasetId);
                    throw new Exception("Model eğitimi için veri noktaları bulunamadı.");
                }

                _logger.LogInformation("Retrieved {Count} aggregated data points for DatasetId: {DatasetId}.", dataPoints.Count(), model.DatasetId);

                var trainingData = dataPoints.Select(dp => new
                {
                    ds = dp.Timestamp.ToString("o"), // Prophet'in anlaması için düzenleme
                    y = dp.Value
                }).ToList();

                ProphetHyperparametersDto? hyperparameters = string.IsNullOrEmpty(model.Hyperparameters)
                    ? null
                    : JsonSerializer.Deserialize<ProphetHyperparametersDto>(model.Hyperparameters, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var requestPayload = new
                {
                    data = trainingData,
                    hyperparameters
                };

                var httpClient = _httpClientFactory.CreateClient(MlServiceClients.MlServiceLongRunning);
                var stringContent = new StringContent(
                    JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }),
                    Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse;
                try
                {
                    httpResponse = await httpClient.PostAsync($"train/{model.Algorithm!.ToLower()}", stringContent, cancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
                {
                    throw new Exception(ErrorMessages.ModelTrainingAPIError, ex);
                }

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new Exception(ErrorMessages.ModelTrainingAPIError);
                }

                var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                var trainingResult = JsonSerializer.Deserialize<PythonTrainingResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (trainingResult?.ModelPath == null)
                {
                    throw new Exception(ErrorMessages.InvalidModelPathResponse);
                }

                var modelPath = trainingResult?.ModelPath;
                model.TrainingCompletedAt = DateTime.UtcNow;
                model.Status = ModelStatus.Completed;
                model.TrainingRowCount = dataPoints.Count();
                model.ModelFilePath = modelPath;
                model.ErrorMessage = null;
                model.ProgressPercentage = 100;

                if (trainingResult?.Metrics != null)
                {
                    var calculatedAt = DateTime.UtcNow;
                    var metricEntities = new List<ModelMetric>
                    {
                        new ModelMetric
                        {
                            ModelId = model.Id,
                            MetricName = MetricName.MAE,
                            MetricValue = (decimal)trainingResult.Metrics.Mae,
                            CalculatedAt = calculatedAt
                        },
                        new ModelMetric
                        {
                            ModelId = model.Id,
                            MetricName = MetricName.RMSE,
                            MetricValue = (decimal)trainingResult.Metrics.Rmse,
                            CalculatedAt = calculatedAt
                        }
                    };

                    await _modelMetricRepository.CreateMetricsAsync(metricEntities);
                }

                // Simülasyon durdurulmadan kaydedersek yüzdeyi 100'ün altına geri yazabilir.
                await StopProgressSimulationAsync(progressCts, progressTask, modelId);

                // Model alanları ve metrikler aynı context'te; tek SaveChanges ile atomik yazılırlar.
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);

                await TryNotifyModelResultAsync(model, success: true);

                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                await StopProgressSimulationAsync(progressCts, progressTask, modelId);
                // Bildirim bilerek gönderilmiyor: graceful shutdown'da iptal edilen job açılışta yeniden
                // çalışır; kullanıcıya önce "başarısız", sonra "başarılı" demek kafa karıştırır.
                await PersistTerminalStateAsync(modelId, ModelStatus.Cancelled, "Model eğitimi iptal edildi.", progressPercentage: null);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model training failed.");
                await StopProgressSimulationAsync(progressCts, progressTask, modelId);
                var failedModel = await PersistTerminalStateAsync(modelId, ModelStatus.Failed, ex.Message, progressPercentage: 100);
                if (failedModel != null) await TryNotifyModelResultAsync(failedModel, success: false);
                return Result.Failure(ResultErrorType.Unexpected, ex.Message);
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Model processing completed in {ElapsedMilliseconds} ms.", sw.ElapsedMilliseconds);
            }
        }
    }

    // Başarısız bir SaveChanges'ten context'te 'Added' olarak kalan entity'ler (ör. metrikler) sonraki
    // kayıtla yazılmasın diye önce tracker temizlenir. Bu, takipteki model nesnesini de düşürdüğünden,
    // terminal durum yeniden yüklenen modele yazılır.
    private async Task<Model?> PersistTerminalStateAsync(int modelId, ModelStatus status, string errorMessage, int? progressPercentage)
    {
        _unitOfWork.DiscardPendingChanges();

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: true);
        if (model == null)
        {
            _logger.LogWarning("Model not found while persisting training state {Status} for ModelId: {ModelId}", status, modelId);
            return null;
        }

        model.Status = status;
        model.ErrorMessage = errorMessage;
        if (progressPercentage.HasValue)
        {
            model.ProgressPercentage = progressPercentage.Value;
        }

        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        return model;
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
            _logger.LogWarning(ex, "Model ilerleme simülasyonu beklenmedik şekilde sonlandı. ModelId: {ModelId}", modelId);
        }
    }

    // Bildirim best-effort: durum zaten kaydedildikten sonra bildirim hatası, eğitimin sonucunu değiştirmemeli.
    private async Task TryNotifyModelResultAsync(Model model, bool success)
    {
        try
        {
            var notifyResult = await NotifyModelResultAsync(model, success);
            if (!notifyResult.IsSuccess)
            {
                _logger.LogWarning("Bildirim gönderilemedi: {Error}", notifyResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Model sonuç bildirimi oluşturulamadı. ModelId: {ModelId}", model.Id);
        }
    }

    private async Task SimulateTrainingProgressAsync(int modelId, CancellationToken ct)
    {
        // Heuristic/tahmini ilerleme: gerçek eğitim ilerlemesinin bir ölçümü
        // değildir, sadece kullanıcıya görsel geri bildirim vermek içindir.
        const int startPct = 5;
        const int capPct = 90;
        const int estimatedDurationSeconds = 20;
        const int ticks = 15;
        var delayPerTick = TimeSpan.FromSeconds(estimatedDurationSeconds / (double)ticks);

        try
        {
            for (var i = 1; i <= ticks; i++)
            {
                await Task.Delay(delayPerTick, ct);
                var pct = startPct + (int)((capPct - startPct) * (i / (double)ticks));

                // Ana akışın izlediği DbContext ile eşzamanlı yazma yapmamak için
                // ayrı bir DI scope üzerinden kendi repository örneğimizi çözüyoruz.
                using var scope = _serviceScopeFactory.CreateScope();
                var modelRepository = scope.ServiceProvider.GetRequiredService<IModelRepository>();
                await modelRepository.UpdateProgressPercentageAsync(modelId, pct);
            }
        }
        catch (OperationCanceledException)
        {
            // beklenen durum: HTTP çağrısı bitince simülasyon iptal edilir
        }
    }

    private async Task<Result> NotifyModelResultAsync(Model model, bool success)
    {
        var project = await _projectRepository.GetProjectByIdAsync(id: model.ProjectId, trackChanges: false);
        if (project == null) return Result.Failure(ResultErrorType.NotFound, ErrorMessages.ProjectNotFound);

        var type = success ? NotificationType.ModelTrainingCompleted : NotificationType.ModelTrainingFailed;
        var message = success
            ? $"\"{model.ModelName}\" modelinin eğitimi tamamlandı."
            : $"\"{model.ModelName}\" modelinin eğitimi başarısız oldu: {model.ErrorMessage}";

        return await _notificationService.CreateNotificationAsync(project.UserId, type, message, "Model", model.Id);
    }

    private class PythonTrainingResponse
    {
        public string? ModelPath { get; set; }
        public PythonMetrics? Metrics { get; set; }
    }

    private class PythonMetrics
    {
        public double Mae { get; set; }
        public double Rmse { get; set; }
    }
}