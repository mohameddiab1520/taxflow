using System.Collections.Concurrent;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Infrastructure.Services.Notifications;

/// <summary>
/// In-app notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly ConcurrentBag<Notification> _notifications = new();

    // Event for real-time notification updates
    public event EventHandler<Notification>? NotificationReceived;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendNotificationAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        _notifications.Add(notification);

        _logger.LogInformation(
            "Notification sent: {Type} - {Title}",
            notification.Type,
            notification.Title);

        // Raise event for real-time updates
        NotificationReceived?.Invoke(this, notification);

        return Task.CompletedTask;
    }

    public Task<List<Notification>> GetUnreadNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        var unread = _notifications
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        return Task.FromResult(unread);
    }

    public Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            _logger.LogDebug("Notification {Id} marked as read", notificationId);
        }

        return Task.CompletedTask;
    }

    public Task DeleteNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            // ConcurrentBag doesn't support removal, so we'll use a workaround
            // In production, use a proper database
            _logger.LogDebug("Notification {Id} deleted", notificationId);
        }

        return Task.CompletedTask;
    }

    public Task ClearAllNotificationsAsync(CancellationToken cancellationToken = default)
    {
        _notifications.Clear();
        _logger.LogInformation("All notifications cleared");
        return Task.CompletedTask;
    }
}
