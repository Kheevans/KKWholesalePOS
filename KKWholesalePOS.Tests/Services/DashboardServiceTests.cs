
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Dashboard;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Services;
using Moq;

namespace KKWholesalePOS.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IDashboardRepository> _repositoryMock = new();

    private DashboardService CreateAuthorizedService()
    {
        var session = new UserSessionService();

        var user = new User
        {
            Id = 1,
            Username = "testadmin",
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role
                    {
                        Name = "Administrator",
                        RolePermissions = new List<RolePermission>
                        {
                            new RolePermission
                            {
                                Permission = new Permission
                                {
                                    Name = "Dashboard.View"
                                }
                            }
                        }
                    }
                }
            }
        };

        session.StartSession(user);

        return new DashboardService(
            _repositoryMock.Object,
            session,
            new AuthorizationService());
    }

    private DashboardService CreateUnauthorizedService()
    {
        return new DashboardService(
            _repositoryMock.Object,
            new UserSessionService(),
            new AuthorizationService());
    }

    [Fact]
    public void GetDashboardSummary_WithPermission_ReturnsSummary()
    {
        var service = CreateAuthorizedService();
        var expectedSummary = new DashboardSummary();

        _repositoryMock
            .Setup(repository => repository.GetDashboardSummary())
            .Returns(expectedSummary);

        var result = service.GetDashboardSummary();

        Assert.Same(expectedSummary, result);

        _repositoryMock.Verify(
            repository => repository.GetDashboardSummary(),
            Times.Once);
    }

    [Fact]
    public void GetDashboardSummary_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateUnauthorizedService();

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.GetDashboardSummary());

        _repositoryMock.Verify(
            repository => repository.GetDashboardSummary(),
            Times.Never);
    }
}
