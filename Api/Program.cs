using System.Configuration;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Api.Context;
using Api.Data;
using Api.Filters;
using Api.Hubs;
using Api.Interceptors;
using Shared.Hubs;
using Api.Logging;
using Api.Services.ControlRoom;
using Api.Services.Dashboards;
using Api.Services.Discharges;
using Api.Services.Messages;
using Api.Services.Drivers;
using Api.Services.Stations;
using Api.Services.Trucks;
using Api.Util;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.MailKitSmtp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RazorLight;
using Shared.Helpers;
using Shared.Interfaces.ControlRoom;
using Shared.Interfaces.Dashboards;
using Shared.Interfaces.Drivers;
using Shared.Interfaces.Stations;
using Shared.Interfaces.Trucks;
using Shared.Models.MessageBroker;

var builder = WebApplication.CreateBuilder(args);


brevo_csharp.Client.Configuration.Default.ApiKey.Add("api-key", builder.Configuration["Brevo:Makulli"]!);
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.Configure<MessageBrokerSetting>(builder.Configuration?.GetSection("RabbitMQ"));
var key = Encoding.ASCII.GetBytes(builder.Configuration["App:Key"]!);
var jwtIssuer = builder.Configuration["App:Issuer"]!;
var jwtAudience = builder.Configuration["App:Audience"]!;
var uploadsPublicUrl = builder.Configuration["FileStorage:PublicUrl"]!;
string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
            policy =>
            {
                policy.WithOrigins(allowedOrigins)
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
});
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new MultiDateFormatBinderProvider());
}).AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
builder.Services.AddRazorPages();

// Rate limiter — fixed window, 20 req/min, partitioned per client IP
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("DispatchCheckPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // Reject immediately — no queuing
            }
        )
    );

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please retry after 60 seconds." },
            cancellationToken);
    };
});

// API key filter — must be registered for [ServiceFilter] DI injection
builder.Services.AddScoped<ApiKeyAuthFilter>();

string? ConnectionString = string.Empty;

#if DEBUG
    ConnectionString = builder.Configuration?.GetConnectionString("Local");
#else
    ConnectionString = builder.Configuration?.GetConnectionString("Production");
#endif

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
dataSourceBuilder.EnableDynamicJson();
await using var dataSource = dataSourceBuilder.Build();
builder.Services.AddSingleton<AuditInterceptor>();
builder.Services.AddSingleton<DashboardChangeNotifierInterceptor>();
builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
{
    var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
    var dashboardNotifier = sp.GetRequiredService<DashboardChangeNotifierInterceptor>();
    options.UseNpgsql(dataSource, o => { o.SetPostgresVersion(16, 4); o.EnableRetryOnFailure(); })
           .AddInterceptors(auditInterceptor, dashboardNotifier);
});
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
    };
    x.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        // WebSockets (SignalR) can't send an Authorization header, so the hub connection sends
        // the JWT via ?access_token= instead. The uploads path accepts the same query param for
        // the same reason: it's rendered through plain <img>/<embed>/<iframe> tags, which also
        // can't attach a header — see Client/Layout/FileListView.razor.
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs") || path.StartsWithSegments(uploadsPublicUrl)))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        // A deactivated account's already-issued token was otherwise valid for up to the rest of
        // its (previously 30-day) lifetime — deactivating a user in UsersController had no effect
        // on requests already carrying their token. This makes deactivation take effect on that
        // user's very next request instead.
        OnTokenValidated = async context =>
        {
            var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                context.Fail("Invalid token.");
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var isActive = await dbContext.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => (bool?)u.IsActive)
                .FirstOrDefaultAsync();

            if (isActive is not true)
            {
                context.Fail("Account is inactive or no longer exists.");
            }
        }
    };
});


builder.Services.AddSignalR(options => {
    options.StatefulReconnectBufferSize = 1000;
});
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ILoggerProvider, ApplicationLoggerProvider>();
builder.Services.AddTransient<IDashboardService, DashboardService>();
builder.Services.AddTransient<IControlRoomService, ControlRoomService>();
builder.Services.AddTransient<ITruckReportService, TruckReportService>();
builder.Services.AddTransient<IDriverReportService, DriverReportService>();
builder.Services.AddTransient<IStationReportService, StationReportService>();
builder.Services.AddScoped<ShortageNotificationService>();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<EmailPublisherService>();
    builder.Services.AddHostedService<EmailConsumerService>();
}

builder.Services.AddSingleton(sp =>
{
    var assembly = typeof(Program).Assembly;
    var engine = new RazorLightEngineBuilder()
        .UseEmbeddedResourcesProject(assembly, "Api.EmailTemplates")
        .UseMemoryCachingProvider()
        .Build();
    return engine;
});


