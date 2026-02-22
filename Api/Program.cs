using System.Configuration;
using System.Text;
using System.Threading.RateLimiting;
using Api.Context;
using Api.Data;
using Api.Filters;
using Api.Interceptors;
using Api.Logging;
using Api.Services.Dashboards;
using Api.Services.Messages;
using Api.Util;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.MailKitSmtp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RazorLight;
using Shared.Helpers;
using Shared.Interfaces.Dashboards;
using Shared.Models.MessageBroker;

var builder = WebApplication.CreateBuilder(args);


brevo_csharp.Client.Configuration.Default.ApiKey.Add("api-key", builder.Configuration["Brevo:Makulli"]!);
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.Configure<MessageBrokerSetting>(builder.Configuration?.GetSection("RabbitMQ"));
var key = Encoding.ASCII.GetBytes(builder.Configuration["App:Key"]!);
string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
            builder =>
            {
                builder.AllowAnyOrigin()
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
builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
{
    var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
    options.UseNpgsql(dataSource, o => { o.SetPostgresVersion(16, 4); o.EnableRetryOnFailure(); })
           .AddInterceptors(auditInterceptor);
});
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = true;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
    };
});


builder.Services.AddSignalR(options => {
    options.StatefulReconnectBufferSize = 1000;
});
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
// builder.Services.AddHostedService<ServerPeriodicJob>();
builder.Services.AddSingleton<ILoggerProvider, ApplicationLoggerProvider>();
builder.Services.AddTransient<IDashboardService, DashboardService>();

builder.Services.AddScoped<EmailPublisherService>();
builder.Services.AddHostedService<EmailConsumerService>();

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
builder.Services.AddFluentEmail("mustapha.aliyu@mcg.com.cn")
    .AddRazorRenderer()
    .AddMailKitSender(client);




var app = builder.Build();
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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

// app.UseBlazorFrameworkFiles();
app.MapStaticAssets();

// Serve uploaded files from the configured upload directory
var rawUploadPath = app.Configuration["FileStorage:UploadPath"]!;
var uploadPath = Path.IsPathRooted(rawUploadPath)
    ? rawUploadPath
    : Path.Combine(app.Environment.ContentRootPath, rawUploadPath);
var publicUrl = app.Configuration["FileStorage:PublicUrl"]!;
if (!Directory.Exists(uploadPath))
    Directory.CreateDirectory(uploadPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = publicUrl
});

app.UseRouting();

// Rate limiter must be after UseRouting (needs route metadata) and before UseAuthentication
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors(MyAllowSpecificOrigins);
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
