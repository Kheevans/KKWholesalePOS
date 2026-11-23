
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests.Services;

public class RoleManagementServiceTests
{
    [Fact]
    public void CreateRole_ShouldSaveRole()
    {
        var roleRepository = new FakeRoleRepository();
        var userRepository = new FakeUserRepository();

        var service = CreateAuthorizedService(
            roleRepository,
            userRepository);

        var role = service.CreateRole(
            "Manager",
            "Manages daily business operations.");

        Assert.Equal("Manager", role.Name);
        Assert.Equal(
            "Manages daily business operations.",
            role.Description);

        Assert.Contains(role, roleRepository.Roles);
    }

    [Fact]
    public void CreateRole_ShouldRejectEmptyName()
    {
        var service = CreateAuthorizedService(
            out _,
            out _);

        Assert.Throws<ArgumentException>(() =>
            service.CreateRole("", "Description"));
    }

    [Fact]
    public void CreateRole_ShouldRejectDuplicateName()
    {
        var roleRepository = new FakeRoleRepository();

        roleRepository.Add(new Role
        {
            Id = 1,
            Name = "Manager"
        });

        var service = CreateAuthorizedService(
            roleRepository,
            new FakeUserRepository());

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateRole("Manager", "Another description."));
    }

    [Fact]
    public void AssignRoleToUser_ShouldAssignExistingRole()
    {
        var roleRepository = new FakeRoleRepository();
        var userRepository = new FakeUserRepository();

        userRepository.Add(new User
        {
            Id = 1,
            Username = "testuser"
        });

        var role = new Role
        {
            Id = 1,
            Name = "Manager"
        };

        roleRepository.Add(role);
        userRepository.AddRole(role);

        var service = CreateAuthorizedService(
            roleRepository,
            userRepository);

        service.AssignRoleToUser(1, 1);

        Assert.Contains(
            userRepository.GetUserRoles(1),
            assignedRole => assignedRole.Id == 1);
    }

    [Fact]
    public void AssignRoleToUser_ShouldRejectUnknownUser()
    {
        var roleRepository = new FakeRoleRepository();

        roleRepository.Add(new Role
        {
            Id = 1,
            Name = "Manager"
        });

        var service = CreateAuthorizedService(
            roleRepository,
            new FakeUserRepository());

        Assert.Throws<InvalidOperationException>(() =>
            service.AssignRoleToUser(99, 1));
    }

    [Fact]
    public void AssignRoleToUser_ShouldRejectUnknownRole()
    {
        var userRepository = new FakeUserRepository();

        userRepository.Add(new User
        {
            Id = 1,
            Username = "testuser"
        });

        var service = CreateAuthorizedService(
            new FakeRoleRepository(),
            userRepository);

        Assert.Throws<InvalidOperationException>(() =>
            service.AssignRoleToUser(1, 99));
    }

    [Fact]
    public void RemoveRoleFromUser_ShouldRemoveAssignedRole()
    {
        var roleRepository = new FakeRoleRepository();
        var userRepository = new FakeUserRepository();

        userRepository.Add(new User
        {
            Id = 1,
            Username = "testuser"
        });

        var role = new Role
        {
            Id = 1,
            Name = "Manager"
        };

        roleRepository.Add(role);
        userRepository.AddRole(role);
        userRepository.AssignRole(1, 1);

        var service = CreateAuthorizedService(
            roleRepository,
            userRepository);

        service.RemoveRoleFromUser(1, 1);

        Assert.Empty(userRepository.GetUserRoles(1));
    }

    [Fact]
    public void GetUserRoles_ShouldReturnAssignedRoles()
    {
        var roleRepository = new FakeRoleRepository();
        var userRepository = new FakeUserRepository();

        userRepository.Add(new User
        {
            Id = 1,
            Username = "testuser"
        });

        var role = new Role
        {
            Id = 1,
            Name = "Manager"
        };

        roleRepository.Add(role);
        userRepository.AddRole(role);
        userRepository.AssignRole(1, 1);

        var service = CreateAuthorizedService(
            roleRepository,
            userRepository);

        var roles = service.GetUserRoles(1);

        Assert.Single(roles);
        Assert.Equal("Manager", roles[0].Name);
    }

    [Fact]
    public void CreateRole_WithoutPermission_ShouldThrowUnauthorizedAccessException()
    {
        var service = CreateService(out _, out _);

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.CreateRole("Manager", "Description"));
    }

    private static RoleManagementService CreateService(
        out FakeRoleRepository roleRepository,
        out FakeUserRepository userRepository)
    {
        roleRepository = new FakeRoleRepository();
        userRepository = new FakeUserRepository();

        return new RoleManagementService(
            roleRepository,
            userRepository,
            new UserSessionService(),
            new AuthorizationService());
    }

    private static RoleManagementService CreateAuthorizedService(
        FakeRoleRepository roleRepository,
        FakeUserRepository userRepository)
    {
        var userSession = new UserSessionService();

        var user = new User
        {
            Id = 1,
            Username = "testuser",
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
                                    Name = "Roles.Manage"
                                }
                            }
                        }
                    }
                }
            }
        };

        userSession.StartSession(user);

        return new RoleManagementService(
            roleRepository,
            userRepository,
            userSession,
            new AuthorizationService());
    }

    private static RoleManagementService CreateAuthorizedService(
        out FakeRoleRepository roleRepository,
        out FakeUserRepository userRepository)
    {
        roleRepository = new FakeRoleRepository();
        userRepository = new FakeUserRepository();

        return CreateAuthorizedService(
            roleRepository,
            userRepository);
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

    private class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();
        private readonly List<(int UserId, int RoleId)> _assignments = new();
        private readonly List<Role> _roles = new();

        public List<User> GetAll()
        {
            return new List<User>();
        }

        public User? GetByUsername(string username)
        {
            return _users.FirstOrDefault(
                u => u.Username == username);
        }

        public User? GetById(int userId)
        {
            return _users.FirstOrDefault(
                u => u.Id == userId);
        }

        public bool HasAnyUsers()
        {
            return _users.Any();
        }

        public void Add(User user)
        {
            _users.Add(user);
        }

        public void Update(User user)
        {
            // Objects are stored by reference in this fake repository.
        }

        public void SaveChanges()
        {
            // No database is used in these tests.
        }

        public void AssignRole(int userId, int roleId)
        {
            if (!_assignments.Contains((userId, roleId)))
            {
                _assignments.Add((userId, roleId));
            }
        }

        public void RemoveRole(int userId, int roleId)
        {
            _assignments.Remove((userId, roleId));
        }

        public List<Role> GetUserRoles(int userId)
        {
            return _assignments
                .Where(a => a.UserId == userId)
                .Select(a => _roles.FirstOrDefault(
                    r => r.Id == a.RoleId))
                .Where(r => r != null)
                .Cast<Role>()
                .ToList();
        }

        public void AddRole(Role role)
        {
            _roles.Add(role);
        }
    }
}
