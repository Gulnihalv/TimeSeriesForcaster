using AutoMapper;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(INotificationRepository notificationRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task CreateNotificationAsync(int userId, NotificationType type, string message, string relatedEntityType, int relatedEntityId)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Message = message,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _notificationRepository.CreateNotification(notification);
        await _unitOfWork.SaveChangesAsync();

        // İleride e-posta gönderimi buraya eklenecek: kullanıcının e-postasını çekip
        // IEmailService.SendEmailAsync çağrılabilir. Şimdilik altyapı hazır, kullanılmıyor.
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(int userId)
    {
        var notifications = await _notificationRepository.GetForUserAsync(userId);
        return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId, trackChanges: true);
        if (notification == null || notification.UserId != userId)
        {
            return false;
        }

        notification.IsRead = true;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId);
    }
}