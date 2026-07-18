using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Client;
using Client.Services.Flowbites;
using Blazored.LocalStorage;
using Client.Handlers;
using Polly;
using Polly.Extensions.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.Interfaces.Auth;
using Client.Services.Auth;
using Shared.Interfaces.Trucks;
using Shared.Models.Trucks;
using Shared.Interfaces.Trips;
using Shared.Interfaces.Drivers;
using Client.Services.Drivers;
using Shared.Interfaces.ControlRoom;
using Client.Services.ControlRoom;
using Client.Services.Trucks;
using Shared.Interfaces.Dashboards;
using Shared.Interfaces.Services;
using Client.Services.Services;
using Shared.Interfaces.Shops;
using Client.Services.Shops;
using Shared.Interfaces.Stations;
using Client.Services.Stations;
using Client.Services.Dashboards;
using Shared.Interfaces.Users;
using Client.Services.Users;
using Shared.Interfaces.AuditLogs;
using Client.Services.AuditLogs;
using Client.Services.Messages;
using Client.Services.Locations;
using Shared.Interfaces.Locations;
using Shared.Interfaces.Destinations;
using Client.Services.Destinations;
using Shared.Interfaces.Checkpoints;
using Client.Services.Checkpoints;
using Client.Services.TripCheckpoints;
using Shared.Interfaces.TripCheckpoints;
using Shared.Interfaces.RefuelInfos;
using Client.Services.RefuelInfos;
using Shared.Interfaces.Discharges;
using Client.Services.Discharges;
using Client.Services.Trips;
using ApexCharts;
using Shared.Interfaces.Incidents;
using Client.Services.Incidents;
using Client.Services.incidenttypes;
using Shared.Interfaces.Settings;
using Client.Services.Settings;
using Shared.Interfaces;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Shared.Helpers;
using Shared.Interfaces.Reports;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOptions();
// Fallback policy so every route requires authentication by default -- most pages had no
// [Authorize] attribute at all (only 10 of 85 did) and were reachable unauthenticated at the
// client-routing level. Pages that must stay public (login, forgot/reset password) opt out
// explicitly with [AllowAnonymous] instead of everything else needing to opt in.
builder.Services.AddAuthorizationCore(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddBlazoredLocalStorage();


string uri = new Client.Handlers.Constants(builder.Services.BuildServiceProvider().GetRequiredService<NavigationManager>(), builder.Configuration).BaseAddress();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(uri) });
builder.Services.AddHttpClient("AppUrl", http =>
{
    http.BaseAddress = new Uri(uri);
}).AddHttpMessageHandler<CustomAuthorizationHandler>()
// Only GET/HEAD are safe to retry automatically — retrying a POST/PUT/DELETE whose response
// was lost (timeout, dropped connection) resends a request that may have already been
// committed server-side, creating a genuine duplicate record independent of any user action.
.AddPolicyHandler(request => request.Method == HttpMethod.Get || request.Method == HttpMethod.Head
    ? HttpPolicyExtensions.HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
    : Policy.NoOpAsync<HttpResponseMessage>());
builder.Services.AddHttpClient<CustomAuthenticationStateProvider>();

builder.Services.AddScoped<AuthenticationStateProvider>(options => options.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddTransient<CustomAuthorizationHandler>();

// builder.Services.AddLogging(logging =>
// {
//     var httpClient = builder.Services.BuildServiceProvider().GetRequiredService<HttpClient>();
//     var authenticationStateProvider = builder.Services.BuildServiceProvider().GetRequiredService<AuthenticationStateProvider>();
//     logging.SetMinimumLevel(LogLevel.Error);
//     logging.AddProvider(new ApplicationLoggerProvider(httpClient, authenticationStateProvider));
// });

// builder.Services.AddM
builder.Services.AddApexCharts(e =>
{
    e.GlobalOptions = new ApexChartBaseOptions
    {
        Debug = true
    };
});
builder.Services.AddScoped<IFlowbiteService, FlowbiteService>();
builder.Services.AddScoped<AppState>();

builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<ITruckService, TruckService>();
builder.Services.AddTransient<ITripService, TripService>();
builder.Services.AddTransient<IDriverService, DriverService>();
builder.Services.AddTransient<IMotorMateService, MotorMateService>();
builder.Services.AddTransient<IDashboardService, DashboardService>();
builder.Services.AddTransient<IControlRoomService, ControlRoomService>();
builder.Services.AddTransient<ITruckReportService, TruckReportService>();
builder.Services.AddTransient<IDriverReportService, DriverReportService>();
builder.Services.AddTransient<IStationReportService, StationReportService>();
builder.Services.AddTransient<IRequestService, RequestService>();
builder.Services.AddTransient<IMaintenanceService, MaintenanceService>();
builder.Services.AddTransient<IStationService, StationService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IAuditLogService, AuditLogService>();
builder.Services.AddTransient<IAppSettingsService, AppSettingsService>();
builder.Services.AddTransient<IDestinationService, DestinationService>();
builder.Services.AddTransient<ICheckpointService, CheckpointService>();
builder.Services.AddTransient<ITripCheckpointService, TripCheckpointService>();
builder.Services.AddTransient<IRefuelInfoService, RefuelInfoService>();
builder.Services.AddTransient<IDischargeService, DischargeService>();
builder.Services.AddTransient<IIncidentService, IncidentService>();
builder.Services.AddTransient<IIncidentTypeService, IncidentTypeService>();
builder.Services.AddTransient<IDailyReportService, DailyReportService>();
builder.Services.AddTransient<IShortageRecommendationService, ShortageRecommendationService>();
builder.Services.AddHttpClient<ILocationService, LocationService>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});


builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<AppHubService>();
builder.Services.AddScoped<SidebarService>();

builder.Services.AddTransient<IExportService, CsvExportService>();

await builder.Build().RunAsync();
