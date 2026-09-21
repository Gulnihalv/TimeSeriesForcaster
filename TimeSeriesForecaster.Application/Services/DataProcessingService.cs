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
    private readonly int _chunkSize;

    public const int DefaultChunkSize = 10000;
    private const DateTimeStyles TimestampStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    private const NumberStyles ValueStyles = NumberStyles.Float;


    public DataProcessingService(IDataPointRepository dataPointRepository, IDatasetRepository datasetRepository, IProjectRepository projectRepository, INotificationService notificationService, IUnitOfWork unitOfWork, IWebHostEnvironment env, ILogger<DataProcessingService> logger, int chunkSize = DefaultChunkSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkSize, 1);
        _chunkSize = chunkSize;
        _dataPointRepository = dataPointRepository;
        _datasetRepository = datasetRepository;
        _projectRepository = projectRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _env = env;
        _logger = logger;
    }

    private record ImportResult(int ImportedRows, int SkippedRows, DateTime? MinDate, DateTime? MaxDate)
    {
        public static readonly ImportResult Empty = new(0, 0, null, null);
    }

    private async Task<ImportResult> ImportDataPointsAsync(Dataset dataset, string filePath, int totalRows, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!csv.Read())
        {
            _logger.LogWarning("CSV dosyası boş, başlık satırı bulunamadı.");
            return ImportResult.Empty;
        }
        csv.ReadHeader();

        // Kolonları her satırda isimle aramak yerine indekslerini bir kez buluyoruz. Karşılaştırma
        // büyük/küçük harf duyarsız: yükleme sırasındaki kolon doğrulaması (DatasetService) da öyle çalışıyor.
        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        var dateIndex = FindColumnIndex(headers, dataset.DateColumn);
        var valueIndex = FindColumnIndex(headers, dataset.TargetColumn);

        if (dateIndex < 0 || valueIndex < 0)
        {
            // Kolon yoksa her satır zaten başarısız olacak; milyonlarca satırı tek tek denemek yerine hemen dönüyoruz.
            _logger.LogWarning("Beklenen kolonlar bulunamadı. DateColumn: {DateColumn}, TargetColumn: {TargetColumn}",
                dataset.DateColumn, dataset.TargetColumn);
            return ImportResult.Empty;
        }

        var dataPoints = new List<DataPoint>(_chunkSize);
        DateTime? minDate = null;
        DateTime? maxDate = null;
        int readRows = 0;
        int importedRows = 0;
        int skippedRows = 0;
        int lastSavedProgress = 0;

        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            readRows++;

            if (TryParseRow(csv, dateIndex, valueIndex, out var timestamp, out var value))
            {
                if (minDate == null || timestamp < minDate) minDate = timestamp;
                if (maxDate == null || timestamp > maxDate) maxDate = timestamp;

                dataPoints.Add(new DataPoint
                {
                    DatasetId = dataset.Id,
                    Timestamp = timestamp,
                    Value = value,
                    IsOutlier = false,
                    CreatedAt = DateTime.UtcNow
                });
                importedRows++;
            }
            else
            {
                skippedRows++;
            }

            // '>=' savunmacı: liste bir şekilde chunk boyutunu aşarsa bile bir sonraki satırda yine flush edilir.
            if (dataPoints.Count >= _chunkSize)
            {
                await _dataPointRepository.BulkCopyDataPointsAsync(dataPoints, cancellationToken);
                dataPoints.Clear();
            }

            // İlerleme okunan satıra (başarılı + atlanan) göre hesaplanır, böylece bozuk satırlı dosyalarda da %100'e ulaşır.
            int currentProgress = totalRows > 0 ? (int)((double)readRows / totalRows * 100) : 0;
            if (currentProgress >= lastSavedProgress + 5)
            {
                dataset.ProgressPercentage = currentProgress;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                lastSavedProgress = currentProgress;
            }
        }

        if (dataPoints.Count > 0)
        {
            await _dataPointRepository.BulkCopyDataPointsAsync(dataPoints, cancellationToken);
            dataPoints.Clear();
        }

        return new ImportResult(importedRows, skippedRows, minDate, maxDate);
    }

    private static int FindColumnIndex(string[] headers, string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName)) return -1;
        return Array.FindIndex(headers, h => string.Equals(h, columnName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryParseRow(CsvReader csv, int dateIndex, int valueIndex, out DateTime timestamp, out decimal value)
    {
        timestamp = default;
        value = default;

        // Eksik alanlı (kısa) satırlar da bozuk satır sayılır.
        if (dateIndex >= csv.Parser.Count || valueIndex >= csv.Parser.Count)
        {
            return false;
        }

        return DateTime.TryParse(csv.Parser[dateIndex], CultureInfo.InvariantCulture, TimestampStyles, out timestamp)
            && decimal.TryParse(csv.Parser[valueIndex], ValueStyles, CultureInfo.InvariantCulture, out value);
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
                dataset.RecordCount = 0;
                dataset.StartDate = null;
                dataset.EndDate = null;
                dataset.SkippedRowCount = null;
                dataset.ErrorMessage = null;
                await _dataPointRepository.RemoveDataPointsForDatasetAsync(datasetId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var totalRows = File.ReadLines(filePath).Count() - 1;
                var importResult = await ImportDataPointsAsync(dataset, filePath, totalRows, cancellationToken);

                if (importResult.SkippedRows > 0)
                {
                    _logger.LogWarning("{FailedRows} / {TotalRows} satır okunamadı.",
                        importResult.SkippedRows, importResult.ImportedRows + importResult.SkippedRows);
                }

                if (!importResult.MinDate.HasValue || !importResult.MaxDate.HasValue)
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

                dataset.IsProcessed = true;
                dataset.Status = ProcessingStatus.Completed;
                dataset.ProgressPercentage = 100;
                dataset.RecordCount = importResult.ImportedRows;
                dataset.StartDate = importResult.MinDate;
                dataset.EndDate = importResult.MaxDate;
                dataset.SkippedRowCount = importResult.SkippedRows > 0 ? importResult.SkippedRows : null;
                dataset.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                var successNotifyResult = await NotifyDatasetResultAsync(dataset, success: true);
                if (!successNotifyResult.IsSuccess)
                {
                    _logger.LogWarning("Bildirim gönderilemedi: {Error}", successNotifyResult.Error);
                }

                _logger.LogInformation("Dataset processing completed successfully. Total records: {RecordCount}, StartDate: {StartDate}, EndDate: {EndDate}, SkippedRows: {SkippedRowCount}",
                    dataset.RecordCount, dataset.StartDate, dataset.EndDate, dataset.SkippedRowCount);
                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                dataset.IsProcessed = false;
                dataset.Status = ProcessingStatus.Failed;
                dataset.ErrorMessage = "Dataset işleme iptal edildi.";
                dataset.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                _logger.LogInformation("Dataset processing cancelled.");

                // Yeniden fırlatmazsak Hangfire job'ı "başarılı" sayar.
                throw;
            }
            catch (Exception ex)
            {
                // Beklenmeyen bir istisna (dosya I/O, DB hatası vb.) dataset'i "Processing"de asılı bırakmasın.
                dataset.IsProcessed = false;
                dataset.Status = ProcessingStatus.Failed;
                dataset.ErrorMessage = ex.Message;
                dataset.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                var notifyResult = await NotifyDatasetResultAsync(dataset, success: false);
                if (!notifyResult.IsSuccess)
                {
                    _logger.LogWarning("Bildirim gönderilemedi: {Error}", notifyResult.Error);
                }

                _logger.LogError(ex, "Dataset processing failed.");
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