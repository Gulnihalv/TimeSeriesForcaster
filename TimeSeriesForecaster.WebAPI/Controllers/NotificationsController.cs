using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.WebAPI.Extensions;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _notificationService.GetNotificationsForUserAsync(userId.Value);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _notificationService.GetUnreadCountAsync(userId.Value);
        return ToActionResult(result, value => Ok(new { count = value }));
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _notificationService.MarkAsReadAsync(id, userId.Value);
        return ToActionResult(result);
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _notificationService.MarkAllAsReadAsync(userId.Value);
        return ToActionResult(result);
    }
}