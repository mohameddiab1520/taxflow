using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TaxFlow.Infrastructure.Services.Auth;

/// <summary>
/// Audit service for logging user actions
/// </summary>
public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IAuditLogRepository auditLogRepository,
        ILogger<AuditService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task LogActionAsync(
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId = null,
        object? oldValues = null,
        object? newValues = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                CreatedAt = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog);

            _logger.LogInformation(
                "Audit log created: User {UserId} performed {Action} on {EntityType} {EntityId}",
                userId,
                action,
                entityType,
                entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log");
            // Don't throw - audit logging should not break the application
        }
    }

    public async Task<List<AuditLog>> GetUserActivityAsync(Guid userId, int count = 100)
    {
        try
        {
            return await _auditLogRepository.GetByUserAsync(userId, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity for {UserId}", userId);
            return new List<AuditLog>();
        }
    }

    public async Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, Guid entityId)
    {
        try
        {
            return await _auditLogRepository.GetByEntityAsync(entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity history for {EntityType} {EntityId}",
                entityType, entityId);
            return new List<AuditLog>();
        }
    }
}
