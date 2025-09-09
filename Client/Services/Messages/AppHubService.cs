namespace Client.Services.Messages;

using Microsoft.AspNetCore.SignalR.Client;

public class AppHubService : IAsyncDisposable
{
    private HubConnection? _connection;
    public event Action<Guid>? OnInvestmentSold; // Pass InvestmentId to be precise

    public async Task StartConnectionAsync(string baseUrl, string? token = null)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/app", options =>
            {
                if (!string.IsNullOrEmpty(token))
                {
                    options.AccessTokenProvider = () => Task.FromResult(token);
                }
            })
            .Build();

        // _connection.On<InvestmentUpdateDto>("InvestmentSold", (update) =>
        // {
        //     OnInvestmentSold?.Invoke(update.InvestmentId); // Notify with ID
        // });

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR connection failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}