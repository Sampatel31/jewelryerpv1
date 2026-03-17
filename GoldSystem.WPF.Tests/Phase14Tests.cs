using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using GoldSystem.WPF.Services;
using GoldSystem.WPF.ViewModels;
using Moq;

namespace GoldSystem.WPF.Tests;

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 14 – PasswordService Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class PasswordServiceTests
{
    private static readonly PasswordService _svc = new();

    [Fact]
    public void HashPassword_ReturnsNonEmptyHash()
    {
        var hash = _svc.HashPassword("Admin@1234");
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.Contains(":", hash);   // salt:hash separator
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var hash = _svc.HashPassword("MySecret@99");
        Assert.True(_svc.VerifyPassword("MySecret@99", hash));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _svc.HashPassword("CorrectHorse#1");
        Assert.False(_svc.VerifyPassword("WrongPassword", hash));
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ReturnsFalse()
    {
        var hash = _svc.HashPassword("Valid@1234");
        Assert.False(_svc.VerifyPassword(string.Empty, hash));
    }

    [Fact]
    public void HashPassword_SameInput_ProducesDifferentHashes()
    {
        // PBKDF2 with random salt means two hashes of the same password differ
        var h1 = _svc.HashPassword("SamePass@1");
        var h2 = _svc.HashPassword("SamePass@1");
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void GenerateOTP_ProducesCorrectLength()
    {
        var otp = _svc.GenerateOTP(6);
        Assert.Equal(6, otp.Length);
        Assert.True(otp.All(char.IsDigit));
    }

    [Fact]
    public void GenerateOTP_FourDigits_Works()
    {
        var otp = _svc.GenerateOTP(4);
        Assert.Equal(4, otp.Length);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 14 – Password Policy Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class PasswordPolicyTests
{
    private static readonly PasswordService _svc = new();
    private static readonly SecurityPolicy  _strict = new()
    {
        PasswordMinLength   = 8,
        RequireUppercase    = true,
        RequireLowercase    = true,
        RequireDigits       = true,
        RequireSpecialChars = true
    };

    [Fact]
    public void ValidatePolicy_ValidPassword_ReturnsTrue()
    {
        var (ok, msg) = _svc.ValidatePasswordPolicy("Str0ng@Pass", _strict);
        Assert.True(ok, msg);
    }

    [Fact]
    public void ValidatePolicy_TooShort_ReturnsFalse()
    {
        var (ok, msg) = _svc.ValidatePasswordPolicy("Ab1@", _strict);
        Assert.False(ok);
        Assert.Contains("at least", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePolicy_NoUppercase_ReturnsFalse()
    {
        var (ok, _) = _svc.ValidatePasswordPolicy("lowercase@1234", _strict);
        Assert.False(ok);
    }

    [Fact]
    public void ValidatePolicy_NoDigits_ReturnsFalse()
    {
        var (ok, _) = _svc.ValidatePasswordPolicy("NoDigits@Pass", _strict);
        Assert.False(ok);
    }

    [Fact]
    public void ValidatePolicy_NoSpecial_ReturnsFalse()
    {
        var (ok, _) = _svc.ValidatePasswordPolicy("NoSpecial1Abc", _strict);
        Assert.False(ok);
    }

    [Fact]
    public void ResetPassword_ValidPassword_ReturnsHash()
    {
        var hash = _svc.ResetPassword("Valid@1234", _strict);
        Assert.True(_svc.VerifyPassword("Valid@1234", hash));
    }

    [Fact]
    public void ResetPassword_InvalidPassword_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _svc.ResetPassword("weak", _strict));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 14 – RBACService Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class RBACServiceTests
{
    private static RBACService CreateRBAC()
    {
        return new RBACService(new PasswordService());
    }

    [Fact]
    public async Task GetAllRoles_ReturnsSeedRoles()
    {
        var svc   = CreateRBAC();
        var roles = await svc.GetAllRolesAsync();
        Assert.True(roles.Count >= 3);
        Assert.Contains(roles, r => r.Name == "Admin");
        Assert.Contains(roles, r => r.Name == "Manager");
        Assert.Contains(roles, r => r.Name == "Staff");
    }

    [Fact]
    public async Task GetAllPermissions_Returns30OrMore()
    {
        var svc   = CreateRBAC();
        var perms = await svc.GetAllPermissionsAsync();
        Assert.True(perms.Count >= 30);   // 6 modules × 6 actions = 36
    }

    [Fact]
    public async Task AdminRole_HasAllPermissions()
    {
        var svc   = CreateRBAC();
        var roles = await svc.GetAllRolesAsync();
        var admin = roles.First(r => r.Name == "Admin");
        var perms = await svc.GetAllPermissionsAsync();
        Assert.Equal(perms.Count, admin.PermissionIds.Count);
    }

    [Fact]
    public async Task CreateRole_AddsToList()
    {
        var svc  = CreateRBAC();
        var role = await svc.CreateRoleAsync("Accountant", "Handles accounts", Array.Empty<int>());
        var all  = await svc.GetAllRolesAsync();
        Assert.Contains(all, r => r.Name == "Accountant");
    }

    [Fact]
    public async Task UpdateRolePermissions_ChangesPermissions()
    {
        var svc   = CreateRBAC();
        var perms = await svc.GetAllPermissionsAsync();
        var roles = await svc.GetAllRolesAsync();
        var staff = roles.First(r => r.Name == "Staff");
        var newIds = new[] { perms[0].Id, perms[1].Id };

        await svc.UpdateRolePermissionsAsync(staff.Id, newIds);

        var updated = (await svc.GetAllRolesAsync()).First(r => r.Id == staff.Id);
        Assert.Equal(2, updated.PermissionIds.Count);
    }

    [Fact]
    public async Task CreateUser_AddsToList()
    {
        var svc   = CreateRBAC();
        var roles = await svc.GetAllRolesAsync();
        var hash  = new PasswordService().HashPassword("Test@1234");
        var user  = await svc.CreateUserAsync("jdoe", "jdoe@shop.com", hash, roles[0].Id);
        var all   = await svc.GetAllUsersAsync();
        Assert.Contains(all, u => u.Username == "jdoe");
    }

    [Fact]
    public async Task DeleteUser_RemovesFromList()
    {
        var svc   = CreateRBAC();
        var roles = await svc.GetAllRolesAsync();
        var hash  = new PasswordService().HashPassword("Test@1234");
        var user  = await svc.CreateUserAsync("toremove", "x@x.com", hash, roles[0].Id);
        await svc.DeleteUserAsync(user.Id);
        var all = await svc.GetAllUsersAsync();
        Assert.DoesNotContain(all, u => u.Id == user.Id);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 14 – AuthenticationService Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class AuthenticationServiceTests
{
    private static (AuthenticationService auth, AuditService audit, RBACService rbac) Build()
    {
        var pwdSvc  = new PasswordService();
        var rbac    = new RBACService(pwdSvc);
        var audit   = new AuditService();
        var policy  = new SecurityPolicy { MaxLoginAttempts = 5, LockoutDurationMinutes = 15 };
        var auth    = new AuthenticationService(pwdSvc, rbac, audit, policy);
        return (auth, audit, rbac);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        var (auth, _, _) = Build();
        var result = await auth.AuthenticateAsync("admin", "Admin@1234");
        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsFail()
    {
        var (auth, _, _) = Build();
        var result = await auth.AuthenticateAsync("admin", "WrongPass");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsFail()
    {
        var (auth, _, _) = Build();
        var result = await auth.AuthenticateAsync("ghost", "Any@Pass");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidateCredentials_CorrectPassword_ReturnsTrue()
    {
        var (auth, _, _) = Build();
        var ok = await auth.ValidateCredentialsAsync("admin", "Admin@1234");
        Assert.True(ok);
    }

    [Fact]
    public async Task ValidateCredentials_WrongPassword_ReturnsFalse()
    {
        var (auth, _, _) = Build();
        var ok = await auth.ValidateCredentialsAsync("admin", "BadPass");
        Assert.False(ok);
    }

    [Fact]
    public async Task Generate2FAOTP_ThenVerify_ReturnsSuccess()
    {
        var (auth, _, rbac) = Build();
        var users = await rbac.GetAllUsersAsync();
        var admin = users.First();

        // Enable 2FA
        admin.TwoFactorEnabled = true;
        await rbac.UpdateUserAsync(admin);

        var otp    = await auth.Generate2FAOTPAsync(admin.Id);
        var result = await auth.Verify2FAOTPAsync(admin.Id, otp);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Verify2FA_WrongOTP_ReturnsFail()
    {
        var (auth, _, rbac) = Build();
        var users = await rbac.GetAllUsersAsync();
        var admin = users.First();

        await auth.Generate2FAOTPAsync(admin.Id);
        var result = await auth.Verify2FAOTPAsync(admin.Id, "000000");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewToken()
    {
        var (auth, _, _) = Build();
        var loginResult  = await auth.AuthenticateAsync("admin", "Admin@1234");
        Assert.False(string.IsNullOrEmpty(loginResult.RefreshToken));

        var refreshResult = await auth.RefreshTokenAsync(loginResult.RefreshToken!);
        Assert.True(refreshResult.Success);
        Assert.False(string.IsNullOrEmpty(refreshResult.Token));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 14 – AuditService Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class AuditServiceTests
{
    [Fact]
    public async Task LogAction_ThenQuery_ReturnsEntry()
    {
        var svc = new AuditService();
        await svc.LogActionAsync(1, "UserCreated", "Security", "User", "42",
            newValue: "alice", ipAddress: "127.0.0.1");

        var logs = await svc.GetAuditLogsAsync();
        Assert.Single(logs);
        Assert.Equal("UserCreated", logs[0].Action);
        Assert.Equal("alice", logs[0].NewValue);
    }

    [Fact]
    public async Task FilterByAction_ReturnsOnlyMatching()
    {
        var svc = new AuditService();
        await svc.LogActionAsync(1, "LoginSuccess", "Security", "User", "1");
        await svc.LogActionAsync(2, "LoginFailed",  "Security", "User", "2");

        var logs = await svc.GetAuditLogsAsync(action: "LoginSuccess");
        Assert.Single(logs);
        Assert.Equal("LoginSuccess", logs[0].Action);
    }

    [Fact]
    public async Task FilterByModule_ReturnsOnlyMatching()
    {
        var svc = new AuditService();
        await svc.LogActionAsync(1, "Create", "Billing",   "Bill", "10");
        await svc.LogActionAsync(2, "Create", "Inventory", "Item", "20");

        var logs = await svc.GetAuditLogsAsync(module: "Billing");
        Assert.Single(logs);
        Assert.Equal("Billing", logs[0].Module);
    }

    [Fact]
    public async Task ExportAuditTrail_ReturnsNonEmptyBytes()
    {
        var svc = new AuditService();
        await svc.LogActionAsync(1, "Login", "Security", "User", "1");

        var bytes = await svc.ExportAuditTrailAsync(await svc.GetAuditLogsAsync());
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task CleanupOldLogs_RemovesOldEntries()
    {
        var svc = new AuditService();
        await svc.LogActionAsync(1, "OldAction", "Security", "User", "1");
        // Keep 0 days → all existing entries (all have UtcNow timestamp, not past)
        await svc.CleanupOldLogsAsync(keepDays: 0);
        var logs = await svc.GetAuditLogsAsync();
        // All entries have UtcNow timestamps so they're within 0-day cutoff boundary
        Assert.Empty(logs);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 14 – AuthorizationService Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class AuthorizationServiceTests
{
    private static (AuthorizationService authz, RBACService rbac) Build()
    {
        var rbac  = new RBACService(new PasswordService());
        var authz = new AuthorizationService(rbac);
        return (authz, rbac);
    }

    [Fact]
    public async Task AdminUser_HasAllPermissions()
    {
        var (authz, rbac) = Build();
        var users = await rbac.GetAllUsersAsync();
        var admin = users.First(u => u.Username == "admin");

        var ok = await authz.HasPermissionAsync(admin.Id, PermissionModule.Admin, PermissionAction.Delete);
        Assert.True(ok);
    }

    [Fact]
    public async Task GetUserPermissions_AdminHasAllPerms()
    {
        var (authz, rbac) = Build();
        var users = await rbac.GetAllUsersAsync();
        var admin = users.First(u => u.Username == "admin");

        var perms = await authz.GetUserPermissionsAsync(admin.Id);
        Assert.True(perms.Count >= 30);
    }

    [Fact]
    public async Task CheckAccess_AdminOnAdmin_DoesNotThrow()
    {
        var (authz, rbac) = Build();
        var users = await rbac.GetAllUsersAsync();
        var admin = users.First(u => u.Username == "admin");

        await authz.CheckAccessAsync(admin.Id, PermissionModule.Admin, PermissionAction.View);
        // No exception = pass
    }

    [Fact]
    public async Task CheckAccess_UnknownUser_Throws()
    {
        var (authz, _) = Build();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            authz.CheckAccessAsync(9999, PermissionModule.Admin, PermissionAction.Delete));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 14 – ViewModel Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class Phase14ViewModelTests
{
    // ── UserManagementViewModel ───────────────────────────────────────────────

    [Fact]
    public async Task UserManagementViewModel_Load_PopulatesUsers()
    {
        var rbac  = new RBACService(new PasswordService());
        var audit = new AuditService();
        var vm    = new UserManagementViewModel(rbac, new PasswordService(), audit);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.NotEmpty(vm.Users);
        Assert.NotEmpty(vm.Roles);
    }

    [Fact]
    public async Task UserManagementViewModel_CreateUser_AddsToList()
    {
        var rbac  = new RBACService(new PasswordService());
        var audit = new AuditService();
        var vm    = new UserManagementViewModel(rbac, new PasswordService(), audit);
        await vm.LoadCommand.ExecuteAsync(null);

        vm.NewUserCommand.Execute(null);
        vm.FormUsername = "newuser";
        vm.FormEmail    = "new@shop.com";
        vm.FormPassword = "Valid@1234";
        vm.FormRoleId   = vm.Roles.First().Id;

        await vm.SaveUserCommand.ExecuteAsync(null);

        Assert.Contains(vm.Users, u => u.Username == "newuser");
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task UserManagementViewModel_CreateUser_InvalidEmail_SetsHasError()
    {
        var rbac  = new RBACService(new PasswordService());
        var audit = new AuditService();
        var vm    = new UserManagementViewModel(rbac, new PasswordService(), audit);
        await vm.LoadCommand.ExecuteAsync(null);

        vm.NewUserCommand.Execute(null);
        vm.FormUsername = "baduser";
        vm.FormEmail    = "not-a-valid-email";
        vm.FormPassword = "Valid@1234";
        vm.FormRoleId   = vm.Roles.First().Id;

        await vm.SaveUserCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task UserManagementViewModel_DeleteUser_RemovesFromList()
    {
        var rbac  = new RBACService(new PasswordService());
        var audit = new AuditService();
        var vm    = new UserManagementViewModel(rbac, new PasswordService(), audit);
        await vm.LoadCommand.ExecuteAsync(null);

        var before = vm.Users.Count;
        var user   = vm.Users.First();
        await vm.DeleteUserCommand.ExecuteAsync(user);

        Assert.Equal(before - 1, vm.Users.Count);
    }

    // ── AuditTrailViewModel ───────────────────────────────────────────────────

    [Fact]
    public async Task AuditTrailViewModel_Load_InitiallyEmpty()
    {
        var svc = new AuditService();
        var vm  = new AuditTrailViewModel(svc);

        await vm.LoadLogsCommand.ExecuteAsync(null);

        Assert.Empty(vm.AuditLogs);
    }

    [Fact]
    public async Task AuditTrailViewModel_Load_AfterLog_ShowsEntry()
    {
        var svc = new AuditService();
        await svc.LogActionAsync(1, "LoginSuccess", "Security", "User", "1");

        var vm = new AuditTrailViewModel(svc);
        await vm.LoadLogsCommand.ExecuteAsync(null);

        Assert.Single(vm.AuditLogs);
        Assert.Equal(1, vm.TotalCount);
    }

    // ── SecuritySettingsViewModel ─────────────────────────────────────────────

    [Fact]
    public async Task SecuritySettingsViewModel_Save_IsValid()
    {
        var vm = new SecuritySettingsViewModel();
        Assert.True(vm.IsValid);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.HasError);
        Assert.Contains("saved", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecuritySettingsViewModel_BuildPolicy_ReflectsProperties()
    {
        var vm = new SecuritySettingsViewModel
        {
            PasswordMinLength = 12,
            RequireSpecialChars = true,
            MaxLoginAttempts = 3
        };

        var policy = vm.BuildPolicy();
        Assert.Equal(12, policy.PasswordMinLength);
        Assert.True(policy.RequireSpecialChars);
        Assert.Equal(3, policy.MaxLoginAttempts);
    }

    [Fact]
    public async Task SecuritySettingsViewModel_ResetToDefaults_RestoresDefaults()
    {
        var vm = new SecuritySettingsViewModel { PasswordMinLength = 20, MaxLoginAttempts = 99 };
        await vm.ResetToDefaultsCommand.ExecuteAsync(null);

        var defaults = new SecurityPolicy();
        Assert.Equal(defaults.PasswordMinLength, vm.PasswordMinLength);
        Assert.Equal(defaults.MaxLoginAttempts,  vm.MaxLoginAttempts);
    }

    // ── RBACViewModel ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RBACViewModel_Load_PopulatesRolesAndPermissions()
    {
        var rbac  = new RBACService(new PasswordService());
        var audit = new AuditService();
        var vm    = new RBACViewModel(rbac, audit);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.NotEmpty(vm.Roles);
        Assert.NotEmpty(vm.AllPermissions);
        Assert.NotEmpty(vm.PermissionMatrix);
    }

    [Fact]
    public async Task RBACViewModel_SavePermissions_DoesNotError()
    {
        var rbac  = new RBACService(new PasswordService());
        var audit = new AuditService();
        var vm    = new RBACViewModel(rbac, audit);

        await vm.LoadCommand.ExecuteAsync(null);

        await vm.SavePermissionsCommand.ExecuteAsync(null);

        Assert.False(vm.HasError);
    }
}
