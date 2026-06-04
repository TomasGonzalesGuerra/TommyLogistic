using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Drivers;

public class MyDeliveriesFilterDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public OrderStatus? Status { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
