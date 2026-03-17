namespace GoldSystem.Core.Models;

// ─── Enumerations ────────────────────────────────────────────────────────────

/// <summary>Application modules that permissions can be scoped to.</summary>
public enum PermissionModule
{
    Billing,
    Inventory,
    Customers,
    Settings,
    Reports,
    Admin
}

/// <summary>Actions that can be granted per module.</summary>
public enum PermissionAction
{
    View,
    Create,
    Edit,
    Delete,
    Print,
    Export
}

/// <summary>User account status.</summary>
public enum UserStatus
{
    Active,
    Inactive,
    Locked,
    PendingActivation
}

/// <summary>Available two-factor authentication delivery methods.</summary>
public enum TwoFactorMethod
{
    None,
    Email,
    SMS,
    Authenticator
}

// ─── Core Security Models ─────────────────────────────────────────────────────

/// <summary>Application user account.</summary>
public sealed class AppUser
{
    public int          Id               { get; set; }
    public string       Username         { get; set; } = string.Empty;
    public string       Email            { get; set; } = string.Empty;
    public string       PasswordHash     { get; set; } = string.Empty;
    public int          RoleId           { get; set; }
    public bool         TwoFactorEnabled { get; set; } = false;
    public TwoFactorMethod TwoFactorMethod { get; set; } = TwoFactorMethod.None;
    public UserStatus   Status           { get; set; } = UserStatus.Active;
    public DateTime?    LastLogin        { get; set; }
    public DateTime     CreatedAt        { get; set; } = DateTime.UtcNow;
    public DateTime?    LockedUntil      { get; set; }

    /// <summary>Display-friendly label for the UI.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Username) ? Email : Username;
    public bool   IsLocked    => Status == UserStatus.Locked
                                 || (LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow);
}

/// <summary>Application role that groups permissions.</summary>
public sealed class AppRole
{
    public int              Id          { get; set; }
    public string           Name        { get; set; } = string.Empty;
    public string           Description { get; set; } = string.Empty;
    public List<int>        PermissionIds { get; set; } = new();
    public DateTime         CreatedAt   { get; set; } = DateTime.UtcNow;
    public bool             IsSystem    { get; set; } = false;
}

/// <summary>Granular permission for a module/action pair.</summary>
public sealed class AppPermission
{
    public int              Id          { get; set; }
    public string           Name        { get; set; } = string.Empty;
    public PermissionModule Module      { get; set; }
    public PermissionAction Action      { get; set; }
    public string           Description { get; set; } = string.Empty;

    public string DisplayKey => $"{Module}.{Action}";
}

/// <summary>Immutable audit log entry recording every user action.</summary>
public sealed record AuditLog(
    int      Id,
    int      UserId,
    string   Action,
    string   Module,
    string   Entity,
    string   EntityId,
    DateTime Timestamp,
    string?  OldValue,
    string?  NewValue,
    string   IpAddress,
    string   UserAgent)
{
    public string FormattedTimestamp => Timestamp.ToString("dd-MMM-yyyy HH:mm:ss");
}

/// <summary>Password and session security policy.</summary>
public sealed class SecurityPolicy
{
    public int    PasswordMinLength        { get; set; } = 8;
    public bool   RequireUppercase         { get; set; } = true;
    public bool   RequireLowercase         { get; set; } = true;
    public bool   RequireDigits            { get; set; } = true;
    public bool   RequireSpecialChars      { get; set; } = false;
    public int    PasswordExpiryDays       { get; set; } = 90;
    public int    PasswordMinAgeDays       { get; set; } = 1;
    public int    MaxLoginAttempts         { get; set; } = 5;
    public int    LockoutDurationMinutes   { get; set; } = 15;
    public int    SessionTimeoutMinutes    { get; set; } = 30;
    public bool   TwoFactorRequired        { get; set; } = false;
}

/// <summary>Tracks per-user login activity and lockout state.</summary>
public sealed class UserActivity
{
    public int       UserId         { get; set; }
    public DateTime? LastLoginTime  { get; set; }
    public string    LastLoginIp    { get; set; } = string.Empty;
    public int       LoginAttempts  { get; set; } = 0;
    public DateTime? IsLockedUntil  { get; set; }
    public int       FailedAttempts { get; set; } = 0;

    public bool IsCurrentlyLocked =>
        IsLockedUntil.HasValue && IsLockedUntil.Value > DateTime.UtcNow;
}

// ─── Auth DTOs ────────────────────────────────────────────────────────────────

/// <summary>Result returned from an authentication attempt.</summary>
public sealed class AuthResult
{
    public bool    Success          { get; init; }
    public string  Message          { get; init; } = string.Empty;
    public string? Token            { get; init; }
    public string? RefreshToken     { get; init; }
    public bool    RequiresTwoFactor { get; init; }
    public AppUser? User            { get; init; }
}

/// <summary>Lightweight DTO shown in the user grid.</summary>
public sealed record UserSummary(
    int    Id,
    string Username,
    string Email,
    string RoleName,
    UserStatus Status,
    DateTime?  LastLogin,
    bool   TwoFactorEnabled);

/// <summary>Lightweight DTO shown in the RBAC role list.</summary>
public sealed record RoleSummary(
    int    Id,
    string Name,
    string Description,
    int    UserCount,
    bool   IsSystem);

/// <summary>Represents a module-grouped permission row in the RBAC matrix.</summary>
public sealed class ModulePermissions
{
    public PermissionModule Module      { get; set; }
    public string           ModuleName  => Module.ToString();
    public bool             CanView     { get; set; }
    public bool             CanCreate   { get; set; }
    public bool             CanEdit     { get; set; }
    public bool             CanDelete   { get; set; }
    public bool             CanPrint    { get; set; }
    public bool             CanExport   { get; set; }
}
