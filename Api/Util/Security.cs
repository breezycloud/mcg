using System;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Shared.Enums;

namespace Api.Util;

public static class Security
{
    public const string ManagedProductsClaim = "managed_products"; // comma-separated product names

    public static bool IsInRole(this ClaimsPrincipal user, UserRole role) =>
        user.IsInRole(role.ToString());

    public static bool IsDriverSupervisor(this ClaimsPrincipal user) =>
        user.IsInRole(UserRole.DriverSupervisor);

    public static HashSet<Product> GetManagedProducts(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ManagedProductsClaim);
        var set = new HashSet<Product>();
        if (string.IsNullOrWhiteSpace(raw)) return set;

        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Converter.TryParseProduct(token, out var p))
                set.Add(p);
        }
        return set;
    }

    private const int Pbkdf2IterationCount = 210_000;
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private const string LegacySalt = "ThisSaltIsUncr@ble@-2300&^%$#@!";

    // Every password is now hashed with PBKDF2-HMACSHA256, a random per-user salt, and a real
    // iteration count — replaces a single SHA-512 round over one hardcoded salt shared by every
    // account, which was also duplicated verbatim into the WASM bundle shipped to every browser
    // (readable by anyone, and enough to forge a valid hash for a chosen password offline).
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2IterationCount, HashAlgorithmName.SHA256, KeySizeBytes);
        return $"v2:{Pbkdf2IterationCount}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
    }

    // Verifies against either scheme so already-issued hashes keep working. Callers that get a
    // true result back from a legacy-format stored hash should immediately re-save
    // HashPassword(password) over it — see Auth.Login — so every account is transparently
    // upgraded on its next successful login instead of forcing a mass password reset.
    public static bool VerifyPassword(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        if (storedHash.StartsWith("v2:", StringComparison.Ordinal))
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
                return false;

            byte[] salt, expectedKey;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedKey = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);
            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }

        return FixedTimeStringEquals(LegacyHash(password), storedHash);
    }

    public static bool IsLegacyHash(string? storedHash) =>
        !string.IsNullOrEmpty(storedHash) && !storedHash.StartsWith("v2:", StringComparison.Ordinal);

    // Verification-only — never used to create new hashes. Kept solely so accounts still
    // carrying an old hash can log in one more time and get upgraded by VerifyPassword's caller.
    private static string LegacyHash(string password)
    {
        using var provider = SHA512.Create();
        byte[] bytes = provider.ComputeHash(Encoding.UTF32.GetBytes(LegacySalt + password));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static bool FixedTimeStringEquals(string a, string b) =>
        a.Length == b.Length && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    public static string GenerateRandomPassword(int length = 12)
    {
        const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
        var res = new char[length];
        for (int i = 0; i < length; i++)
        {
            res[i] = valid[RandomNumberGenerator.GetInt32(valid.Length)];
        }
        return new string(res);
    }

    public static string GenerateResetToken()
    {
        var tokenBytes = new byte[32];
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    // 512 bits of randomness — the token itself carries enough entropy that, unlike a password,
    // it needs no salt or iteration count. Only its hash is ever persisted (see HashToken), so a
    // database read/leak alone never yields a usable refresh token.
    public static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    // Deterministic (unsalted) by design — refresh-token lookup has to find the matching row by
    // exact hash, unlike password verification which recomputes against a known candidate. Safe
    // specifically because the input already has 512 bits of entropy from GenerateRefreshToken;
    // this must never be reused for hashing anything an attacker could feasibly enumerate.
    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
