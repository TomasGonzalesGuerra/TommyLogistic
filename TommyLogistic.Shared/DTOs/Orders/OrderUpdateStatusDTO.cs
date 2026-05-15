using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Orders;

public class OrderUpdateStatusDTO
{
    public OrderStatus NewStatus { get; set; }
    public string? NewDriverID { get; set; }
    public string? Note { get; set; }
}
