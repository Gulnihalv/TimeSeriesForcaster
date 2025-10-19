using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Interfaces;

public interface IBackgroundJobRepository
{
    Task<BackgroundJob?> GetJobByIdAsync(int id, bool trackChanges);
    Task<IEnumerable<BackgroundJob>> GetJobsForUserAsync(int userId, bool trackChanges);
    Task<IEnumerable<BackgroundJob>> GetJobsByStatusAsync(JobStatus status, bool trackChanges);
    Task<IEnumerable<BackgroundJob>> GetJobsByTypeAsync(JobType jobType, bool trackChanges);
    Task<IEnumerable<BackgroundJob>> GetActiveJobsAsync(bool trackChanges); // running, queued
    Task<IEnumerable<BackgroundJob>> GetJobsForDatasetAsync(int datasetId, bool trackChanges);
    void CreateJob(BackgroundJob job);
    void RemoveJob(BackgroundJob job);
    void UpdateJob(BackgroundJob job);
    Task<bool> JobExistsAsync(int id);
    Task<bool> UserOwnsJobAsync(int jobId, int userId);
    Task<IEnumerable<BackgroundJob>> GetCompletedJobsOlderThanAsync(DateTime cutoffDate); // Cleanup için
    Task<BackgroundJob?> GetRunningJobForDatasetAsync(int datasetId, JobType jobType);
}
