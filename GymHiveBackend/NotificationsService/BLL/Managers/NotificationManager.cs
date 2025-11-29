using NotificationsService.BLL.DTOs;
using NotificationsService.BLL.ManagerInterfaces;
using NotificationsService.DAL.RepositoryInterfaces;

namespace NotificationsService.BLL.Managers;

public class NotificationManager : INotificationManager
{
    private readonly INotificationRepository _repository;

    public NotificationManager(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<NotificationDTO>> GetUserNotificationsAsync(Guid userId, int skip = 0, int take = 20)
    {
        var notifications = await _repository.GetUserNotificationsAsync(userId, skip, take);
        
        return notifications.Select(n => new NotificationDTO
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            RelatedEntityId = n.RelatedEntityId,
            RelatedEntityType = n.RelatedEntityType,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task<UnreadCountDTO> GetUnreadCountAsync(Guid userId)
    {
        var count = await _repository.GetUnreadCountAsync(userId);
        return new UnreadCountDTO { Count = count };
    }

    public async Task<bool> MarkAsReadAsync(int id, Guid userId)
    {
        return await _repository.MarkAsReadAsync(id, userId);
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        return await _repository.MarkAllAsReadAsync(userId);
    }

    public async Task<bool> DeleteAsync(int id, Guid userId)
    {
        return await _repository.DeleteAsync(id, userId);
    }
}
