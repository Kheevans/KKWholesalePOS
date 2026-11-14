using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _dbContext;

    public AuditLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(AuditLogEntry entry)
    {
        _dbContext.AuditLogEntries.Add(entry);
    }

    public List<AuditLogEntry> GetAll()
    {
        return _dbContext.AuditLogEntries
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .ToList();
    }

    public List<AuditLogEntry> GetByUserId(int userId)
    {
        return _dbContext.AuditLogEntries
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .ToList();
    }

    public List<AuditLogEntry> GetByEntity(
        string entityType,
        string entityId)
    {
        return _dbContext.AuditLogEntries
            .Include(a => a.User)
            .Where(a =>
                a.EntityType == entityType &&
                a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToList();
    }

    public void SaveChanges()
    {
        _dbContext.SaveChanges();
    }
}