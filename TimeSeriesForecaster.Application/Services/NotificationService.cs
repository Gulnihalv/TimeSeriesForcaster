using AutoMapper;
using TimeSeriesForecaster.Application.Common;
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

    public async Task<Result> CreateNotificationAsync(int userId, NotificationType type, string message, string relatedEntityType, int relatedEntityId)
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
        return Result.Success();
    }

    public async Task<Result<IEnumerable<NotificationDto>>> GetNotificationsForUserAsync(int userId)
    {
        var notifications = await _notificationRepository.GetForUserAsync(userId);
        return Result.Success(_mapper.Map<IEnumerable<NotificationDto>>(notifications));
    }

    public async Task<Result<int>> GetUnreadCountAsync(int userId)
    {
        var count = await _notificationRepository.GetUnreadCountAsync(userId);
        return Result.Success(count);
    }

    public async Task<Result<bool>> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId, trackChanges: true);
        if (notification == null)
        {
            return Result.Failure<bool>(ResultErrorType.NotFound, ErrorMessages.NotificationNotFound);
        }

        if (notification.UserId != userId)
        {
            return Result.Failure<bool>(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        notification.IsRead = true;
        await _unitOfWork.SaveChangesAsync();
        return Result.Success(true);
    }

    public async Task<Result> MarkAllAsReadAsync(int userId)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId);
        return Result.Success();
    }
}