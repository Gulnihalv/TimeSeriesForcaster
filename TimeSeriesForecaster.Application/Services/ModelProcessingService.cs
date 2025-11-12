using System.Text;
using System.Text.Json;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;

namespace TimeSeriesForecaster.Application.Services;

public class ModelProcessingService : IModelProcessingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataPointRepository _dataPointRepository;
    private readonly IModelRepository _modelRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ModelProcessingService(IHttpClientFactory httpClientFactory, IDataPointRepository dataPointRepository, IModelRepository modelRepository, IUnitOfWork unitOfWork)
    {
        _httpClientFactory = httpClientFactory;
        _dataPointRepository = dataPointRepository;
        _modelRepository = modelRepository;
        _unitOfWork = unitOfWork;
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
    
            var httpClient = _httpClientFactory.CreateClient();
            var stringContent = new StringContent(JsonSerializer.Serialize(trainingData), Encoding.UTF8, "application/json");
            var httpResponse = await httpClient.PostAsync($"http://localhost:8000/train/{model.Algorithm!.ToLower()}", stringContent);
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception("Model eğitimi sırasında Python API'ında bir hata oluştu.");
            }

            var responseBody = await httpResponse.Content.ReadAsStringAsync();
            var trainingResult = JsonSerializer.Deserialize<PythonTrainingResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (trainingResult?.ModelPath == null)
            {
                throw new Exception("Python API'ından geçerli bir model yolu dönmedi.");
            }

            var modelPath = trainingResult?.ModelPath;
            model.TrainingCompletedAt = DateTime.UtcNow;
            model.Status = ModelStatus.Completed;
            model.ModelFilePath = modelPath;
            model.ErrorMessage = null;

        }catch (Exception ex)
        {
            model.Status = ModelStatus.Failed;
            model.ErrorMessage = ex.Message;
        }
        finally
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
    }

    private class PythonTrainingResponse
    {
        public string? ModelPath { get; set; }
    }
}
