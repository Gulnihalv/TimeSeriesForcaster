using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Contracts.Persistence;

public interface IDashboardDismissalRepository
{
    Task<bool> ExistsAsync(int userId, string entityType, int entityId);
    Task<HashSet<(string EntityType, int EntityId)>> GetDismissedKeysForUserAsync(int userId);
    void CreateDismissal(DashboardDismissal dismissal);
}
