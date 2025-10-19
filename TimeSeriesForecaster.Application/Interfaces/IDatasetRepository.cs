using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Interfaces;

public interface IDatasetRepository
{
    Task<Dataset?> GetDatasetByIdAsync(int id, bool trackChanges);
    Task<IEnumerable<Dataset>> GetAllDatasetsForProjectAsync(int projectId, bool trackChanges, bool includeUnprocessed = true);
    void CreateDataset(Dataset dataset);
    void UpdateDataset(Dataset dataset);
    void RemoveDataset(Dataset dataset);
    Task<bool> DatasetExistsAsync(int id);
    Task<bool> UserOwnsDatasetAsync(int datasetId, int userId);
    Task<bool> IsDatasetProcessedAsync(int id);
    Task<int> GetDatasetRecordCountAsync(int id);
    // Task<(DateTime startDate, DateTime endDate)> GetDatasetDateRangeAsync(int id); // Bu datapointsteki GetDateRangeAsync methedu ile aynı olduğundan bunu kaldırıyorum.
    
}
