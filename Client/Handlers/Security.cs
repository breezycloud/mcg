using System;
using System.Security.Claims;
using Shared.Enums;

namespace Client.Handlers;

public static class Security
{
    // Password hashing happens server-side only now (Api/Util/Security.HashPassword) — this used
    // to duplicate the server's hash function here with a hardcoded salt baked into the WASM
    // bundle every browser downloads, which let anyone forge a valid hash for any chosen
    // password offline. See Client/Pages/Users/MyProfile.razor's HandlePasswordChange, which now
    // sends the plaintext new password to Auth/change-password instead.

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
