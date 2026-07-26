using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IDashboardService
{
    Task<Result<IEnumerable<RecentActivityItemDto>>> GetRecentActivityAsync(int userId, int take = 20);
    Task<Result> DismissActivityAsync(int userId, string entityType, int entityId);
}
