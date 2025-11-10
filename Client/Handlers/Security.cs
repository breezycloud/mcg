using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Shared.Enums;

namespace Client.Handlers;

public static class Security
{
    public static string Encrypt(string password)
    {
        var provider = SHA512.Create();
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

    public static IReadOnlyList<Product> GetManagedProducts(ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(c => c.Type == "managed_products")?.Value;
        if (string.IsNullOrWhiteSpace(claim)) return Array.Empty<Product>();

        var list = new List<Product>();
        foreach (var token in claim.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<Product>(token, true, out var p))
                list.Add(p);
        }
        return list;
    }

    public static bool IsDriverSupervisor(this ClaimsPrincipal user) =>
        user.IsInRole(UserRole.DriverSupervisor.ToString());
}