var smtp = builder.Configuration?.GetSection(SmtpOption.Key).Get<SmtpOption>();


// var messageBrokerSetting = builder.Configuration.GetSection("RabbitMQ").Get<MessageBrokerSetting>();
// Console.WriteLine(messageBrokerSetting.HostName);


var client = new SmtpClientOptions()
{
    User = smtp?.Email,
    Password = smtp?.Password,
    Port = smtp!.Port,
    Server = smtp?.Host,
    RequiresAuthentication = true,
    UseSsl = false

};
builder.Services.AddSingleton<ISender>(x => new MailKitSender(client));

builder.Services.AddTransient<IFluentEmailFactory, FluentEmailFactory>();
// Brevo only relays mail from a verified sender — must match the domain-authenticated
// address configured in the Brevo dashboard, not an arbitrary address.
var senderEmail = builder.Configuration["Brevo:SenderEmail"] ?? "mis@atlanticlogistics-atv.com.ng";
var senderName = builder.Configuration["Brevo:SenderName"];
builder.Services.AddFluentEmail(senderEmail, senderName)
    .AddRazorRenderer()
    .AddMailKitSender(client);




var app = builder.Build();

// Resolved once, here — a normal top-level resolution, not nested inside anything else. See
// DashboardChangeNotifierInterceptor's doc comment for why it can't safely resolve this itself.
DashboardChangeNotifierInterceptor.HubContext = app.Services.GetRequiredService<IHubContext<DashboardHub>>();

await SeedData.EnsureSeeded(app.Services);

//app.UseResponseCompression();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// KnownNetworks/KnownProxies are deliberately left at their secure default (loopback only,
// ForwardLimit defaults to 1). This API is only ever reverse-proxied by an nginx instance
// running on the same host (see .github/workflows/staging-deploy-api.yml, which reloads a
// host-level nginx service), which connects via 127.0.0.1 — already within the default trust
// list. Widening this to trust non-loopback sources would let any external caller forge
// X-Forwarded-For and manipulate the rate limiter's per-IP partitioning below. If nginx is ever
// moved off the same host (e.g. into its own container/network), this assumption breaks and
// KnownProxies must be updated to name it explicitly — do not just clear the trust lists.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// app.UseBlazorFrameworkFiles();
app.MapStaticAssets();

var rawUploadPath = app.Configuration["FileStorage:UploadPath"]!;
var uploadPath = Path.IsPathRooted(rawUploadPath)
    ? rawUploadPath
    : Path.Combine(app.Environment.ContentRootPath, rawUploadPath);
var publicUrl = app.Configuration["FileStorage:PublicUrl"]!;
if (!Directory.Exists(uploadPath))
    Directory.CreateDirectory(uploadPath);

app.UseRouting();

// CORS must run before Authentication/Authorization: a request that gets rejected by auth
// short-circuits before reaching any later middleware, so if UseCors ran after them, that 401/403
// response would go out with no Access-Control-Allow-Origin header — the browser then blocks the
// caller from ever reading the status code and Blazor's HttpClient surfaces a generic
// "TypeError: Failed to fetch" instead of a normal 401 the client code could handle.
app.UseCors(MyAllowSpecificOrigins);

// Rate limiter must be after UseRouting (needs route metadata) and before UseAuthentication
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Uploaded trip/service documents used to be mounted here unconditionally, before
// UseAuthentication/UseAuthorization even ran — anyone who could guess or otherwise obtain a
// ServerFileName (e.g. via the trip listing, before that was locked down) could download it with
// no credentials at all. Now gated behind the same JWT auth as the rest of the API; the client
// authenticates by appending its token as ?access_token= (see OnMessageReceived above), since a
// plain <img>/<embed>/<iframe> src can't carry an Authorization header.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments(uploadsPublicUrl))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }
    await next();
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = publicUrl
});
// UseExceptionHandler("/Error") above pointed at an endpoint that didn't exist — the built-in
// fallback happened to serve the SPA shell instead of leaking a raw exception, but API callers
// (fetch/HttpClient) got a garbled non-JSON response instead of a structured error.
app.Map("/Error", (HttpContext context) =>
{
    var feature = context.Features.Get<IExceptionHandlerPathFeature>();
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
    logger.LogError(feature?.Error, "Unhandled exception on {Path}", feature?.Path);
    return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An unexpected error occurred.");
});

app.MapRazorPages();
app.MapControllers();
app.MapHub<AppHub>("/hubs/app");
app.MapHub<DashboardHub>("/hubs/dashboard");
app.MapFallbackToFile("index.html");

app.Run();
