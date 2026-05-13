using Microsoft.AspNetCore.SignalR;

namespace TommyLogistic.API.Hubs;

public class NotificationHub : Hub
{
    // Cuando un cliente Driver se conecta, se une al grupo "Drivers"
    // Así podemos hacer broadcast a todos los drivers de una sola vez
    public async Task JoinDriversGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Drivers");
    }

    // En NotificationHub.cs, agrega este método:
    public async Task JoinPersonalGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Driver_{userId}");
    }
}