namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Interface for notification service
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification
    /// </summary>
    Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notifications
    /// </summary>
    Task<List<Notification>> GetUnreadNotificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks notifications as read
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification
    /// </summary>
    Task DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all notifications
    /// </summary>
    Task ClearAllNotificationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Notification model
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationSeverity Severity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public string? ActionUrl { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Notification types
/// </summary>
public enum NotificationType
{
    SubmissionSuccess,
    SubmissionFailure,
    BatchCompleted,
    CertificateExpiring,
    CertificateExpired,
    ValidationError,
    SystemError,
    Information
}

/// <summary>
/// Notification severity levels
/// </summary>
public enum NotificationSeverity
{
    Information,
    Success,
    Warning,
    Error
}
