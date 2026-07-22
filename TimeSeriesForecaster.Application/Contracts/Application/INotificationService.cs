using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface INotificationService
{
    Task<Result> CreateNotificationAsync(int userId, NotificationType type, string message, string relatedEntityType, int relatedEntityId);
    Task<Result<IEnumerable<NotificationDto>>> GetNotificationsForUserAsync(int userId);
    Task<Result<int>> GetUnreadCountAsync(int userId);
    Task<Result<bool>> MarkAsReadAsync(int notificationId, int userId);
    Task<Result> MarkAllAsReadAsync(int userId);
}