using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Client.Handlers;

class StringConverter
{
    public static string ConvertToTitleCase(string value)
    {
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
    }

    public static decimal ConvertToDecimal(string value)
    {
        return decimal.Parse(value);
    }

    public static string ConvertToMonth(int value)
    {
        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(value);
    }

    public static string ConvertToBase64(Stream stream)
    {
        byte[] bytes;
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            bytes = memoryStream.ToArray();
        }

        return Convert.ToBase64String(bytes);
    }
    private static readonly Dictionary<Enum, string> _descriptionCache = new();

    public static string ToDescription(Enum value)
    {
        if (_descriptionCache.TryGetValue(value, out var desc))
            return desc;

        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        desc = attribute != null ? attribute.Description : value.ToString();
        _descriptionCache[value] = desc;
        return desc;
    }
}