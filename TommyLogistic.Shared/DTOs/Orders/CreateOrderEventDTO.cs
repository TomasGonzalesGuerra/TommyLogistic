using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Orders;

public class CreateOrderEventDTO
{
    public int OrderId { get; set; }
    public string ActorUserID { get; set; } = null!;
    public OrderEventType EventType { get; set; }
    public string? Notes { get; set; }
    public string? BaglokLocation { get; set; }
    public DateTime? RescheduledFor { get; set; }
}