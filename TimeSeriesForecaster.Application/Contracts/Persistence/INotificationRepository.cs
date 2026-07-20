using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Contracts.Persistence;

public interface INotificationRepository
{
    void CreateNotification(Notification notification);
    Task<IEnumerable<Notification>> GetForUserAsync(int userId, int limit = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task<Notification?> GetByIdAsync(int id, bool trackChanges);
    Task MarkAllAsReadAsync(int userId);
}