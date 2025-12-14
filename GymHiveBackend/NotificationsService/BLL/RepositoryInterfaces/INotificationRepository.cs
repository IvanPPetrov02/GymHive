using NotificationsService.BLL.Entities;

namespace NotificationsService.BLL.RepositoryInterfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int skip = 0, int take = 20);
    Task<List<Notification>> GetUnreadNotificationsAsync(Guid userId, int skip = 0, int take = 20);
    Task<List<Notification>> GetByUserIdAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<Notification?> GetByIdAsync(int id);
    Task<Notification> CreateAsync(Notification notification);
    Task<bool> MarkAsReadAsync(int id, Guid userId);
    Task<int> MarkAllAsReadAsync(Guid userId);
    Task<bool> DeleteAsync(int id, Guid userId);
    Task<bool> DeleteAsync(int id);
}
