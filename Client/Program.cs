using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Client;
using Client.Services.Flowbites;
using Shared.Helpers;
using Blazored.LocalStorage;
using Client.Handlers;
using Polly;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.Interfaces.Auth;
using Client.Services.Auth;
using Shared.Interfaces.Trucks;
using Shared.Models.Trucks;
using Shared.Interfaces.Trips;
using Shared.Interfaces.Drivers;
using Client.Services.Drivers;
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
using ApexCharts;
using Shared.Interfaces.Incidents;
using Client.Services.Incidents;
using Client.Services.incidenttypes;
using Shared.Interfaces;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();

string uri = string.Empty;
#if DEBUG
    uri = Constants.DevBaseAddress!;
#else
    uri = Constants.ProdBaseAddress;
#endif

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(uri) });
builder.Services.AddHttpClient(Constants.Url, http =>
{
    http.BaseAddress = new Uri(uri);
}).AddHttpMessageHandler<CustomAuthorizationHandler>()
.AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
    );;
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
builder.Services.AddTransient<IDashboardService, DashboardService>();
builder.Services.AddTransient<IRequestService, RequestService>();
builder.Services.AddTransient<IMaintenanceService, MaintenanceService>();
builder.Services.AddTransient<IStationService, StationService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IAuditLogService, AuditLogService>();
builder.Services.AddTransient<IDestinationService, DestinationService>();
builder.Services.AddTransient<ICheckpointService, CheckpointService>();
builder.Services.AddTransient<ITripCheckpointService, TripCheckpointService>();
builder.Services.AddTransient<IRefuelInfoService, RefuelInfoService>();
builder.Services.AddTransient<IDischargeService, DischargeService>();
builder.Services.AddTransient<IIncidentService, IncidentService>();
builder.Services.AddTransient<IIncidentTypeService, IncidentTypeService>();
builder.Services.AddHttpClient<ILocationService, LocationService>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});


builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<AppHubService>();
builder.Services.AddScoped<SidebarService>();

builder.Services.AddTransient<IExportService, CsvExportService>();

await builder.Build().RunAsync();
