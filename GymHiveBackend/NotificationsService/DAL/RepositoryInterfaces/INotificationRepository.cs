using NotificationsService.DAL.Entities;

namespace NotificationsService.DAL.RepositoryInterfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int skip = 0, int take = 20);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<Notification?> GetByIdAsync(int id);
    Task<Notification> CreateAsync(Notification notification);
    Task<bool> MarkAsReadAsync(int id, Guid userId);
    Task<int> MarkAllAsReadAsync(Guid userId);
    Task<bool> DeleteAsync(int id, Guid userId);
}
