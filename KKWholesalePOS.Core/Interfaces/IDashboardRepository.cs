
using KKWholesalePOS.Core.Models.Dashboard;

namespace KKWholesalePOS.Core.Interfaces;

public interface IDashboardRepository
{
    DashboardSummary GetDashboardSummary();
}
