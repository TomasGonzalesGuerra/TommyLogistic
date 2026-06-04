using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Cargas;

public class MyCargaEventDTO
{
    public DateTime Timestamp { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string? Note { get; set; }
    public string? BaglokLocation { get; set; }
    public string UserName { get; set; } = null!;
}
