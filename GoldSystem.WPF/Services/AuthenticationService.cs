using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Handles user login, 2FA OTP, and JWT-style token management.
/// Tokens are signed HMACSHA256 blobs: base64(header).base64(payload).base64(sig)
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IPasswordService   _passwordService;
    private readonly IRBACService       _rbacService;
    private readonly IAuditService      _auditService;
    private readonly SecurityPolicy     _policy;

    // In-memory stores for tokens and OTPs
    private readonly ConcurrentDictionary<string, (int UserId, DateTime Expiry)> _refreshTokens = new();
    private readonly ConcurrentDictionary<int,    (string Otp, DateTime Expiry)> _pendingOtps    = new();
    private readonly ConcurrentDictionary<string, byte> _revokedTokens = new();
    private readonly ConcurrentDictionary<int, UserActivity> _activities = new();

    private static readonly byte[] _signingKey =
        Encoding.UTF8.GetBytes("GoldSystemJWT_Phase14_SecretKey!#2026");

    public AuthenticationService(
        IPasswordService passwordService,
        IRBACService     rbacService,
        IAuditService    auditService,
        SecurityPolicy?  policy = null)
    {
        _passwordService = passwordService;
        _rbacService     = rbacService;
        _auditService    = auditService;
        _policy          = policy ?? new SecurityPolicy();
    }

    // ── Authenticate ──────────────────────────────────────────────────────────

    public async Task<AuthResult> AuthenticateAsync(
        string username, string password,
        string ipAddress    = "",
        CancellationToken ct = default)
    {
        var rbac = (RBACService)_rbacService;
        var user = rbac.FindByUsername(username);

        if (user is null)
        {
            await _auditService.LogActionAsync(0, "LoginFailed", "Security", "User", username,
                ipAddress: ipAddress, ct: ct);
            return Fail("Invalid username or password.");
        }

        // Lockout check
        var activity = GetOrCreateActivity(user.Id);
        if (activity.IsCurrentlyLocked)
            return Fail($"Account is locked until {activity.IsLockedUntil:HH:mm:ss}.");

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            activity.FailedAttempts++;
            activity.LoginAttempts++;
            if (activity.FailedAttempts >= _policy.MaxLoginAttempts)
            {
                activity.IsLockedUntil = DateTime.UtcNow.AddMinutes(_policy.LockoutDurationMinutes);
                user.Status      = UserStatus.Locked;
                user.LockedUntil = activity.IsLockedUntil;
                await _auditService.LogActionAsync(user.Id, "AccountLocked", "Security", "User",
                    user.Id.ToString(), ipAddress: ipAddress, ct: ct);
            }

            await _auditService.LogActionAsync(user.Id, "LoginFailed", "Security", "User",
                user.Id.ToString(), ipAddress: ipAddress, ct: ct);
            return Fail("Invalid username or password.");
        }

        if (user.Status == UserStatus.Inactive)
            return Fail("Account is inactive. Contact your administrator.");

        // Reset failed attempts on success
        activity.FailedAttempts = 0;
        activity.LastLoginTime  = DateTime.UtcNow;
        activity.LastLoginIp    = ipAddress;
        user.LastLogin = DateTime.UtcNow;

        if (user.TwoFactorEnabled)
        {
            var otp = await Generate2FAOTPAsync(user.Id, ct);
            return new AuthResult
            {
                Success           = false,
                RequiresTwoFactor = true,
                Message           = $"2FA OTP generated: {otp}",
                User              = user
            };
        }

        var (token, refresh) = GenerateTokenPair(user);
        await _auditService.LogActionAsync(user.Id, "LoginSuccess", "Security", "User",
            user.Id.ToString(), ipAddress: ipAddress, ct: ct);

        return new AuthResult
        {
            Success      = true,
            Message      = "Login successful.",
            Token        = token,
            RefreshToken = refresh,
            User         = user
        };
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password,
        CancellationToken ct = default)
    {
        var rbac = (RBACService)_rbacService;
        var user = rbac.FindByUsername(username);
        return user is not null && _passwordService.VerifyPassword(password, user.PasswordHash);
    }

    // ── 2FA ───────────────────────────────────────────────────────────────────

    public Task<string> Generate2FAOTPAsync(int userId, CancellationToken ct = default)
    {
        var otp = _passwordService.GenerateOTP(6);
        _pendingOtps[userId] = (otp, DateTime.UtcNow.AddMinutes(10));
        return Task.FromResult(otp);
    }

    public async Task<AuthResult> Verify2FAOTPAsync(int userId, string otp,
        CancellationToken ct = default)
    {
        if (!_pendingOtps.TryGetValue(userId, out var pending))
            return Fail("No pending 2FA request.");

        if (DateTime.UtcNow > pending.Expiry)
        {
            _pendingOtps.TryRemove(userId, out _);
            return Fail("OTP expired. Please log in again.");
        }

        if (!string.Equals(pending.Otp, otp, StringComparison.Ordinal))
            return Fail("Invalid OTP.");

        _pendingOtps.TryRemove(userId, out _);

        var rbac = (RBACService)_rbacService;
        var user = rbac.FindById(userId);
        if (user is null) return Fail("User not found.");

        var (token, refresh) = GenerateTokenPair(user);
        await _auditService.LogActionAsync(userId, "2FASuccess", "Security", "User",
            userId.ToString(), ct: ct);

        return new AuthResult
        {
            Success      = true,
            Message      = "2FA verification successful.",
            Token        = token,
            RefreshToken = refresh,
            User         = user
        };
    }

    // ── Token Refresh / Revoke ────────────────────────────────────────────────

    public Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var entry))
            return Task.FromResult(Fail("Invalid or expired refresh token."));

        if (DateTime.UtcNow > entry.Expiry)
        {
            _refreshTokens.TryRemove(refreshToken, out _);
            return Task.FromResult(Fail("Refresh token expired. Please log in again."));
        }

        var rbac = (RBACService)_rbacService;
        var user = rbac.FindById(entry.UserId);
        if (user is null)
            return Task.FromResult(Fail("User not found."));

        _refreshTokens.TryRemove(refreshToken, out _);
        var (token, newRefresh) = GenerateTokenPair(user);
        return Task.FromResult(new AuthResult
        {
            Success      = true,
            Message      = "Token refreshed.",
            Token        = token,
            RefreshToken = newRefresh,
            User         = user
        });
    }

    public Task RevokeTokenAsync(string token, CancellationToken ct = default)
    {
        _revokedTokens.TryAdd(token, 0);
        return Task.CompletedTask;
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private (string token, string refresh) GenerateTokenPair(AppUser user)
    {
        var payload  = $"{user.Id}|{user.Username}|{user.RoleId}|{DateTime.UtcNow.AddHours(8):O}";
        var token    = CreateSignedToken(payload);

        var rawRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _refreshTokens[rawRefresh] = (user.Id, DateTime.UtcNow.AddDays(7));

        return (token, rawRefresh);
    }

    private static string CreateSignedToken(string payload)
    {
        var header     = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\"}"));
        var body       = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        var toSign     = $"{header}.{body}";
        using var hmac = new HMACSHA256(_signingKey);
        var sig        = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign)));
        return $"{toSign}.{sig}";
    }

    private UserActivity GetOrCreateActivity(int userId) =>
        _activities.GetOrAdd(userId, id => new UserActivity { UserId = id });

    private static AuthResult Fail(string msg) =>
        new() { Success = false, Message = msg };
}
