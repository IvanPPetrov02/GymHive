using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationsService.BLL.ManagerInterfaces;
using NotificationsService.Services;

namespace NotificationsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationManager _notificationManager;
    private readonly IUserContextService _userContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationManager notificationManager,
        IUserContextService userContext,
        ILogger<NotificationsController> logger)
    {
        _notificationManager = notificationManager;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        try
        {
            var userId = _userContext.GetUserId();
            var notifications = await _notificationManager.GetUserNotificationsAsync(userId, skip, take);
            return Ok(notifications);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(500, new { error = "An error occurred while retrieving notifications" });
        }
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = _userContext.GetUserId();
            var count = await _notificationManager.GetUnreadCountAsync(userId);
            return Ok(count);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread count");
            return StatusCode(500, new { error = "An error occurred while retrieving unread count" });
        }
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = _userContext.GetUserId();
            var result = await _notificationManager.MarkAsReadAsync(id, userId);
            
            if (!result)
                return NotFound(new { error = "Notification not found" });

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { error = "An error occurred while marking notification as read" });
        }
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = _userContext.GetUserId();
            var count = await _notificationManager.MarkAllAsReadAsync(userId);
            return Ok(new { markedAsRead = count });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { error = "An error occurred while marking notifications as read" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        try
        {
            var userId = _userContext.GetUserId();
            var result = await _notificationManager.DeleteAsync(id, userId);
            
            if (!result)
                return NotFound(new { error = "Notification not found" });

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            return StatusCode(500, new { error = "An error occurred while deleting notification" });
        }
    }
}
