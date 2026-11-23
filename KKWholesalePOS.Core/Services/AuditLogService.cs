
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class AuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        UserSessionService userSession,
        AuthorizationService authorizationService)
    {
        _auditLogRepository = auditLogRepository;
        _userSession = userSession;
        _authorizationService = authorizationService;
    }

    private void RequirePermission(string permissionName)
    {
        var currentUser = _userSession.CurrentUser;

        if (currentUser == null ||
            !_authorizationService.HasPermission(
                currentUser,
                permissionName))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to perform this action.");
        }
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
        RequirePermission("AuditLog.View");
        return _auditLogRepository.GetAll();
    }

    public List<AuditLogEntry> GetUserEntries(int userId)
    {
        RequirePermission("AuditLog.View");
        return _auditLogRepository.GetByUserId(userId);
    }

    public List<AuditLogEntry> GetEntityEntries(
        string entityType,
        string entityId)
    {
        RequirePermission("AuditLog.View");

        return _auditLogRepository.GetByEntity(
            entityType,
            entityId);
    }
}
