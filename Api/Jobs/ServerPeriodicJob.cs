using Microsoft.AspNetCore.SignalR;
using Shared.Hubs;

namespace Api.Jobs;

public class ServerPeriodicJob : BackgroundService, IDisposable
{
    private readonly ILogger<ServerPeriodicJob> _logger;
    private readonly IHubContext<AppHub> _context;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(10));    
    public ServerPeriodicJob(ILogger<ServerPeriodicJob> logger, IHubContext<AppHub> context)
    {
        _logger = logger;
        _context = context;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("My Background Service is starting.");        
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {            
            await _context.Clients.All.SendAsync("UpdateSession");
            await _context.Clients.All.SendAsync("FetchLogs");
        }
    }
    void IDisposable.Dispose()
    {
        _timer.Dispose();
        _logger.LogInformation("timer disposed");
    }


}
