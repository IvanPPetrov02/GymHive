using NotificationsService.BLL.DTOs;

namespace NotificationsService.BLL.ManagerInterfaces;

public interface INotificationManager
{
    Task<List<NotificationDTO>> GetUserNotificationsAsync(Guid userId, int skip = 0, int take = 20);
    Task<List<NotificationDTO>> GetUnreadNotificationsAsync(Guid userId, int skip = 0, int take = 20);
    Task<UnreadCountDTO> GetUnreadCountAsync(Guid userId);
    Task<bool> MarkAsReadAsync(int id, Guid userId);
    Task<int> MarkAllAsReadAsync(Guid userId);
    Task<bool> DeleteAsync(int id, Guid userId);
}
