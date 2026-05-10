using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Orders;

public class OrderEventDTO
{
    public int Id { get; set; }
    public OrderEventType EventType { get; set; }
    public string EventLabel { get; set; } = "";
    public string EventIcon { get; set; } = "";
    public string EventColor { get; set; } = "";
    public OrderStatus StatusAfter { get; set; }
    public DateTime OccurredAt { get; set; }
    public int DeliveryAttempt { get; set; }

    // Actor
    public string ActorName { get; set; } = null!;
    public string ActorRole { get; set; } =null!;

    // Contexto extra
    public string? Notes { get; set; }
    public string? BaglokLocation { get; set; }
    public DateTime? RescheduledFor { get; set; }

    // Agrupación visual para el timeline
    public TimelinePhase Phase { get; set; }
}