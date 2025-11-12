using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Hangfire;
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

    public async Task<ModelDto?> TrainModelAsync(int datasetId, int userId, string algorithm)
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

}
