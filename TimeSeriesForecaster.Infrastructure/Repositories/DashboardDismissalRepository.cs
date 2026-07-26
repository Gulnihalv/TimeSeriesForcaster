using Microsoft.EntityFrameworkCore;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Repositories;

public class DashboardDismissalRepository : IDashboardDismissalRepository
{
    private readonly AppDbContext _context;

    public DashboardDismissalRepository(AppDbContext context)
    {
        _context = context;
    }

    public void CreateDismissal(DashboardDismissal dismissal) => _context.DashboardDismissals.Add(dismissal);

    public async Task<bool> ExistsAsync(int userId, string entityType, int entityId)
    {
        return await _context.DashboardDismissals
            .AsNoTracking()
            .AnyAsync(d => d.UserId == userId && d.EntityType == entityType && d.EntityId == entityId);
    }

    public async Task<HashSet<(string EntityType, int EntityId)>> GetDismissedKeysForUserAsync(int userId)
    {
        var rows = await _context.DashboardDismissals
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => new { d.EntityType, d.EntityId })
            .ToListAsync();

        return rows.Select(r => (r.EntityType, r.EntityId)).ToHashSet();
    }
}
