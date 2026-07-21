using System.Text;
using System.Text.Json;
using AutoMapper;
using Hangfire;
using Microsoft.Extensions.Options;
using TimeSeriesForecaster.Application.Common;
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

    public async Task<Result<IEnumerable<ModelDto>>> GetAllModelsForDatasetAsync(int datasetId, int userId)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return Result.Failure<IEnumerable<ModelDto>>(ResultErrorType.Forbidden, "Bu veri seti üzerinde işlem yapılamaz.");
        }

        var models = await _modelRepository.GetModelsForDatasetAsync(datasetId: datasetId, trackChanges: false);
        var modelDto = _mapper.Map<IEnumerable<ModelDto>>(models);
        return Result.Success(modelDto);
    }

    public async Task<Result<ModelDto?>> GetModelByIdAsync(int modelId, int userId)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return null;
        }

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: false);
        if (model == null)
        {
            return Result.Failure<ModelDto?>(ResultErrorType.NotFound, "Model bulunamadı.");
        }
        var modelDto = _mapper.Map<ModelDto>(model);
        return Result.Success(modelDto);
    }

    public async Task<Result<ModelDto?>> TrainModelAsync(int datasetId, int userId, string algorithm, ProphetHyperparametersDto? hyperparameters = null)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return Result.Failure<ModelDto?>(ResultErrorType.Forbidden, "Bu veri seti üzerinde işlem yapılamaz.");
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
            return Result.Failure<ModelDto?>(ResultErrorType.Unexpected, modelEntity.ErrorMessage);
        }

        modelEntity.HangfireJobId = jobId;
        await _unitOfWork.SaveChangesAsync();

        var modelDto = _mapper.Map<ModelDto>(modelEntity);
        return Result.Success<ModelDto?>(modelDto);
    }

    public async Task<Result<ModelDetailDto?>> GetModelDetailByIdAsync(int modelId, int userId)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return Result.Failure<ModelDetailDto?>(ResultErrorType.Forbidden, "Bu model üzerinde işlem yapılamaz.");
        }

        var model = await _modelRepository.GetModelWithMetricsAsync(id: modelId, trackChanges: false);
        if (model == null)
        {
            return Result.Failure<ModelDetailDto?>(ResultErrorType.NotFound, "Model bulunamadı.");
        }

        var modelDetailDto = _mapper.Map<ModelDetailDto>(model);
        return Result.Success<ModelDetailDto?>(modelDetailDto);
    }

    public async Task<Result> GenerateForecastAsync(int modelId, int userId, int horizon)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return Result.Failure(ResultErrorType.Forbidden, "Bu model üzerinde işlem yapılamaz.");
        }

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: false);
        if (model == null || model.Status != ModelStatus.Completed)
        {
            return Result.Failure(ResultErrorType.ValidationError, "Tahmin üretebilmek için modelin eğitiminin tamamlanmış olması gerekir.");
        }

        _backgroundJobClient.Enqueue<IForecastingService>(service =>
            service.ProcessForecastAsync(modelId, horizon, CancellationToken.None));

        return Result.Success();
    }

    public async Task<Result> DeleteModelAsync(int modelId, int userId)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return Result.Failure(ResultErrorType.Forbidden, "Bu model üzerinde işlem yapılamaz.");
        }

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: true);
        if (model == null)
        {
            return Result.Failure(ResultErrorType.NotFound, "Model bulunamadı.");
        }

        model.IsActive = false;
        _modelRepository.UpdateModel(model);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<ModelComponentsDto?>> GetModelComponentsAsync(int modelId, int userId)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return Result.Failure<ModelComponentsDto?>(ResultErrorType.Forbidden, "Bu model üzerinde işlem yapılamaz.");
        }
 
        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: false);
        if (model == null)
        {
            return Result.Failure<ModelComponentsDto?>(ResultErrorType.NotFound, "Model bulunamadı.");
        }
 
        if (model.Status != ModelStatus.Completed || string.IsNullOrEmpty(model.ModelFilePath))
        {
            return Result.Failure<ModelComponentsDto?>(ResultErrorType.ValidationError, "Bileşenleri görebilmek için modelin eğitiminin tamamlanmış olması gerekir.");
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
            return Result.Failure<ModelComponentsDto?>(ResultErrorType.Unexpected, $"ML servisine ulaşılamadı: {ex.Message}");
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            return Result.Failure<ModelComponentsDto?>(ResultErrorType.Unexpected, "Model bileşenleri alınırken Python API'ında bir hata oluştu.");
        }
 
        var responseBody = await httpResponse.Content.ReadAsStringAsync();
        var componentsResult = JsonSerializer.Deserialize<ModelComponentsDto>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
 
        return Result.Success<ModelComponentsDto?>(componentsResult);
    }

}