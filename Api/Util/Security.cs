using System;
using System.Security.Cryptography;
using System.Text;

namespace Api.Util;

public class Security
{
    public static string Encrypt(string password)
    {
        var provider = SHA512.Create();
        string salt = "ThisSaltIsUncr@ble@-2300&^%$#@!";
        byte[] bytes = provider.ComputeHash(Encoding.UTF32.GetBytes(salt + password));
        var pass = BitConverter.ToString(bytes).Replace("-", "").ToLower();
        return pass;
    }
}
