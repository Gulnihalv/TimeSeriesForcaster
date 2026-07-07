using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
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

    public async Task ProcessForecastAsync(int modelId, int horizon, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: false);
        if (model == null)
        {
            throw new Exception($"Tahmin üretilecek model bulunamadı: {modelId}");
        }

        if (model.Status != ModelStatus.Completed || string.IsNullOrEmpty(model.ModelFilePath))
        {
            throw new Exception("Tahmin üretebilmek için modelin eğitiminin tamamlanmış olması gerekir.");
        }

        var requestBody = new { model_path = model.ModelFilePath, horizon };
        var httpClient = _httpClientFactory.CreateClient();
        var stringContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var httpResponse = await httpClient.PostAsync($"{_mlServiceSettings.BaseUrl}/predict/{model.Algorithm!.ToLower()}", stringContent);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new Exception("Tahmin üretimi sırasında Python API'ında bir hata oluştu.");
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync();
        var predictResult = JsonSerializer.Deserialize<PythonPredictResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (predictResult?.Predictions == null)
        {
            throw new Exception("Python API'ından geçerli bir tahmin listesi dönmedi.");
        }

        // Bu model için önceki tahminleri temizle - yeni forecast, eskisinin yerini alır
        // (aynı modelden farklı horizon'larla tekrar tekrar tahmin alınabilir).
        await _predictionRepository.RemovePredictionsForModelAsync(modelId);

        var createdAt = DateTime.UtcNow;
        var newPredictions = predictResult.Predictions.Select(p => new Prediction
        {
            ModelId = modelId,
            PredictionDate = DateTime.Parse(p.Ds),
            PredictedValue = (decimal)p.Yhat,
            ConfidenceLower = (decimal)p.YhatLower,
            ConfidenceUpper = (decimal)p.YhatUpper,
            ActualValue = null, // gelecek tarihli tahmin, henüz gerçekleşmedi
            IsAnomaly = false,
            CreatedAt = createdAt
        }).ToList();

        await _predictionRepository.CreatePredictionsBulkAsync(newPredictions);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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