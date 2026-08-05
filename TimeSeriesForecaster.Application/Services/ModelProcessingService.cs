using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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

    public ModelProcessingService(IHttpClientFactory httpClientFactory, IDataPointRepository dataPointRepository, IModelRepository modelRepository, IModelMetricRepository modelMetricRepository, IProjectRepository projectRepository, INotificationService notificationService, IUnitOfWork unitOfWork, IServiceScopeFactory serviceScopeFactory)
    {
        _httpClientFactory = httpClientFactory;
        _dataPointRepository = dataPointRepository;
        _modelRepository = modelRepository;
        _modelMetricRepository = modelMetricRepository;
        _projectRepository = projectRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result> ProcessModelAsync(int modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: true);
        if (model == null) return Result.Failure(ResultErrorType.NotFound, ErrorMessages.ModelNotFound);

        model.Status = ModelStatus.Training;
        model.TrainingStartedAt = DateTime.UtcNow;
        model.ProgressPercentage = 5;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Prophet'in fit() çağrısı tek, bloklayıcı ve ara ilerleme sinyali
        // vermeyen bir işlem (ml-service'te epoch/adım bazlı bir geri bildirim
        // yok). Bu yüzden burada gerçek bir ölçüm değil, zamana dayalı tahmini
        // bir ilerleme simülasyonu çalıştırıyoruz - HTTP çağrısı boyunca
        // yüzdeyi kademeli olarak artırıp çağrı bitince 100'e sabitliyoruz.
        using var progressCts = new CancellationTokenSource();
        var progressTask = SimulateTrainingProgressAsync(model.Id, progressCts.Token);

        try
        {
            var dataPoints = await _dataPointRepository.GetDataPointsAsync(datasetId: model.DatasetId);
            if (dataPoints == null || !dataPoints.Any())
            {
                throw new Exception("Model eğitimi için veri noktaları bulunamadı.");
            }
    
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

            await NotifyModelResultAsync(model, success: true);
        }
        catch (OperationCanceledException)
        {
            model.Status = ModelStatus.Cancelled;
            model.ErrorMessage = "Model eğitimi iptal edildi.";
            await NotifyModelResultAsync(model, success: false);
            throw;
        }
        catch (Exception ex)
        {
            model.Status = ModelStatus.Failed;
            model.ErrorMessage = ex.Message;

            model.ProgressPercentage = 100;
            await NotifyModelResultAsync(model, success: false);
            return Result.Failure(ResultErrorType.Unexpected, model.ErrorMessage);
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
                // Beklenen durum: ana iş bitince ilerleme simülasyonunu bilerek iptal ediyoruz.
                // Bu bir hata değil, o yüzden loglamıyoruz.
            }
            catch (Exception ex)
            {
                // TODO Loglama kurulcak
            }
            await _unitOfWork.SaveChangesAsync(CancellationToken.None); 
        }

        return Result.Success();
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