using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Orders;

public class OrderTimelineDTO
{
    public int OrderId { get; set; }
    public string TrackingCode { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string Address { get; set; } = "";
    public OrderStatus CurrentStatus { get; set; }
    public int TotalAttempts { get; set; }
    public List<OrderEventDTO> Events { get; set; } = [];
}
