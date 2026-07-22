using System.Globalization;
using AutoMapper;
using CsvHelper;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

public class DatasetService : IDatasetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IDataPointRepository _dataPointRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;

    public DatasetService(IUnitOfWork unitOfWork, IDatasetRepository datasetRepository, IProjectRepository projectRepository, IDataPointRepository dataPointRepository, IBackgroundJobClient backgroundJobClient, IMapper mapper, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _datasetRepository = datasetRepository;
        _projectRepository = projectRepository;
        _dataPointRepository = dataPointRepository;
        _backgroundJobClient = backgroundJobClient;
        _mapper = mapper;
        _env = env;
    }
    public async Task<Result<string>> SaveFileAsync(IFormFile file, string subDirectory)
    {
        if (file.Length == 0 || file == null)
        {
            return Result.Failure<string>(ResultErrorType.BadRequest, ErrorMessages.FileCannotBeEmpty);
        }
        var _uploadsDirectoryName = "uploads";

        var contentRootPath = _env.ContentRootPath;
        var relativeUploadDir = Path.Combine(_uploadsDirectoryName, subDirectory);    
        var absoluteUploadPath = Path.Combine(contentRootPath, relativeUploadDir);

        Directory.CreateDirectory(absoluteUploadPath);
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        var absoluteFilePath = Path.Combine(absoluteUploadPath, uniqueFileName); // dosyanın kaydedilceği tam yol
        await using (var stream = new FileStream(absoluteFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        var relativeFilePath = Path.Combine(relativeUploadDir, uniqueFileName)
                                       .Replace(Path.DirectorySeparatorChar, '/');
            
        return Result.Success(relativeFilePath);
    
    }

    // CSV'nin header satırını okuyup dateColumnName/targetColumnName'in gerçekten var olup olmadığını kontrol eder. Dosyayı diske kaydetmeden önce çağrılır.
    private async Task<Result> ValidateCsvColumnsAsync(IFormFile file, string dateColumnName, string targetColumnName)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        var missingColumns = new List<string>();
        if (!headers.Contains(dateColumnName, StringComparer.OrdinalIgnoreCase))
        {
            missingColumns.Add(dateColumnName);
        }
        if (!headers.Contains(targetColumnName, StringComparer.OrdinalIgnoreCase))
        {
            missingColumns.Add(targetColumnName);
        }

        if (missingColumns.Any())
        {
            return Result.Failure(ResultErrorType.BadRequest,
                $"CSV dosyasında şu sütun(lar) bulunamadı: {string.Join(", ", missingColumns)}. " +
                $"Dosyadaki mevcut sütunlar: {string.Join(", ", headers)}");
        }

        return Result.Success();
    }

    public async Task<Result<DatasetDto?>> CreateDatasetFromUploadAsync(int projectId, int userId, string name, IFormFile file, string dateColumnName, string targetColumnName)
    {
        var userOwnsProject = await _projectRepository.UserOwnsProjectAsync(projectId: projectId, userId: userId);
        if (!userOwnsProject)
        {
            return Result.Failure<DatasetDto?>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        // 1) Dosya tipi kontrolü - sadece .csv kabul ediyoruz
        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<DatasetDto?>(ResultErrorType.BadRequest, $"Sadece .csv uzantılı dosyalar yüklenebilir. Yüklenen dosya: '{file.FileName}'");
        }

        // 2) Kolon adı kontrolü - dosyayı diske kaydetmeden ÖNCE header'ı okuyup doğruluyoruz,
        // böylece geçersiz bir dosya diskte yer kaplamıyor ve kullanıcı hatayı anında (Hangfire'ı beklemeden) görüyor.
        var columnValidationResult = await ValidateCsvColumnsAsync(file, dateColumnName, targetColumnName);
        if (!columnValidationResult.IsSuccess)
        {
            return Result.Failure<DatasetDto?>(columnValidationResult.ErrorType!.Value, columnValidationResult.Error!);
        }

        var storagePathResult = await SaveFileAsync(file, "datasets");
        if (!storagePathResult.IsSuccess)
        {
            return Result.Failure<DatasetDto?>(storagePathResult.ErrorType!.Value, storagePathResult.Error!);
        }

        var datasetEntity = new Dataset
        {
            ProjectId = projectId,
            Name = name,
            OriginalFileName = file.FileName,
            FilePath = storagePathResult.Value,
            DateColumn = dateColumnName,
            TargetColumn = targetColumnName,
            RecordCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = ProcessingStatus.Queued,
            IsProcessed = false,
            IsActive = true
        };

        _datasetRepository.CreateDataset(dataset: datasetEntity);
        await _unitOfWork.SaveChangesAsync();

        string jobId;
        try
        {
            jobId = _backgroundJobClient.Enqueue<IDataProcessingService>(service => 
                service.ProcessDatasetAsync(datasetEntity.Id, CancellationToken.None));
        }
        catch (Exception ex)
        {
            datasetEntity.Status = ProcessingStatus.Failed;
            datasetEntity.ErrorMessage = $"Enqueue'da hata oluştu: {ex.Message}";
            await _unitOfWork.SaveChangesAsync();
            return Result.Failure<DatasetDto?>(ResultErrorType.Unexpected, datasetEntity.ErrorMessage); 
        }
        
        datasetEntity.HangfireJobId = jobId;
        await _unitOfWork.SaveChangesAsync();

        var datasetResultDto = _mapper.Map<DatasetDto>(datasetEntity);
        return Result.Success<DatasetDto?>(datasetResultDto);
    }

    public async Task<Result<IEnumerable<DatasetDto>>> GetAllDatasetsForProjectAsync(int projectId, int userId)
    {
        var userOwnsProject = await _projectRepository.UserOwnsProjectAsync(projectId: projectId, userId: userId);
        if (!userOwnsProject)
        {
            return Result.Failure<IEnumerable<DatasetDto>>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }
        var datasets = await _datasetRepository.GetAllDatasetsForProjectAsync(projectId: projectId, trackChanges: false, includeUnprocessed: true);
        var datasetsDto = _mapper.Map<IEnumerable<DatasetDto>>(datasets);

        return Result.Success(datasetsDto);
    }

    public async Task<Result<DatasetDto?>> GetDatasetByIdAsync(int datasetId, int userId)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return Result.Failure<DatasetDto?>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        var dataset = await _datasetRepository.GetDatasetByIdAsync(id: datasetId, trackChanges: false);
        if (dataset == null)
        {
            return Result.Failure<DatasetDto?>(ResultErrorType.NotFound, ErrorMessages.DatasetNotFound);
        }

        var datasetDto = _mapper.Map<DatasetDto>(dataset);
        return Result.Success<DatasetDto?>(datasetDto);
    }

    public async Task<Result<bool>> DeleteDatasetAsync(int datasetId, int userId)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return Result.Failure<bool>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        var dataset = await _datasetRepository.GetDatasetByIdAsync(id: datasetId, trackChanges: true);
        if (dataset == null)
        {
            return Result.Failure<bool>(ResultErrorType.NotFound, ErrorMessages.DatasetNotFound);
        }

        dataset.IsActive = false;
        dataset.UpdatedAt = DateTime.UtcNow;
        _datasetRepository.UpdateDataset(dataset);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(true);
    }

    public async Task<Result<IEnumerable<DataPointDto>?>> GetDataPointsForDatasetAsync(int datasetId, int userId)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return Result.Failure<IEnumerable<DataPointDto>?>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        // Not: dataset'ler tipik bir portfolyo/demo boyutunda (yüzlerce-birkaç bin satır) olacağından
        // şimdilik sayfalama yapmıyoruz. Çok büyük dataset'ler için ileride limit/pagination eklenebilir.
        var dataPoints = await _dataPointRepository.GetDataPointsAsync(datasetId: datasetId);
        var ordered = dataPoints.OrderBy(dp => dp.Timestamp);

        return Result.Success<IEnumerable<DataPointDto>?>(_mapper.Map<IEnumerable<DataPointDto>>(ordered));
    }
}