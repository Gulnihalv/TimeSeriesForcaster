using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

public class DataProcessingService : IDataProcessingService
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IDataPointRepository _dataPointRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DataProcessingService> _logger;

    public DataProcessingService(IDataPointRepository dataPointRepository, IDatasetRepository datasetRepository, IProjectRepository projectRepository, INotificationService notificationService, IUnitOfWork unitOfWork, IWebHostEnvironment env, ILogger<DataProcessingService> logger)
    {
        _dataPointRepository = dataPointRepository;
        _datasetRepository = datasetRepository;
        _projectRepository = projectRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _env = env;
        _logger = logger;
    }
    
    public async Task<Result> ProcessDatasetAsync(int datasetId, CancellationToken cancellationToken = default)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["DatasetId"] = datasetId }))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Dataset processing started.");

            var dataset = await _datasetRepository.GetDatasetByIdAsync(id: datasetId, trackChanges: true);
            if (dataset == null) return Result.Failure(ResultErrorType.NotFound, ErrorMessages.DatasetNotFound);

            var filePath = Path.Combine(_env.ContentRootPath, dataset.FilePath!);

            try
            {
                dataset.Status = ProcessingStatus.Processing;
                dataset.ProgressPercentage = 0;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Yüzdelik ilerlemeyi hesaplayabilmek için toplam satır sayısını
                // önceden (header hariç) sayıyoruz.
                var totalRows = File.ReadLines(filePath).Count() - 1;

                var dataPoints = new List<DataPoint>();
                DateTime minDate = DateTime.MaxValue;
                DateTime maxDate = DateTime.MinValue;
                var processedRows = 0;
                var failedRows = 0;
                var lastPersistedThreshold = 0;

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();

                    string dateColumn = dataset.DateColumn!;
                    string targetColumn = dataset.TargetColumn!;

                    while (csv.Read())
                    {
                        try
                        {
                            var rawTimeStamp = csv.GetField<DateTime>(dateColumn);
                            var timeStamp = DateTime.SpecifyKind(rawTimeStamp, DateTimeKind.Utc);
                            var value = csv.GetField<decimal>(targetColumn);

                            if (timeStamp < minDate) minDate = timeStamp;
                            if (timeStamp > maxDate) maxDate = timeStamp;

                            var newDataPoint = new DataPoint
                            {
                                DatasetId = dataset.Id,
                                Timestamp = timeStamp,
                                Value = value,
                                IsOutlier = false,
                                CreatedAt = DateTime.UtcNow
                            };

                            dataPoints.Add(newDataPoint);
                        }
                        catch (Exception)
                        {
                            failedRows++;
                        }
                        finally
                        {
                            processedRows++;
                            if (totalRows > 0)
                            {
                                var pct = (int)(processedRows / (double)totalRows * 100);
                                var threshold = pct / 10 * 10;
                                if (threshold > lastPersistedThreshold)
                                {
                                    lastPersistedThreshold = threshold;
                                    dataset.ProgressPercentage = threshold;
                                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                                }
                            }
                        }
                    }
                }

                if (failedRows > 0)
                {
                    _logger.LogWarning("{FailedRows} / {TotalRows} satır okunamadı.", failedRows, processedRows);
                }

                if (!dataPoints.Any())
                {
                    dataset.IsProcessed = false;
                    dataset.Status = ProcessingStatus.Failed;
                    dataset.ErrorMessage = "Dosyadan veri okunamadı veya sütunlar yanlış.";
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    var notifyResult = await NotifyDatasetResultAsync(dataset, success: false);
                    if (!notifyResult.IsSuccess)
                    {
                        _logger.LogWarning("Bildirim gönderilemedi: {Error}", notifyResult.Error);
                    }

                    _logger.LogWarning("Dataset processing failed: {ErrorMessage}", dataset.ErrorMessage);
                    return Result.Failure(ResultErrorType.BadRequest, dataset.ErrorMessage);
                }

                await _dataPointRepository.CreateDataPointsBulkAsync(dataPoints);

                dataset.IsProcessed = true;
                dataset.Status = ProcessingStatus.Completed;
                dataset.ProgressPercentage = 100;
                dataset.RecordCount = dataPoints.Count;
                dataset.StartDate = minDate;
                dataset.EndDate = maxDate;
                dataset.SkippedRowCount = failedRows > 0 ? failedRows : null;
                dataset.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                var successNotifyResult = await NotifyDatasetResultAsync(dataset, success: true);
                if (!successNotifyResult.IsSuccess)
                {
                    _logger.LogWarning("Bildirim gönderilemedi: {Error}", successNotifyResult.Error);
                }

                _logger.LogInformation("Dataset processing completed successfully. Total records: {RecordCount}, StartDate: {StartDate}, EndDate: {EndDate}, SkippedRows: {SkippedRowCount}", dataset.RecordCount, dataset.StartDate, dataset.EndDate, dataset.SkippedRowCount);
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Parse döngüsünün dışında (örn. dosya I/O hatası) beklenmeyen bir
                // istisna oluşursa dataset "Processing"de asılı kalmasın.
                dataset.IsProcessed = false;
                dataset.Status = ProcessingStatus.Failed;
                dataset.ErrorMessage = ex.Message;
                dataset.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                var notifyResult = await NotifyDatasetResultAsync(dataset, success: false);
                if (!notifyResult.IsSuccess)
                {
                    _logger.LogWarning("Bildirim gönderilemedi: {Error}", notifyResult.Error);
                }

                _logger.LogError(ex, "Dataset processing failed: {ErrorMessage}", ex.Message);
                return Result.Failure(ResultErrorType.Unexpected, dataset.ErrorMessage);
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Dataset processing completed in {ElapsedMilliseconds} ms.", sw.ElapsedMilliseconds);
            }
        }
    }

    private async Task<Result> NotifyDatasetResultAsync(Dataset dataset, bool success)
    {
        var project = await _projectRepository.GetProjectByIdAsync(id: dataset.ProjectId, trackChanges: false);
        if (project == null) return Result.Failure(ResultErrorType.NotFound, ErrorMessages.ProjectNotFound);

        var type = success ? NotificationType.DatasetProcessingCompleted : NotificationType.DatasetProcessingFailed;
        var message = success
            ? $"\"{dataset.Name}\" dataset'i başarıyla işlendi."
            : $"\"{dataset.Name}\" dataset'i işlenemedi: {dataset.ErrorMessage}";

        return await _notificationService.CreateNotificationAsync(project.UserId, type, message, "Dataset", dataset.Id);
    }
}