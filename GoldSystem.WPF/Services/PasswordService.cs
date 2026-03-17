using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Implements password hashing (PBKDF2/SHA-256), OTP generation,
/// and password-policy validation without any external NuGet dependencies.
/// </summary>
public sealed class PasswordService : IPasswordService
{
    private const int SaltSize       = 16;   // 128-bit salt
    private const int HashSize       = 32;   // 256-bit output
    private const int Iterations     = 100_000;
    private const char Separator     = ':';

    // ── Hash / Verify ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string HashPassword(string plainTextPassword)
    {
        if (string.IsNullOrEmpty(plainTextPassword))
            throw new ArgumentException("Password must not be empty.", nameof(plainTextPassword));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Pbkdf2(plainTextPassword, salt);

        return $"{Convert.ToBase64String(salt)}{Separator}{Convert.ToBase64String(hash)}";
    }

    /// <inheritdoc/>
    public bool VerifyPassword(string plainTextPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(plainTextPassword) || string.IsNullOrEmpty(storedHash))
            return false;

        var parts = storedHash.Split(Separator);
        if (parts.Length != 2) return false;

        try
        {
            var salt         = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            var actualHash   = Pbkdf2(plainTextPassword, salt);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }

    // ── Policy Validation ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public (bool IsValid, string Message) ValidatePasswordPolicy(string password, SecurityPolicy policy)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Password must not be empty.");

        if (password.Length < policy.PasswordMinLength)
            return (false, $"Password must be at least {policy.PasswordMinLength} characters long.");

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
            return (false, "Password must contain at least one uppercase letter.");

        if (policy.RequireLowercase && !password.Any(char.IsLower))
            return (false, "Password must contain at least one lowercase letter.");

        if (policy.RequireDigits && !password.Any(char.IsDigit))
            return (false, "Password must contain at least one digit.");

        if (policy.RequireSpecialChars && !password.Any(c => !char.IsLetterOrDigit(c)))
            return (false, "Password must contain at least one special character.");

        return (true, "Password meets all requirements.");
    }

    // ── OTP ───────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string GenerateOTP(int digits = 6)
    {
        if (digits is < 4 or > 10)
            throw new ArgumentOutOfRangeException(nameof(digits), "OTP must be 4–10 digits.");

        var max   = (int)Math.Pow(10, digits);
        var value = RandomNumberGenerator.GetInt32(max);
        return value.ToString($"D{digits}");
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string ResetPassword(string newPassword, SecurityPolicy policy)
    {
        var (isValid, msg) = ValidatePasswordPolicy(newPassword, policy);
        if (!isValid)
            throw new InvalidOperationException($"Password does not meet policy: {msg}");

        return HashPassword(newPassword);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static byte[] Pbkdf2(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
    }
}
