using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TommyLogistic.API.Hubs;

/// <summary>
/// Grupos:
///   "order-{orderId}"  → todos los clientes viendo ese pedido
///   "dashboard"        → admins/supervisores en el dashboard principal
///
/// Métodos que el SERVIDOR puede llamar en el cliente:
///   "NewOrderEvent"      → nuevo evento en el timeline
///   "OrderStatusChanged" → cambio de estado del pedido
/// </summary>
[Authorize]
public class OrderHub : Hub
{
    // ── El cliente se une al grupo de un pedido específico ──────
    public async Task JoinOrderGroup(int orderId) => await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(orderId));
    public async Task LeaveOrderGroup(int orderId) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(orderId));

    // ── El cliente se une al grupo del dashboard ─────────────────
    public async Task JoinDashboard() => await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
    public async Task LeaveDashboard() => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");

    // ── Helper ──────────────────────────────────────────────────
    public static string GroupName(int orderId) => $"order-{orderId}";
}