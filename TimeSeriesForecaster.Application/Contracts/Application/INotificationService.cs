using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface INotificationService
{
    // Background job'lar (ModelProcessingService, DataProcessingService) tarafından çağrılır.
    Task CreateNotificationAsync(int userId, NotificationType type, string message, string relatedEntityType, int relatedEntityId);

    Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
}