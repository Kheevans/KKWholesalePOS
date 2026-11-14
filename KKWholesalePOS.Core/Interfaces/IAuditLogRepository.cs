using KKWholesalePOS.Core.Models;

namespace KKWholesalePOS.Core.Interfaces;

public interface IAuditLogRepository
{
    void Add(AuditLogEntry entry);

    List<AuditLogEntry> GetAll();

    List<AuditLogEntry> GetByUserId(int userId);

    List<AuditLogEntry> GetByEntity(
        string entityType,
        string entityId);

    void SaveChanges();
}