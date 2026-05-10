using Microsoft.AspNetCore.SignalR;
using TommyLogistic.API.Hubs;
using TommyLogistic.Shared.DTOs.Orders;

namespace TommyLogistic.API.Services;

/// <summary>
/// Encapsula todas las notificaciones SignalR relacionadas con pedidos.
/// El OrderEventService lo inyecta para notificar después de cada evento.
/// </summary>
public class OrderHubService(IHubContext<OrderHub> _hub)
{
    // ── Notificar nuevo evento en el timeline de un pedido ───────
    /// <summary>
    /// Enviado al grupo "order-{orderId}" cuando se registra un nuevo evento.
    /// El cliente recibe el DTO completo y lo agrega al timeline sin recargar.
    /// </summary>
    public async Task NotifyNewEventAsync(int orderId, OrderEventDTO eventDto)
    {
        await _hub.Clients.Group(OrderHub.GroupName(orderId)).SendAsync("NewOrderEvent", eventDto);
    }

    // ── Notificar cambio de estado ────────────────────────────────
    /// <summary>
    /// Enviado al grupo "order-{orderId}" y al "dashboard" cuando cambia el estado.
    /// </summary>
    public async Task NotifyStatusChangedAsync(int orderId, string trackingCode, string newStatus, string statusColor)
    {
        var payload = new
        {
            OrderId = orderId,
            TrackingCode = trackingCode,
            NewStatus = newStatus,
            StatusColor = statusColor,
            ChangedAt = DateTime.UtcNow,
        };

        // Notificar a quienes tienen abierto ese pedido
        await _hub.Clients.Group(OrderHub.GroupName(orderId)).SendAsync("OrderStatusChanged", payload);

        // Notificar también al dashboard
        await _hub.Clients.Group("dashboard").SendAsync("OrderStatusChanged", payload);
    }

    // ── Notificar nuevo pedido al dashboard ───────────────────────
    public async Task NotifyNewOrderAsync(object orderSummary) => await _hub.Clients.Group("dashboard").SendAsync("NewOrderReceived", orderSummary);
}
