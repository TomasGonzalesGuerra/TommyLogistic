using TommyLogistic.API.Data;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Services;

public class OrderEventService(LogisticDataContext dataContext)
{
    private readonly LogisticDataContext _dataContext = dataContext;

    public async Task RegisterAsync(int orderId, OrderStatus newStatus, string userId, string? assignedDriverId = null, string? note = null)
    {
        var ev = new OrderEvent
        {
            OrderID = orderId,
            NewStatus = newStatus,
            UserID = userId,
            AssignedDriverID = assignedDriverId,
            Note = note,
            Timestamp = DateTime.UtcNow
        };

        _dataContext.OrderEvents.Add(ev);
        await _dataContext.SaveChangesAsync();
    }
}


//    // ── Consultar timeline ───────────────────────────────────────

//    public async Task<OrderTimelineDTO?> GetTimelineAsync(int orderId)
//    {
//        var order = await _datContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);

//        if (order is null) return null;

//        var events = await _datContext.OrderEvents.AsNoTracking()
//            .Where(e => e.OrderID == orderId)
//            .Include(e => e.User)
//            .OrderBy(e => e.OccurredAt)
//            .ToListAsync();

//        return new OrderTimelineDTO
//        {
//            OrderId = order.Id,
//            TrackingCode = order.TrackingCode,
//            RecipientName = order.RecipientName,
//            Address = order.RecipientAddress,
//            CurrentStatus = order.OrderStatus,
//            TotalAttempts = events.Count(e => e.EventType == OrderEventType.PickedUpByDriver),
//            Events = events.Select(MapToDTO).ToList(),
//        };
//    }

//    // ── Helpers ──────────────────────────────────────────────────

//    private async Task<int> CountDeliveryAttempts(int orderId)
//        => await _datContext.OrderEvents.CountAsync(e =>
//            e.OrderID == orderId &&
//            e.EventType == OrderEventType.PickedUpByDriver);

//    private static OrderStatus ResolveStatus(OrderEventType t, OrderStatus current) => t switch
//    {
//        OrderEventType.Received => OrderStatus.Registered,
//        OrderEventType.RegisteredInWarehouse => OrderStatus.Registered,
//        OrderEventType.AssignedToDriver => OrderStatus.Assigned,
//        OrderEventType.Reassigned => OrderStatus.Assigned,
//        OrderEventType.PickedUpByDriver => OrderStatus.PickedUp,
//        OrderEventType.OutForDelivery => OrderStatus.OnTheWay,
//        OrderEventType.Delivered => OrderStatus.Delivered,
//        OrderEventType.RecipientAbsent => OrderStatus.RecipientAbsent,
//        OrderEventType.DeliveryFailed => OrderStatus.Failed,
//        OrderEventType.Returning => OrderStatus.Returning,
//        OrderEventType.StoredInBaglok => OrderStatus.OnStorage,
//        OrderEventType.Rescheduled => OrderStatus.Rescheduled,
//        OrderEventType.ReleasedFromBaglok => OrderStatus.Assigned,
//        OrderEventType.MarkedAsFailed => OrderStatus.Failed,
//        OrderEventType.ReturnedToClient => OrderStatus.Failed,
//        OrderEventType.Cancelled => OrderStatus.Failed,
//        _ => current,
//    };

//    private static string StatusColor(OrderStatus s) => s switch
//    {
//        OrderStatus.Registered => "#64748b",
//        OrderStatus.Assigned => "#0694a2",
//        OrderStatus.PickedUp => "#7c3aed",
//        OrderStatus.OnTheWay => "#1a56db",
//        OrderStatus.Delivered => "#0e9f6e",
//        OrderStatus.RecipientAbsent => "#e3a008",
//        OrderStatus.Rescheduled => "#d97706",
//        OrderStatus.Returning => "#ea580c",
//        OrderStatus.OnStorage => "#6366f1",
//        OrderStatus.Failed => "#e02424",
//        _ => "#94a3b8",
//    };

//    private static OrderEventDTO MapToDTO(OrderEvent e)
//    {
//        var (label, icon, color, phase) = Metadata(e.EventType);
//        return new OrderEventDTO
//        {
//            Id = e.Id,
//            EventType = e.EventType,
//            EventLabel = label,
//            EventIcon = icon,
//            EventColor = color,
//            StatusAfter = e.StatusAfter,
//            OccurredAt = e.OccurredAt,
//            DeliveryAttempt = e.DeliveryAttempt,
//            ActorName = e.User!.FullName,
//            ActorRole = e.User!.UserType.ToString(),
//            Notes = e.Notes,
//            BaglokLocation = e.BaglokLocation,
//            RescheduledFor = e.RescheduledFor,
//            Phase = phase,
//        };
//    }

//    private static (string label, string icon, string color, TimelinePhase phase)
//        Metadata(OrderEventType type) => type switch
//        {
//            OrderEventType.Received => ("Pedido recibido", "📥", "#6366f1", TimelinePhase.Entry),
//            OrderEventType.RegisteredInWarehouse => ("Registrado en almacén", "🏭", "#0694a2", TimelinePhase.Entry),
//            OrderEventType.AssignedToDriver => ("Asignado a repartidor", "👤", "#1a56db", TimelinePhase.Dispatch),
//            OrderEventType.Reassigned => ("Reasignado", "🔀", "#7c3aed", TimelinePhase.Dispatch),
//            OrderEventType.PickedUpByDriver => ("Retirado por repartidor", "🛵", "#1a56db", TimelinePhase.Dispatch),
//            OrderEventType.OutForDelivery => ("En camino al destinatario", "🚚", "#1a56db", TimelinePhase.Delivery),
//            OrderEventType.Delivered => ("Entregado exitosamente", "✅", "#0e9f6e", TimelinePhase.Delivery),
//            OrderEventType.RecipientAbsent => ("Destinatario ausente", "🚪", "#e3a008", TimelinePhase.Delivery),
//            OrderEventType.DeliveryFailed => ("Entrega fallida", "❌", "#e02424", TimelinePhase.Delivery),
//            OrderEventType.Returning => ("En retorno al almacén", "↩️", "#ea580c", TimelinePhase.Return),
//            OrderEventType.StoredInBaglok => ("Guardado en Baglok", "📦", "#6366f1", TimelinePhase.Return),
//            OrderEventType.Rescheduled => ("Reprogramado", "📅", "#d97706", TimelinePhase.Reschedule),
//            OrderEventType.ReleasedFromBaglok => ("Liberado del Baglok", "🔓", "#0694a2", TimelinePhase.Reschedule),
//            OrderEventType.MarkedAsFailed => ("Marcado como fallido", "🚫", "#e02424", TimelinePhase.Closed),
//            OrderEventType.ReturnedToClient => ("Devuelto al cliente", "📤", "#64748b", TimelinePhase.Closed),
//            OrderEventType.Cancelled => ("Cancelado", "🗑️", "#64748b", TimelinePhase.Closed),
//            OrderEventType.Note => ("Nota interna", "📝", "#94a3b8", TimelinePhase.Entry),
//            _ => ("Evento", "•", "#94a3b8", TimelinePhase.Entry),
//        };
//}