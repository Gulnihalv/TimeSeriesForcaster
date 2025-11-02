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
        var filePath = _env.ContentRootPath + dataset!.FilePath;

        var dataPoints = new List<DataPoint>();

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                try
                {
                    var timeStamp = csv.GetField<DateTime>("TimeStamp");
                    var value = csv.GetField<decimal>("Value");

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
                catch (Exception ex)
                {
                    // _logger.LogWarning($"CSV satırı okunamadı: {ex.Message}");
                }
            }
        }

        if (!dataPoints.Any())
        {
            return;
        }
        await _dataPointRepository.CreateDataPointsBulkAsync(dataPoints);

        dataset.IsProcessed = true;
        dataset.RecordCount = dataPoints.Count;
        dataset.StartDate = dataPoints.Min(dp => dp.Timestamp);
        dataset.EndDate = dataPoints.Max(dp => dp.Timestamp);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
