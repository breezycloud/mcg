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
