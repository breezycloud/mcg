using System.Text;
using Api.Context;
using Api.Data;
using Api.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
var key = Encoding.ASCII.GetBytes(builder.Configuration["App:Key"]!);
string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
            builder =>
            {
                // builder.WithOrigins("https://localhost:44323",
                //             "https://localhost:5059",
                //             "http://localhost:5059")
                builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
            });
});
builder.Services.AddControllers()
                .AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
builder.Services.AddRazorPages();

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
builder.Services.AddScoped<AppDbContext>();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(dataSource, o => { o.SetPostgresVersion(16, 4); o.EnableRetryOnFailure(); });
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

var app = builder.Build();
SeedData.EnsureSeeded(app.Services);

//app.UseResponseCompression();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
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

app.UseBlazorFrameworkFiles();
app.MapStaticAssets();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors(MyAllowSpecificOrigins);
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
