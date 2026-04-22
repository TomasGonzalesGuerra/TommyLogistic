using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Orders;

public class OrderSummaryDTO
{
    public int Id { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public int DeliveryAttempts { get; set; }
    public string? TrackingCode { get; set; }
    public string? RecipientName { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string? RecipientDistrict { get; set; }
    public string? PackageDescription { get; set; }
    public string? DeliveryPersonName { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
}
