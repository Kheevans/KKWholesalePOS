using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Settings;

namespace KKWholesalePOS.Data.Repositories;

public class BusinessSettingsRepository : IBusinessSettingsRepository
{
    private readonly AppDbContext _dbContext;

    public BusinessSettingsRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public BusinessSettings? GetSettings()
    {
        return _dbContext.BusinessSettings.FirstOrDefault();
    }

    public void Add(BusinessSettings settings)
    {
        _dbContext.BusinessSettings.Add(settings);
    }

    public void SaveChanges()
    {
        _dbContext.SaveChanges();
    }
}