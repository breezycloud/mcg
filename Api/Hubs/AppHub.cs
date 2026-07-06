using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Shared.Hubs;

/// <summary>
/// Application-level SignalR hub.
/// Authenticated — JWT is required to connect.
/// On connect, places Admin/Master in a broadcast group so the server can notify
/// them of every report submission without querying the DB each time. Everyone
/// else's report-submission notifications are role-agnostic and targeted directly
/// at whoever is set as the submitter's Supervisor (see DailyReportsController),
/// since visibility is driven by User.SupervisorId, not by role name.
/// </summary>
[Authorize]
public class AppHub : Hub
{
    // Group name for the roles with unrestricted report visibility
    public const string ManagersGroup = "Managers";

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role is "Admin" or "Master")
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
