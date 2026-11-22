
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Dashboard;

namespace KKWholesalePOS.Core.Services;

public class DashboardService
{
    private readonly IDashboardRepository _repository;

    public DashboardService(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public DashboardSummary GetDashboardSummary()
    {
        return _repository.GetDashboardSummary();
    }
}
