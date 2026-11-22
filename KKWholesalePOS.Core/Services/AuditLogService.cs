using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class AuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public void LogAction(
        int? userId,
        string action,
        string entityType,
        string? entityId = null,
        string? details = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException(
                "Audit action cannot be empty.",
                nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException(
                "Entity type cannot be empty.",
                nameof(entityType));
        }

        var entry = new AuditLogEntry
        {
            UserId = userId,
            Action = action.Trim(),
            EntityType = entityType.Trim(),
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _auditLogRepository.Add(entry);
        _auditLogRepository.SaveChanges();
    }

    public List<AuditLogEntry> GetAllEntries()
    {
        return _auditLogRepository.GetAll();
    }

    public List<AuditLogEntry> GetUserEntries(int userId)
    {
        return _auditLogRepository.GetByUserId(userId);
    }

    public List<AuditLogEntry> GetEntityEntries(
        string entityType,
        string entityId)
    {
        return _auditLogRepository.GetByEntity(
            entityType,
            entityId);
    }
}