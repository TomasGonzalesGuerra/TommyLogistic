using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Order;

public class OrderSummaryDTO
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public int DeliveryAttempts { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string? DeliveryPersonName { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public string TrackingCode { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientDistrict { get; set; } = string.Empty;
    public string PackageDescription { get; set; } = string.Empty;
}
