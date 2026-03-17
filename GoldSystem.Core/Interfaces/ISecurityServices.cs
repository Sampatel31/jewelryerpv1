using GoldSystem.Core.Models;

namespace GoldSystem.Core.Interfaces;

// ─── IPasswordService ─────────────────────────────────────────────────────────

/// <summary>Handles password hashing, verification, OTP generation, and policy validation.</summary>
public interface IPasswordService
{
    /// <summary>Creates a salted PBKDF2 hash for storage.</summary>
    string HashPassword(string plainTextPassword);

    /// <summary>Verifies a plain-text password against a stored hash.</summary>
    bool VerifyPassword(string plainTextPassword, string storedHash);

    /// <summary>Validates a plain-text password against the current <see cref="SecurityPolicy"/>.</summary>
    (bool IsValid, string Message) ValidatePasswordPolicy(string password, SecurityPolicy policy);

    /// <summary>Generates a numeric OTP of the specified digit length.</summary>
    string GenerateOTP(int digits = 6);

    /// <summary>Resets the user's password hash (returns the new hash).</summary>
    string ResetPassword(string newPassword, SecurityPolicy policy);
}

// ─── IAuthenticationService ───────────────────────────────────────────────────

/// <summary>Handles user authentication including 2FA and JWT tokens.</summary>
public interface IAuthenticationService
{
    /// <summary>Authenticates username + password. Returns AuthResult with token or 2FA prompt.</summary>
    Task<AuthResult> AuthenticateAsync(string username, string password, string ipAddress = "", CancellationToken ct = default);

    /// <summary>Validates credentials without issuing a token.</summary>
    Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default);

    /// <summary>Generates and dispatches a 2FA OTP for the given user.</summary>
    Task<string> Generate2FAOTPAsync(int userId, CancellationToken ct = default);

    /// <summary>Verifies a 2FA OTP and completes authentication.</summary>
    Task<AuthResult> Verify2FAOTPAsync(int userId, string otp, CancellationToken ct = default);

    /// <summary>Issues a new access token from a valid refresh token.</summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Revokes/invalidates a token.</summary>
    Task RevokeTokenAsync(string token, CancellationToken ct = default);
}

// ─── IAuthorizationService ────────────────────────────────────────────────────

/// <summary>Checks what actions a user or role is permitted to perform.</summary>
public interface IAuthorizationService
{
    /// <summary>Returns true if the user has the specified permission.</summary>
    Task<bool> HasPermissionAsync(int userId, PermissionModule module, PermissionAction action, CancellationToken ct = default);

    /// <summary>Returns all permissions held by the user through their role.</summary>
    Task<IReadOnlyList<AppPermission>> GetUserPermissionsAsync(int userId, CancellationToken ct = default);

    /// <summary>Returns all permissions assigned to the given role.</summary>
    Task<IReadOnlyList<AppPermission>> GetRolePermissionsAsync(int roleId, CancellationToken ct = default);

    /// <summary>Checks whether a user can access a given resource and throws if not.</summary>
    Task CheckAccessAsync(int userId, PermissionModule module, PermissionAction action, CancellationToken ct = default);
}

// ─── IAuditService ────────────────────────────────────────────────────────────

/// <summary>Records and queries the audit trail.</summary>
public interface IAuditService
{
    /// <summary>Records an action in the audit trail.</summary>
    Task LogActionAsync(int userId, string action, string module, string entity, string entityId,
        string? oldValue = null, string? newValue = null,
        string ipAddress = "", string userAgent = "",
        CancellationToken ct = default);

    /// <summary>Returns audit logs filtered by optional criteria.</summary>
    Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(
        int?     userId     = null,
        string?  action     = null,
        string?  module     = null,
        DateTime? from      = null,
        DateTime? to        = null,
        int      maxRecords = 500,
        CancellationToken ct = default);

    /// <summary>Exports audit trail to Excel bytes.</summary>
    Task<byte[]> ExportAuditTrailAsync(
        IReadOnlyList<AuditLog> logs,
        CancellationToken ct = default);

    /// <summary>Removes logs older than <paramref name="keepDays"/> days.</summary>
    Task CleanupOldLogsAsync(int keepDays = 365, CancellationToken ct = default);
}

// ─── IRBACService ─────────────────────────────────────────────────────────────

/// <summary>Manages roles, permissions, and user-role assignments.</summary>
public interface IRBACService
{
    /// <summary>Returns all roles.</summary>
    Task<IReadOnlyList<AppRole>> GetAllRolesAsync(CancellationToken ct = default);

    /// <summary>Returns all available permissions.</summary>
    Task<IReadOnlyList<AppPermission>> GetAllPermissionsAsync(CancellationToken ct = default);

    /// <summary>Creates a new custom role.</summary>
    Task<AppRole> CreateRoleAsync(string name, string description, IEnumerable<int> permissionIds, CancellationToken ct = default);

    /// <summary>Updates the permission set for an existing role.</summary>
    Task UpdateRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds, CancellationToken ct = default);

    /// <summary>Assigns a role to a user.</summary>
    Task AssignRoleToUserAsync(int userId, int roleId, CancellationToken ct = default);

    /// <summary>Returns all users.</summary>
    Task<IReadOnlyList<AppUser>> GetAllUsersAsync(CancellationToken ct = default);

    /// <summary>Creates a new user.</summary>
    Task<AppUser> CreateUserAsync(string username, string email, string passwordHash, int roleId, CancellationToken ct = default);

    /// <summary>Updates an existing user.</summary>
    Task UpdateUserAsync(AppUser user, CancellationToken ct = default);

    /// <summary>Deletes a user.</summary>
    Task DeleteUserAsync(int userId, CancellationToken ct = default);
}
