using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Collections.Concurrent;

namespace GoldSystem.WPF.Services;

/// <summary>
/// In-memory RBAC service seeded with 3 system roles (Admin, Manager, Staff)
/// and 30 granular permissions across 6 modules.
/// </summary>
public sealed class RBACService : IRBACService
{
    private readonly IPasswordService _passwordService;

    // ── In-memory stores ──────────────────────────────────────────────────────
    private readonly List<AppPermission>            _permissions;
    private readonly List<AppRole>                  _roles;
    private readonly List<AppUser>                  _users;
    private          int _nextPermissionId = 1;
    private          int _nextRoleId       = 1;
    private          int _nextUserId       = 1;

    public RBACService(IPasswordService passwordService)
    {
        _passwordService = passwordService;
        _permissions     = BuildPermissions();
        _roles           = BuildRoles(_permissions);
        _users           = BuildDefaultUsers(_roles);
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<AppUser>> GetAllUsersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppUser>>(_users.AsReadOnly());

    public Task<AppUser> CreateUserAsync(string username, string email, string passwordHash, int roleId,
        CancellationToken ct = default)
    {
        var user = new AppUser
        {
            Id           = _nextUserId++,
            Username     = username,
            Email        = email,
            PasswordHash = passwordHash,
            RoleId       = roleId,
            CreatedAt    = DateTime.UtcNow,
            Status       = UserStatus.Active
        };
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task UpdateUserAsync(AppUser user, CancellationToken ct = default)
    {
        var idx = _users.FindIndex(u => u.Id == user.Id);
        if (idx >= 0) _users[idx] = user;
        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(int userId, CancellationToken ct = default)
    {
        _users.RemoveAll(u => u.Id == userId);
        return Task.CompletedTask;
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<AppRole>> GetAllRolesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppRole>>(_roles.AsReadOnly());

    public Task<AppRole> CreateRoleAsync(string name, string description,
        IEnumerable<int> permissionIds, CancellationToken ct = default)
    {
        var role = new AppRole
        {
            Id            = _nextRoleId++,
            Name          = name,
            Description   = description,
            PermissionIds = permissionIds.ToList(),
            CreatedAt     = DateTime.UtcNow,
            IsSystem      = false
        };
        _roles.Add(role);
        return Task.FromResult(role);
    }

    public Task UpdateRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds,
        CancellationToken ct = default)
    {
        var role = _roles.FirstOrDefault(r => r.Id == roleId);
        if (role is not null)
            role.PermissionIds = permissionIds.ToList();
        return Task.CompletedTask;
    }

    public Task AssignRoleToUserAsync(int userId, int roleId, CancellationToken ct = default)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user is not null)
            user.RoleId = roleId;
        return Task.CompletedTask;
    }

    // ── Permissions ───────────────────────────────────────────────────────────

    public Task<IReadOnlyList<AppPermission>> GetAllPermissionsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppPermission>>(_permissions.AsReadOnly());

    // ── Internal Helpers ──────────────────────────────────────────────────────

    internal AppUser? FindByUsername(string username) =>
        _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    internal AppUser? FindById(int id) =>
        _users.FirstOrDefault(u => u.Id == id);

    internal AppRole? FindRole(int roleId) =>
        _roles.FirstOrDefault(r => r.Id == roleId);

    internal IReadOnlyList<AppPermission> GetPermissionsForRole(int roleId)
    {
        var role = FindRole(roleId);
        if (role is null) return Array.Empty<AppPermission>();
        return _permissions.Where(p => role.PermissionIds.Contains(p.Id)).ToList();
    }

    // ── Seed Data ─────────────────────────────────────────────────────────────

    private List<AppPermission> BuildPermissions()
    {
        var list  = new List<AppPermission>();
        var id    = _nextPermissionId;
        var mods  = Enum.GetValues<PermissionModule>();
        var acts  = Enum.GetValues<PermissionAction>();

        foreach (var mod in mods)
        {
            foreach (var act in acts)
            {
                list.Add(new AppPermission
                {
                    Id          = id++,
                    Name        = $"{mod}.{act}",
                    Module      = mod,
                    Action      = act,
                    Description = $"Allow {act} on {mod} module"
                });
            }
        }

        _nextPermissionId = id;
        return list;
    }

    private List<AppRole> BuildRoles(List<AppPermission> permissions)
    {
        var allIds = permissions.Select(p => p.Id).ToList();

        // Admin: all permissions
        var adminRole = new AppRole
        {
            Id            = _nextRoleId++,
            Name          = "Admin",
            Description   = "Full system access",
            PermissionIds = allIds,
            CreatedAt     = DateTime.UtcNow,
            IsSystem      = true
        };

        // Manager: all except Admin module Delete/Edit
        var managerIds = permissions
            .Where(p => !(p.Module == PermissionModule.Admin
                          && p.Action is PermissionAction.Delete or PermissionAction.Edit))
            .Select(p => p.Id).ToList();

        var managerRole = new AppRole
        {
            Id            = _nextRoleId++,
            Name          = "Manager",
            Description   = "Manages daily operations; limited admin access",
            PermissionIds = managerIds,
            CreatedAt     = DateTime.UtcNow,
            IsSystem      = true
        };

        // Staff: View/Create/Print on Billing/Inventory/Customers
        var staffModules = new[] { PermissionModule.Billing, PermissionModule.Inventory, PermissionModule.Customers };
        var staffActions = new[] { PermissionAction.View, PermissionAction.Create, PermissionAction.Print };
        var staffIds = permissions
            .Where(p => staffModules.Contains(p.Module) && staffActions.Contains(p.Action))
            .Select(p => p.Id).ToList();

        var staffRole = new AppRole
        {
            Id            = _nextRoleId++,
            Name          = "Staff",
            Description   = "Day-to-day billing and inventory access",
            PermissionIds = staffIds,
            CreatedAt     = DateTime.UtcNow,
            IsSystem      = true
        };

        return new List<AppRole> { adminRole, managerRole, staffRole };
    }

    private List<AppUser> BuildDefaultUsers(List<AppRole> roles)
    {
        var adminRole = roles.First(r => r.Name == "Admin");
        var adminHash = _passwordService.HashPassword("Admin@1234");

        var admin = new AppUser
        {
            Id               = _nextUserId++,
            Username         = "admin",
            Email            = "admin@goldstore.com",
            PasswordHash     = adminHash,
            RoleId           = adminRole.Id,
            TwoFactorEnabled = false,
            Status           = UserStatus.Active,
            CreatedAt        = DateTime.UtcNow
        };

        return new List<AppUser> { admin };
    }
}
