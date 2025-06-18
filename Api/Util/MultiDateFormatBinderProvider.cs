namespace Api.Util;


using Microsoft.AspNetCore.Mvc.ModelBinding;

public class MultiDateFormatBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Only apply to DateTime, nullable DateTime, DateOnly, and nullable DateOnly
        if (context.Metadata.ModelType == typeof(DateTime) ||
            context.Metadata.ModelType == typeof(DateTime?) ||
            context.Metadata.ModelType == typeof(DateOnly) ||
            context.Metadata.ModelType == typeof(DateOnly?))
        {
            return new MultiDateFormatBinder();
        }

        return null;
    }
}