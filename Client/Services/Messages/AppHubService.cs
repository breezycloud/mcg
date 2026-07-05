namespace Client.Services.Messages;

using Microsoft.AspNetCore.SignalR.Client;

public class AppHubService : IAsyncDisposable
{
    private HubConnection? _connection;

    /// <summary>Fired when the server sends a ReportAssigned notification to this user.</summary>
    public event Action<string, Guid>? OnReportAssigned; // (ReportNo, ReportId)

    /// <summary>Fired when any team member submits a report. Only managers receive this via the Managers group.</summary>
    public event Action<string, Guid>? OnReportSubmitted; // (ReportNo, ReportId)

    public async Task StartConnectionAsync(string baseUrl, string token)
    {
        if (_connection is not null) return; // Already started

        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/app", options =>
            {
                // JWT via query string — SignalR WebSocket can't send headers
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ReportAssignedPayload>("ReceiveReportAssigned", payload =>
        {
            OnReportAssigned?.Invoke(payload.ReportNo ?? payload.ReportId.ToString(), payload.ReportId);
        });

        _connection.On<ReportAssignedPayload>("ReceiveReportSubmitted", payload =>
        {
            OnReportSubmitted?.Invoke(payload.ReportNo ?? payload.ReportId.ToString(), payload.ReportId);
        });

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppHub] Connection failed: {ex.Message}");
        }
    }

    public async Task StopConnectionAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed record ReportAssignedPayload(string? ReportNo, Guid ReportId);
}
