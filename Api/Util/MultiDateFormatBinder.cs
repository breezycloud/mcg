using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Util;
public class MultiDateFormatBinder : IModelBinder
{
   private static readonly string[] SupportedFormats =
    {
        "yyyy-MM-dd",    // ISO 8601
        "dd/MM/yyyy",    // European format
        "MM/dd/yyyy",    // US format
        "yyyyMMdd",      // Compact format
        "dd-MM-yyyy",     // Alternative European
        "MM-dd-yyyy",
        "M/d/yyyy",
        "M-d-yyyy"
    };

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.FieldName).FirstValue;
        if (string.IsNullOrEmpty(value))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (DateOnly.TryParseExact(value, SupportedFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.FieldName,
                $"Invalid date format. Supported formats: {string.Join(", ", SupportedFormats)}");
        }

        return Task.CompletedTask;
    }
}