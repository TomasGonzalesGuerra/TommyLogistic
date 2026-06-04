using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Operators;

public class UltimoEventoDTO
{
    public DateTime Timestamp { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string TrackingCode { get; set; } = null!;
    public string RecipientName { get; set; } = null!;
    public string? Note { get; set; }
}