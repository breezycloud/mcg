using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

public class ApiKeyAuthFilter : IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthFilter> _logger;

    public ApiKeyAuthFilter(IConfiguration configuration, ILogger<ApiKeyAuthFilter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey)
            || string.IsNullOrWhiteSpace(extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Unauthorized." });
            return;
        }

        string apiKeyValue = extractedApiKey.ToString();

        var validKeys = _configuration.GetSection("ExternalApiKeys:ValidKeys").Get<string[]>();

        if (validKeys is null || validKeys.Length == 0)
        {
            _logger.LogError("ExternalApiKeys:ValidKeys is not configured. Denying all requests.");
            context.Result = new ObjectResult(new { error = "Service unavailable." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        // Constant-time comparison prevents timing-based enumeration of valid keys.
        // The length guard is required because FixedTimeEquals demands equal-length spans.
        bool isValid = validKeys.Any(k =>
            k.Length == apiKeyValue.Length &&
            CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(k),
                System.Text.Encoding.UTF8.GetBytes(apiKeyValue)
            )
        );

        if (!isValid)
        {
            string maskedKey = apiKeyValue.Length >= 4
                ? new string('*', apiKeyValue.Length - 4) + apiKeyValue[^4..]
                : "****";

            _logger.LogWarning("Invalid API key attempt. MaskedKey={MaskedKey} IP={IpAddress}",
                maskedKey,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

            context.Result = new UnauthorizedObjectResult(new { error = "Unauthorized." });
            return;
        }

        context.HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.HttpContext.Response.Headers["Cache-Control"] = "no-store";

        await next();
    }
}
