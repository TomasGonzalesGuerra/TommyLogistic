using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Orders;

public class OrderPreviewDTO
{
    public int Id { get; set; }
    public string TrackingCode { get; set; } = null!;
    public OrderStatus OrderStatus { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public int DeliveryAttempts { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }

    // Destinatario
    public string RecipientName { get; set; } = null!;
    public string RecipientPhone { get; set; } = null!;
    public string RecipientAddress { get; set; } = null!;
    public string RecipientDistrict { get; set; } = null!;

    // Paquete
    public string PackageDescription { get; set; } = null!;
    public int Quantity { get; set; }

    // Empresa y Driver
    public string? CompanyName { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
}