using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.WebAPI.Extensions;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
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
            return Unauthorized("User id bulunmadı");
        }

        var notifications = await _notificationService.GetNotificationsForUserAsync(userId.Value);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(new { count });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var success = await _notificationService.MarkAsReadAsync(id, userId.Value);
        if (!success)
        {
            return NotFound("Bildirim bulunamadı veya bu kullanıcıya ait değil.");
        }

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        await _notificationService.MarkAllAsReadAsync(userId.Value);
        return NoContent();
    }
}