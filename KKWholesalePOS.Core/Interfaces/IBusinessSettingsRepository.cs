using KKWholesalePOS.Core.Models.Settings;

namespace KKWholesalePOS.Core.Interfaces;

public interface IBusinessSettingsRepository
{
    BusinessSettings? GetSettings();

    void Add(BusinessSettings settings);

    void SaveChanges();
}