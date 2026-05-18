using Microsoft.AspNetCore.SignalR;

namespace TommyLogistic.API.Hubs;

public class NotificationHub : Hub
{
    // Driver ──────────────────────────────────────────────────
    public async Task JoinDriversGroup() => await Groups.AddToGroupAsync(Context.ConnectionId, "Drivers");
    public async Task LeaveDriversGroup() => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Drivers");
    public async Task JoinPersonalGroup(string userID) => await Groups.AddToGroupAsync(Context.ConnectionId, $"Driver_{userID}");
    public async Task LeavePersonalGroup(string userID) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Driver_{userID}");

    // Admins ──────────────────────────────────────────────────
    public async Task JoinAdminGroup() => await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
    public async Task LeaveAdminGroup() => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
}