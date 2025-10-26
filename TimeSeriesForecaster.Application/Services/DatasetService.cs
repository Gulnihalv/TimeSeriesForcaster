using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;

    public DatasetService(IUnitOfWork unitOfWork, IDatasetRepository datasetRepository, IProjectRepository projectRepository, IMapper mapper, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _datasetRepository = datasetRepository;
        _projectRepository = projectRepository;
        _mapper = mapper;
        _env = env;
    }
    public async Task<string> SaveFileAsync(IFormFile file, string subDirectory)
    {
        if (file.Length == 0 || file == null)
        {
            throw new ArgumentException("Dosya boş olamaz.", nameof(file));
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
            
        return relativeFilePath;
    
    }

    public async Task<DatasetDto?> CreateDatasetFromUploadAsync(int projectId, int userId, string name, IFormFile file)
    {
        var userOwnsProject = await _projectRepository.UserOwnsProjectAsync(projectId: projectId, userId: userId);
        if (!userOwnsProject)
        {
            return null;
        }

        var storagePath = await SaveFileAsync(file, "datasets");

        var dasetEntity = new Dataset
        {
            ProjectId = projectId,
            Name = name,
            OriginalFileName = file.FileName,
            FilePath = storagePath,
            RecordCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _datasetRepository.CreateDataset(dataset: dasetEntity);
        await _unitOfWork.SaveChangesAsync();

        var datasetResultDto = _mapper.Map<DatasetDto>(dasetEntity);
        return datasetResultDto;
    }

    public async Task<IEnumerable<DatasetDto>> GetAllDatasetsForProjectAsync(int projectId, int userId)
    {
        var userOwnsProject = await _projectRepository.UserOwnsProjectAsync(projectId: projectId, userId: userId);
        if (!userOwnsProject)
        {
            return new List<DatasetDto>(); // ama böyle yapınca hata dönmiycek boş dönücek.
        }
        var datasets = await _datasetRepository.GetAllDatasetsForProjectAsync(projectId: projectId, trackChanges: false, includeUnprocessed: true);
        var datasetsDto = _mapper.Map<IEnumerable<DatasetDto>>(datasets);

        return datasetsDto;
    }

    public async Task<DatasetDto?> GetDatasetByIdAsync(int datasetId, int userId)
    {
        var userOwnsDataset = await _datasetRepository.UserOwnsDatasetAsync(datasetId: datasetId, userId: userId);
        if (!userOwnsDataset)
        {
            return null;
        }

        var dataset = await _datasetRepository.GetDatasetByIdAsync(id: datasetId, trackChanges: false);
        if (dataset == null)
        {
            return null;
        }

        var datasetDto = _mapper.Map<DatasetDto>(dataset);
        return datasetDto;
    }
}
