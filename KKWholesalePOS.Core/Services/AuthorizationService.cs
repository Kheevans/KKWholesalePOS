using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class AuthorizationService
{
    public bool HasPermission(User user, string permissionName)
    {
        if (user == null || !user.IsActive ||
            string.IsNullOrWhiteSpace(permissionName))
        {
            return false;
        }

        return user.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Any(rp =>
                rp.Permission != null &&
                rp.Permission.Name.Equals(
                    permissionName,
                    StringComparison.OrdinalIgnoreCase));
    }
}