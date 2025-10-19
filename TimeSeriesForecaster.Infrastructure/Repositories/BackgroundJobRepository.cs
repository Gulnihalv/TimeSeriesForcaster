using Microsoft.EntityFrameworkCore;
using TimeSeriesForecaster.Application.Interfaces;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Repositories;

public class BackgroundJobRepository: IBackgroundJobRepository
{
    private readonly AppDbContext _context;

    public BackgroundJobRepository(AppDbContext context)
    {
        _context = context;
    }

    public void CreateJob(BackgroundJob job) => _context.BackgroundJobs.Add(job);
    public void RemoveJob(BackgroundJob job) => _context.BackgroundJobs.Remove(job);
    public void UpdateJob(BackgroundJob job) => _context.BackgroundJobs.Update(job);

    public async Task<IEnumerable<BackgroundJob>> GetActiveJobsAsync(bool trackChanges)
    {
        IQueryable<BackgroundJob> query = trackChanges
            ? _context.BackgroundJobs
            : _context.BackgroundJobs.AsNoTracking();

        return await query
            .Where(j => j.Status == JobStatus.Queued || j.Status == JobStatus.Running)
            .ToListAsync();
    }

    public async Task<IEnumerable<BackgroundJob>> GetCompletedJobsOlderThanAsync(DateTime cutoffDate)
    {
        return await _context.BackgroundJobs
            .AsNoTracking()
            .Where(j => j.Status == JobStatus.Completed && j.CompletedAt < cutoffDate)
            .ToListAsync();
    }

    public async Task<BackgroundJob?> GetJobByIdAsync(int id, bool trackChanges)
    {
        IQueryable<BackgroundJob> query = trackChanges
            ? _context.BackgroundJobs
            : _context.BackgroundJobs.AsNoTracking();

        return await query.FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<IEnumerable<BackgroundJob>> GetJobsByStatusAsync(JobStatus status, bool trackChanges)
    {
        IQueryable<BackgroundJob> query = trackChanges
            ? _context.BackgroundJobs
            : _context.BackgroundJobs.AsNoTracking();

        return await query.Where(j => j.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<BackgroundJob>> GetJobsByTypeAsync(JobType jobType, bool trackChanges)
    {
        IQueryable<BackgroundJob> query = trackChanges
            ? _context.BackgroundJobs
            : _context.BackgroundJobs.AsNoTracking();

        return await query.Where(j => j.JobType == jobType).ToListAsync();
    }

    public async Task<IEnumerable<BackgroundJob>> GetJobsForDatasetAsync(int datasetId, bool trackChanges)
    {
        IQueryable<BackgroundJob> query = trackChanges
            ? _context.BackgroundJobs
            : _context.BackgroundJobs.AsNoTracking();

        return await query.Where(j => j.DatasetId == datasetId).ToListAsync();
    }

    public async Task<IEnumerable<BackgroundJob>> GetJobsForUserAsync(int userId, bool trackChanges)
    {
        IQueryable<BackgroundJob> query = trackChanges
            ? _context.BackgroundJobs
            : _context.BackgroundJobs.AsNoTracking();

        return await query.Where(j => j.UserId == userId).ToListAsync();
    }

    public async Task<BackgroundJob?> GetRunningJobForDatasetAsync(int datasetId, JobType jobType)
    {
        var activeStatuses = new[] { JobStatus.Queued, JobStatus.Running };

        return await _context.BackgroundJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.DatasetId == datasetId && j.JobType == jobType && activeStatuses.Contains(j.Status));
    }

    public async Task<bool> JobExistsAsync(int id)
    {
        return await _context.BackgroundJobs.AnyAsync(j => j.Id == id);
    }

    public async Task<bool> UserOwnsJobAsync(int jobId, int userId)
    {
        return await _context.BackgroundJobs.AnyAsync(j => j.Id == jobId && j.UserId == userId);
    }
}
