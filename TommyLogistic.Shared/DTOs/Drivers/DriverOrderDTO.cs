using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverOrderDTO
{
    public int Id { get; set; }
    public string TrackingCode { get; set; } = null!;
    public OrderStatus OrderStatus { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public int DeliveryAttempts { get; set; }
    public DateTime RegistrationDate { get; set; }
    public int Quantity { get; set; }
    public string CompanyName { get; set; } = null!;
    public string RecipientName { get; set; } = null!;
    public string RecipientPhone { get; set; } = null!;
    public string RecipientAddress { get; set; } = null!;
    public string RecipientDistrict { get; set; } = null!;
    public string PackageDescription { get; set; } = null!;
}