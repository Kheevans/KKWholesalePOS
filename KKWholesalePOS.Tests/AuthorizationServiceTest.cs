
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests;

public class PermissionManagementServiceTests
{
    [Fact]
    public void CreatePermission_ShouldSavePermission()
    {
        // Arrange
        var permissionRepository = new FakePermissionRepository();
        var roleRepository = new FakeRoleRepository();

        var service = new PermissionManagementService(
            permissionRepository,
            roleRepository);

        // Act
        var permission = service.CreatePermission(
            "Consignment.Process",
            "Allows processing of consignments.");

        // Assert
        Assert.Equal("Consignment.Process", permission.Name);
        Assert.Contains(permission, permissionRepository.Permissions);
    }

    [Fact]
    public void CreatePermission_ShouldRejectEmptyName()
    {
        // Arrange
        var service = CreateService(out _, out _);

        // Act and Assert
        Assert.Throws<ArgumentException>(() =>
            service.CreatePermission("", "Description"));
    }

    [Fact]
    public void CreatePermission_ShouldRejectDuplicateName()
    {
        // Arrange
        var permissionRepository = new FakePermissionRepository();

        permissionRepository.Add(new Permission
        {
            Id = 1,
            Name = "Consignment.Process"
        });

        var service = new PermissionManagementService(
            permissionRepository,
            new FakeRoleRepository());

        // Act and Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.CreatePermission(
                "Consignment.Process",
                "Another description."));
    }

    [Fact]
    public void AssignPermissionToRole_ShouldAssignPermission()
    {
        // Arrange
        var permissionRepository = new FakePermissionRepository();
        var roleRepository = new FakeRoleRepository();

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

        permissionRepository.Add(permission);
        roleRepository.Add(role);

        var service = new PermissionManagementService(
            permissionRepository,
            roleRepository);

        // Act
        service.AssignPermissionToRole(1, 1);

        // Assert
        Assert.Contains(
            role.RolePermissions,
            rp => rp.PermissionId == 1);
    }

    [Fact]
    public void AssignPermissionToRole_ShouldNotCreateDuplicateAssignment()
    {
        // Arrange
        var permissionRepository = new FakePermissionRepository();
        var roleRepository = new FakeRoleRepository();

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

        permissionRepository.Add(permission);
        roleRepository.Add(role);

        var service = new PermissionManagementService(
            permissionRepository,
            roleRepository);

        // Act
        service.AssignPermissionToRole(1, 1);
        service.AssignPermissionToRole(1, 1);

        // Assert
        Assert.Single(role.RolePermissions);
    }

    [Fact]
    public void AssignPermissionToRole_ShouldRejectUnknownRole()
    {
        // Arrange
        var permissionRepository = new FakePermissionRepository();

        permissionRepository.Add(new Permission
        {
            Id = 1,
            Name = "Consignment.Process"
        });

        var service = new PermissionManagementService(
            permissionRepository,
            new FakeRoleRepository());

        // Act and Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.AssignPermissionToRole(99, 1));
    }

    [Fact]
    public void AssignPermissionToRole_ShouldRejectUnknownPermission()
    {
        // Arrange
        var roleRepository = new FakeRoleRepository();

        roleRepository.Add(new Role
        {
            Id = 1,
            Name = "Consignment Officer"
        });

        var service = new PermissionManagementService(
            new FakePermissionRepository(),
            roleRepository);

        // Act and Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.AssignPermissionToRole(1, 99));
    }

    [Fact]
    public void RemovePermissionFromRole_ShouldRemoveAssignment()
    {
        // Arrange
        var permissionRepository = new FakePermissionRepository();
        var roleRepository = new FakeRoleRepository();

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

        permissionRepository.Add(permission);
        roleRepository.Add(role);

        var service = new PermissionManagementService(
            permissionRepository,
            roleRepository);

        service.AssignPermissionToRole(1, 1);

        // Act
        service.RemovePermissionFromRole(1, 1);

        // Assert
        Assert.Empty(role.RolePermissions);
    }

    [Fact]
    public void GetRolePermissions_ShouldReturnAssignedPermissions()
    {
        // Arrange
        var permissionRepository = new FakePermissionRepository();
        var roleRepository = new FakeRoleRepository();

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

        permissionRepository.Add(permission);
        roleRepository.Add(role);

        var service = new PermissionManagementService(
            permissionRepository,
            roleRepository);

        service.AssignPermissionToRole(1, 1);

        // Act
        var permissions = service.GetRolePermissions(1);

        // Assert
        Assert.Single(permissions);
        Assert.Equal("Consignment.Process", permissions[0].Name);
    }

    private static PermissionManagementService CreateService(
        out FakePermissionRepository permissionRepository,
        out FakeRoleRepository roleRepository)
    {
        permissionRepository = new FakePermissionRepository();
        roleRepository = new FakeRoleRepository();

        return new PermissionManagementService(
            permissionRepository,
            roleRepository);
    }

    private class FakePermissionRepository : IPermissionRepository
    {
        public List<Permission> Permissions { get; } = new();

        private int _nextId = 1;

        public Permission? GetById(int permissionId)
        {
            return Permissions.FirstOrDefault(
                p => p.Id == permissionId);
        }

        public Permission? GetByName(string name)
        {
            return Permissions.FirstOrDefault(
                p => p.Name == name);
        }

        public List<Permission> GetAll()
        {
            return Permissions.OrderBy(p => p.Name).ToList();
        }

        public void Add(Permission permission)
        {
            if (permission.Id == 0)
            {
                permission.Id = _nextId++;
            }

            Permissions.Add(permission);
        }

        public void Update(Permission permission)
        {
            // Objects are stored by reference in this fake repository.
        }

        public void SaveChanges()
        {
            // No database is used in these tests.
        }
    }

    private class FakeRoleRepository : IRoleRepository
    {
        public List<Role> Roles { get; } = new();

        private int _nextId = 1;

        public Role? GetById(int roleId)
        {
            return Roles.FirstOrDefault(r => r.Id == roleId);
        }

        public Role? GetByName(string name)
        {
            return Roles.FirstOrDefault(r => r.Name == name);
        }

        public List<Role> GetAll()
        {
            return Roles.OrderBy(r => r.Name).ToList();
        }

        public void Add(Role role)
        {
            if (role.Id == 0)
            {
                role.Id = _nextId++;
            }

            Roles.Add(role);
        }

        public void Update(Role role)
        {
            // Objects are stored by reference in this fake repository.
        }

        public void SaveChanges()
        {
            // No database is used in these tests.
        }
    }
}
