using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests.Services;

public class AuthorizationServiceTests
{
    [Fact]
    public void HasPermission_ShouldReturnTrueWhenUserHasPermission()
    {
        // Arrange
        var permission = new Permission
        {
            Id = 1,
            Name = "Consignment.Process"
        };

        var role = new Role
        {
            Id = 1,
            Name = "Consignment Officer"
        };

        role.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            Role = role,
            PermissionId = permission.Id,
            Permission = permission
        });

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true
        };

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            User = user,
            RoleId = role.Id,
            Role = role
        });

        var service = new AuthorizationService();

        // Act
        bool result = service.HasPermission(
            user,
            "Consignment.Process");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasPermission_ShouldReturnFalseWhenUserLacksPermission()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true
        };

        var service = new AuthorizationService();

        // Act
        bool result = service.HasPermission(
            user,
            "Consignment.Process");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPermission_ShouldReturnFalseForInactiveUser()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = false
        };

        var service = new AuthorizationService();

        // Act
        bool result = service.HasPermission(
            user,
            "Consignment.Process");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPermission_ShouldIgnorePermissionNameCase()
    {
        // Arrange
        var permission = new Permission
        {
            Id = 1,
            Name = "Consignment.Process"
        };

        var role = new Role
        {
            Id = 1,
            Name = "Consignment Officer"
        };

        role.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            Role = role,
            PermissionId = permission.Id,
            Permission = permission
        });

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true
        };

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            User = user,
            RoleId = role.Id,
            Role = role
        });

        var service = new AuthorizationService();

        // Act
        bool result = service.HasPermission(
            user,
            "consignment.process");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasPermission_ShouldReturnFalseForEmptyPermissionName()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true
        };

        var service = new AuthorizationService();

        // Act
        bool result = service.HasPermission(user, "");

        // Assert
        Assert.False(result);
    }
}
