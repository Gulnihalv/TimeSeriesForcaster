using System.Globalization;
using CsvHelper;
using Microsoft.AspNetCore.Hosting;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

public class DataProcessingService : IDataProcessingService
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IDataPointRepository _dataPointRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;

    public DataProcessingService(IDataPointRepository dataPointRepository, IDatasetRepository datasetRepository, IUnitOfWork unitOfWork, IWebHostEnvironment env)
    {
        _dataPointRepository = dataPointRepository;
        _datasetRepository = datasetRepository;
        _unitOfWork = unitOfWork;
        _env = env;
    }
    
    public async Task ProcessDatasetAsync(int datasetId, CancellationToken cancellationToken = default)
    {
        var dataset = await _datasetRepository.GetDatasetByIdAsync(id: datasetId, trackChanges: false);
        if (dataset == null) return;

        var filePath = Path.Combine(_env.ContentRootPath, dataset.FilePath!);
        var dataPoints = new List<DataPoint>();

        DateTime minDate = DateTime.MaxValue;
        DateTime maxDate = DateTime.MinValue;

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
                    var timeStamp = csv.GetField<DateTime>(dateColumn);
                    var value = csv.GetField<decimal>(targetColumn);

                    if (timeStamp < minDate) minDate = timeStamp;
                    if (timeStamp > maxDate) maxDate = timeStamp;

                    var newDataPoint = new DataPoint
                    {
                        DatasetId = dataset.Id,
                        Timestamp = timeStamp.ToUniversalTime(),
                        Value = value,
                        IsOutlier = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    dataPoints.Add(newDataPoint);
                }
                catch // şimdilik loglama mekanizması yok.
                {
                    // _logger.LogWarning($"CSV satırı okunamadı: {ex.Message}");
                }
            }
        }

        if (!dataPoints.Any())
        {
            dataset.IsProcessed = false;
            dataset.ErrorMessage = "Dosyadan veri okunamadı veya sütunlar yanlış.";
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _dataPointRepository.CreateDataPointsBulkAsync(dataPoints);

        dataset.IsProcessed = true;
        dataset.RecordCount = dataPoints.Count;
        dataset.StartDate = minDate;
        dataset.EndDate = maxDate;
        dataset.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
