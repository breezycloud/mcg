using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Shared.Hubs;

/// <summary>
/// Application-level SignalR hub.
/// Authenticated — JWT is required to connect.
/// On connect, places the user in a role-based group so the server
/// can broadcast to all managers without querying the DB each time.
/// </summary>
[Authorize]
public class AppHub : Hub
{
    // Group name for all roles that should receive report-submission notifications
    public const string ManagersGroup = "Managers";

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role is "Admin" or "Master" or "Supervisor")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ManagersGroup);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Group membership is automatically cleaned up on disconnect by SignalR
        await base.OnDisconnectedAsync(exception);
    }
}
