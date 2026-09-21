using System.Text;
using System.Text.Json;
using AutoMapper;
using Hangfire;
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

    public ModelService(IModelRepository modelRepository, IDatasetRepository datasetRepository, IDataPointRepository dataPointRepository, IUnitOfWork unitOfWork, IMapper mapper, IBackgroundJobClient backgroundJobClient, IHttpClientFactory httpClientFactory)
    {
        _modelRepository = modelRepository;
        _datasetRepository = datasetRepository;
        _dataPointRepository = dataPointRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _backgroundJobClient = backgroundJobClient;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<IEnumerable<ModelDto>>> GetAllModelsForDatasetAsync(int datasetId, int userId)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return Result.Failure<IEnumerable<ModelDto>>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
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
            return Result.Failure<ModelDto?>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: false);
        if (model == null)
        {
            return Result.Failure<ModelDto?>(ResultErrorType.NotFound, "Model bulunamadı.");
        }
        var modelDto = _mapper.Map<ModelDto>(model);
        return Result.Success<ModelDto?>(modelDto);
    }

    public async Task<Result<ModelDto?>> TrainModelAsync(int datasetId, int userId, string algorithm, ProphetHyperparametersDto? hyperparameters = null, TimeResolution? timeResolution = null, AggregationFunction? aggregationFunction = null)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return Result.Failure<ModelDto?>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        var dataset = await _datasetRepository.GetDatasetByIdAsync(id: datasetId, trackChanges: false);
        if (dataset == null)
        {
            return Result.Failure<ModelDto?>(ResultErrorType.NotFound, ErrorMessages.DatasetNotFound);
        }

        // Çözünürlük gönderilmediyse ham veriyle eğitim demektir. Bu durum da doğrulamadan geçmeli;
        // aksi halde API'yi doğrudan çağıran biri limitin çok üstündeki bir dataset'i ham haliyle eğitime sokabilir.
        var effectiveResolution = timeResolution ?? TimeResolution.Raw;

        // Ham veride agregasyon anlamsız. Agregasyonlu çözünürlükte fonksiyon gönderilmediyse UI'daki varsayılan
        // (ortalama) kullanılır; aksi halde None ile kuyruğa giren job, agregasyon sorgusunda patlardı.
        var effectiveAggregation = effectiveResolution == TimeResolution.Raw
            ? AggregationFunction.None
            : aggregationFunction ?? AggregationFunction.Average;

        var pointCount = await GetPointCountForResolutionAsync(dataset, effectiveResolution);
        if (!IsWithinAllowedRange(pointCount))
        {
            return Result.Failure<ModelDto?>(ResultErrorType.BadRequest,
                $"Seçilen çözünürlük ({effectiveResolution}) {pointCount:N0} nokta üretiyor; izin verilen aralık " +
                $"{ResolutionSettings.MinPointsForAllowed:N0}–{ResolutionSettings.MaxPointsForAllowed:N0}. Farklı bir zaman çözünürlüğü seçin.");
        }

        var modelEntity = new Model
        {
            ProjectId = dataset.ProjectId,
            DatasetId = datasetId,
            ModelName = $"{algorithm} Model - {DateTime.UtcNow:d}",
            Algorithm = algorithm,
            // Hiperparametreler camelCase JSON olarak saklanıyor: frontend'deki model karşılaştırma ekranı bu
            // anahtarlarla okuyor. Python'a gönderilirken ModelProcessingService tarafından snake_case'e çevriliyor.
            // Hiçbiri gönderilmezse null kalır, Prophet kendi varsayılanlarını kullanır.
            Hyperparameters = hyperparameters == null
                ? null
                : JsonSerializer.Serialize(hyperparameters, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            TrainingResolution = timeResolution,
            TrainingAggregation = effectiveResolution == TimeResolution.Raw ? null : effectiveAggregation,
            TrainingRowCount = null,
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
                service.ProcessModelAsync(modelEntity.Id, effectiveResolution, effectiveAggregation, CancellationToken.None));
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

    // Ham nokta sayısı dataset'te zaten saklı (RecordCount); pahalı COUNT(DISTINCT ...) sorgusunu sadece
    // agregasyonlu çözünürlüklerde çalıştırıyoruz.
    private async Task<int> GetPointCountForResolutionAsync(Dataset dataset, TimeResolution resolution, CancellationToken cancellationToken = default)
    {
        if (resolution == TimeResolution.Raw)
        {
            return dataset.RecordCount;
        }

        var counts = await _dataPointRepository.GetResolutionPointCountsAsync(dataset.Id, cancellationToken);
        return counts.GetValueOrDefault(resolution);
    }

    private static bool IsWithinAllowedRange(int points)
        => points >= ResolutionSettings.MinPointsForAllowed && points <= ResolutionSettings.MaxPointsForAllowed;

    public async Task<Result<ModelDetailDto?>> GetModelDetailByIdAsync(int modelId, int userId)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return Result.Failure<ModelDetailDto?>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
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
            return Result.Failure(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        var model = await _modelRepository.GetModelByIdAsync(id: modelId, trackChanges: true);
        if (model == null || model.Status != ModelStatus.Completed)
        {
            return Result.Failure(ResultErrorType.ValidationError, "Tahmin üretebilmek için modelin eğitiminin tamamlanmış olması gerekir.");
        }

        model.ForecastStatus = ForecastStatus.Queued;
        model.ForecastProgressPercentage = 0;
        model.ForecastErrorMessage = null;
        model.ForecastCompletedAt = null;
        await _unitOfWork.SaveChangesAsync();

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

    public async Task<Result<ModelComponentsDto?>> GetModelComponentsAsync(int modelId, int userId, CancellationToken cancellationToken = default)
    {
        var userOwnsModel = await _modelRepository.UserOwnsModelAsync(modelId: modelId, userId: userId);
        if (!userOwnsModel)
        {
            return Result.Failure<ModelComponentsDto?>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
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
        var httpClient = _httpClientFactory.CreateClient(MlServiceClients.MlServiceStandard);
        var stringContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await httpClient.PostAsync($"components/{model.Algorithm!.ToLower()}", stringContent, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // ML servisine hiç ulaşılamadı (ayakta değil, ağ sorunu, timeout vs.)
            return Result.Failure<ModelComponentsDto?>(ResultErrorType.Unexpected, $"ML servisine ulaşılamadı: {ex.Message}");
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            return Result.Failure<ModelComponentsDto?>(ResultErrorType.Unexpected, "Model bileşenleri alınırken Python API'ında bir hata oluştu.");
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var componentsResult = JsonSerializer.Deserialize<ModelComponentsDto>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        return Result.Success<ModelComponentsDto?>(componentsResult);
    }

    public async Task<Result<List<ResolutionOptionDto>>> GetResolutionOptionsAsync(int datasetId, int userId, CancellationToken cancellationToken = default)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return Result.Failure<List<ResolutionOptionDto>>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        var resolutionPointCounts = await _dataPointRepository.GetResolutionPointCountsAsync(datasetId: datasetId, cancellationToken);

        var resolutionOptions = new List<ResolutionOptionDto>();
        foreach (var resolution in Enum.GetValues<TimeResolution>())
        {
            // Sözlükte olmayan çözünürlük 0 nokta sayılır → izinsiz.
            var estimatedPoints = resolutionPointCounts.GetValueOrDefault(resolution);

            resolutionOptions.Add(new ResolutionOptionDto
            {
                Resolution = resolution,
                EstimatedPoints = estimatedPoints,
                IsAllowed = IsWithinAllowedRange(estimatedPoints),
                IsRecommended = false
            });
        }

        var closestOption = resolutionOptions
            .Where(o => o.IsAllowed)
            .MinBy(o => GetDistanceToRecommendedRange(o.EstimatedPoints));

        if (closestOption is not null)
        {
            closestOption.IsRecommended = true;
        }

        return Result<List<ResolutionOptionDto>>.Success(resolutionOptions);
    }

    private static int GetDistanceToRecommendedRange(int estimatedPoints)
    {
        if (estimatedPoints < ResolutionSettings.MinPointsForRecommended)
        {
            return ResolutionSettings.MinPointsForRecommended - estimatedPoints;
        }

        if (estimatedPoints > ResolutionSettings.MaxPointsForRecommended)
        {
            return estimatedPoints - ResolutionSettings.MaxPointsForRecommended;
        }

        return 0;
    }
}