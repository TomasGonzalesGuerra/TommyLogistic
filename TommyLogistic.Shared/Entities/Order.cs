using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.Entities;

public class Order
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public bool Invoiced { get; set; }
    public int DeliveryAttempts { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DateTime? RescheduledDate { get; set; }
    public string TrackingCode { get; set; } = null!;
    public string RecipientName { get; set; } = null!;
    public string RecipientPhone { get; set; } = null!;
    public string RecipientAddress { get; set; } = null!;
    public string RecipientDistrict { get; set; } = null!;
    public string PackageDescription { get; set; } = null!;

    // Company Dueña
    public int CompanyID { get; set; }
    public Company Company { get; set; } = null!;

    // Driver Asignado
    public string? DriverID { get; set; }
    public Driver? Driver { get; set; }
}
