using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Checks whether a user has a given permission by looking up their role
/// in the in-memory RBAC service.
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly IRBACService _rbacService;

    public AuthorizationService(IRBACService rbacService)
    {
        _rbacService = rbacService;
    }

    public async Task<bool> HasPermissionAsync(int userId, PermissionModule module,
        PermissionAction action, CancellationToken ct = default)
    {
        var perms = await GetUserPermissionsAsync(userId, ct);
        return perms.Any(p => p.Module == module && p.Action == action);
    }

    public async Task<IReadOnlyList<AppPermission>> GetUserPermissionsAsync(int userId,
        CancellationToken ct = default)
    {
        var users = await _rbacService.GetAllUsersAsync(ct);
        var user  = users.FirstOrDefault(u => u.Id == userId);
        if (user is null) return Array.Empty<AppPermission>();

        return await GetRolePermissionsAsync(user.RoleId, ct);
    }

    public async Task<IReadOnlyList<AppPermission>> GetRolePermissionsAsync(int roleId,
        CancellationToken ct = default)
    {
        var rbac = (RBACService)_rbacService;
        return rbac.GetPermissionsForRole(roleId);
    }

    public async Task CheckAccessAsync(int userId, PermissionModule module,
        PermissionAction action, CancellationToken ct = default)
    {
        if (!await HasPermissionAsync(userId, module, action, ct))
            throw new UnauthorizedAccessException(
                $"User {userId} does not have permission {module}.{action}.");
    }
}
