namespace TommyLogistic.Shared.DTOs.Admin;

public class AdminActivityDTO
{
    public string EventDescription { get; set; } = null!;
    public string ExecutedBy { get; set; } = null!;
    public string ExecutedRole { get; set; } = null!;
    public string OrderStatus { get; set; } = null!;
    public string TrackingCode { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}
