using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverUpdateStatusDTO
{
    public OrderStatus NewStatus { get; set; }
    public string? Note { get; set; }
}
