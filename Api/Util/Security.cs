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

    public static string Encrypt(string password)
    {
        using var provider = SHA512.Create();
        string salt = "ThisSaltIsUncr@ble@-2300&^%$#@!";
        byte[] bytes = provider.ComputeHash(Encoding.UTF32.GetBytes(salt + password));
        var pass = BitConverter.ToString(bytes).Replace("-", "").ToLower();
        return pass;
    }

    public static string GenerateRandomPassword(int length = 12)
    {
        const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
        var res = new char[length];
        var rnd = new Random();
        for (int i = 0; i < length; i++)
        {
            res[i] = valid[rnd.Next(valid.Length)];
        }
        return new string(res);
    }
}
