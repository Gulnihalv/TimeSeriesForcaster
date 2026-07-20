using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
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
    private readonly MlServiceSettings _mlServiceSettings;

    public ModelProcessingService(IHttpClientFactory httpClientFactory, IDataPointRepository dataPointRepository, IModelRepository modelRepository, IModelMetricRepository modelMetricRepository, IProjectRepository projectRepository, INotificationService notificationService, IUnitOfWork unitOfWork, IOptions<MlServiceSettings> mlServiceSettings)
    {
        _httpClientFactory = httpClientFactory;
        _dataPointRepository = dataPointRepository;
        _modelRepository = modelRepository;
        _modelMetricRepository = modelMetricRepository;
        _projectRepository = projectRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _mlServiceSettings = mlServiceSettings.Value;
    }

    public async Task ProcessModelAsync(int modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: true);
        if (model == null) return;

        model.Status = ModelStatus.Training;
        model.TrainingStartedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

            var httpClient = _httpClientFactory.CreateClient();
            var stringContent = new StringContent(
                JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }),
                Encoding.UTF8, "application/json");
            var httpResponse = await httpClient.PostAsync($"{_mlServiceSettings.BaseUrl}/train/{model.Algorithm!.ToLower()}", stringContent);
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception("Model eğitimi sırasında Python API'ında bir hata oluştu.");
            }

            var responseBody = await httpResponse.Content.ReadAsStringAsync();
            var trainingResult = JsonSerializer.Deserialize<PythonTrainingResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (trainingResult?.ModelPath == null)
            {
                throw new Exception("Python API'ından geçerli bir model yolu dönmedi.");
            }

            var modelPath = trainingResult?.ModelPath;
            model.TrainingCompletedAt = DateTime.UtcNow;
            model.Status = ModelStatus.Completed;
            model.ModelFilePath = modelPath;
            model.ErrorMessage = null;

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
        catch (Exception ex)
        {
            model.Status = ModelStatus.Failed;
            model.ErrorMessage = ex.Message;
            await NotifyModelResultAsync(model, success: false);
        }
        finally
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task NotifyModelResultAsync(Model model, bool success)
    {
        var project = await _projectRepository.GetProjectByIdAsync(id: model.ProjectId, trackChanges: false);
        if (project == null) return;

        var type = success ? NotificationType.ModelTrainingCompleted : NotificationType.ModelTrainingFailed;
        var message = success
            ? $"\"{model.ModelName}\" modelinin eğitimi tamamlandı."
            : $"\"{model.ModelName}\" modelinin eğitimi başarısız oldu: {model.ErrorMessage}";

        await _notificationService.CreateNotificationAsync(project.UserId, type, message, "Model", model.Id);
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