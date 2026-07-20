using System.Text;
using System.Text.Json;
using AutoMapper;
using Hangfire;
using Microsoft.Extensions.Options;
using TimeSeriesForecaster.Application.Configuration;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

public class ModelService : IModelService
{
    private readonly IModelRepository _modelRepository;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IDataPointRepository _dataPointRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MlServiceSettings _mlServiceSettings;
 
    public ModelService(IModelRepository modelRepository, IDatasetRepository datasetRepository, IDataPointRepository dataPointRepository, IUnitOfWork unitOfWork, IMapper mapper, IBackgroundJobClient backgroundJobClient, IHttpClientFactory httpClientFactory, IOptions<MlServiceSettings> mlServiceSettings)
    {
        _modelRepository = modelRepository;
        _datasetRepository = datasetRepository;
        _dataPointRepository = dataPointRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _backgroundJobClient = backgroundJobClient;
        _httpClientFactory = httpClientFactory;
        _mlServiceSettings = mlServiceSettings.Value;
    }

    public async Task<IEnumerable<ModelDto>> GetAllModelsForDatasetAsync(int datasetId, int userId)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return new List<ModelDto>(); // boşliste
        }

        var models = await _modelRepository.GetModelsForDatasetAsync(datasetId: datasetId, trackChanges: false);
        var modelDto = _mapper.Map<IEnumerable<ModelDto>>(models);
        return modelDto;
    }

    public async Task<ModelDto?> GetModelByIdAsync(int modelId, int userId)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return null;
        }

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: false);
        if (model == null)
        {
            return null;
        }
        var modelDto = _mapper.Map<ModelDto>(model);
        return modelDto;
    }

    public async Task<ModelDto?> TrainModelAsync(int datasetId, int userId, string algorithm, ProphetHyperparametersDto? hyperparameters = null)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            throw new UnauthorizedAccessException("Bu veri seti üzerinde işlem yapılamaz.");
        }

        var dataset = await _datasetRepository.GetDatasetByIdAsync(id: datasetId, trackChanges: false);
        var modelEntity = new Model
        {
            ProjectId = dataset!.ProjectId,
            DatasetId = datasetId,
            ModelName = $"{algorithm} Model - {DateTime.UtcNow:d}",
            Algorithm = algorithm,
            // Hiperparametreleri JSON string olarak saklıyoruz - hem eğitim sırasında Python'a
            // iletmek hem de ileride model karşılaştırma ekranında "hangi ayarla eğitildi" diye
            // göstermek için. Hiçbiri gönderilmezse null kalır, Prophet kendi varsayılanlarını kullanır.
            Hyperparameters = hyperparameters == null
                ? null
                : JsonSerializer.Serialize(hyperparameters, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            ModelFilePath = null, // burası pythondan gelince doldurulcak
            Status = ModelStatus.Queued,
            TrainingStartedAt = null,
            TrainingCompletedAt = null,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _modelRepository.CreateModel(modelEntity);
        await _unitOfWork.SaveChangesAsync();

        string jobId;
        try
        {
            jobId = _backgroundJobClient.Enqueue<IModelProcessingService>(service =>
                service.ProcessModelAsync(modelEntity.Id, CancellationToken.None));
        }
        catch (Exception ex)
        {
            modelEntity.Status = ModelStatus.Failed;
            modelEntity.ErrorMessage = $"Model Enqueue'da hata oluştu: {ex.Message}";
            await _unitOfWork.SaveChangesAsync();
            return null;
        }

        modelEntity.HangfireJobId = jobId;
        await _unitOfWork.SaveChangesAsync();

        var modelDto = _mapper.Map<ModelDto>(modelEntity);
        return modelDto;
    }

    public async Task<ModelDetailDto?> GetModelDetailByIdAsync(int modelId, int userId)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return null;
        }

        var model = await _modelRepository.GetModelWithMetricsAsync(id: modelId, trackChanges: false);
        if (model == null)
        {
            return null;
        }

        var modelDetailDto = _mapper.Map<ModelDetailDto>(model);
        return modelDetailDto;
    }

    public async Task<bool> GenerateForecastAsync(int modelId, int userId, int horizon)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            throw new UnauthorizedAccessException("Bu model üzerinde işlem yapılamaz.");
        }

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: false);
        if (model == null || model.Status != ModelStatus.Completed)
        {
            throw new InvalidOperationException("Tahmin üretebilmek için modelin eğitiminin tamamlanmış olması gerekir.");
        }

        _backgroundJobClient.Enqueue<IForecastingService>(service =>
            service.ProcessForecastAsync(modelId, horizon, CancellationToken.None));

        return true;
    }

    public async Task<bool> DeleteModelAsync(int modelId, int userId)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return false;
        }

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: true);
        if (model == null)
        {
            return false;
        }

        model.IsActive = false;
        _modelRepository.UpdateModel(model);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<ModelComponentsDto?> GetModelComponentsAsync(int modelId, int userId)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return null;
        }
 
        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: false);
        if (model == null)
        {
            return null;
        }
 
        if (model.Status != ModelStatus.Completed || string.IsNullOrEmpty(model.ModelFilePath))
        {
            throw new InvalidOperationException("Bileşenleri görebilmek için modelin eğitiminin tamamlanmış olması gerekir.");
        }
 
        var requestBody = new { model_path = model.ModelFilePath };
        var httpClient = _httpClientFactory.CreateClient();
        var stringContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
 
        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await httpClient.PostAsync($"{_mlServiceSettings.BaseUrl}/components/{model.Algorithm!.ToLower()}", stringContent);
        }
        catch (HttpRequestException ex)
        {
            // ML servisine hiç ulaşılamadı (ayakta değil, ağ sorunu vs.)
            throw new Exception($"ML servisine ulaşılamadı: {ex.Message}");
        }
 
        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new Exception("Model bileşenleri alınırken Python API'ında bir hata oluştu.");
        }
 
        var responseBody = await httpResponse.Content.ReadAsStringAsync();
        var componentsResult = JsonSerializer.Deserialize<ModelComponentsDto>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
 
        return componentsResult;
    }

}