using System.Text.RegularExpressions;

namespace Shared.Helpers;

// Nigerian local-format phone numbers are stored as-is (e.g. "08066951596"),
// but the same subscriber number is sometimes entered without the leading
// trunk "0" (e.g. "8066951596"). Comparisons should treat these as the same
// number; storage format is left untouched.
public static class PhoneNumberHelper
{
    public static string NormalizeForComparison(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        var digits = Regex.Replace(phone, @"\D", "");
        return digits.Length > 10 ? digits[^10..] : digits;
    }

    public static bool AreSameNumber(string? a, string? b)
    {
        var normalizedA = NormalizeForComparison(a);
        var normalizedB = NormalizeForComparison(b);
        return normalizedA.Length > 0 && normalizedA == normalizedB;
    }
}
