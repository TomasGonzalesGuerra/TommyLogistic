using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Cargas;

public class MyCargaOrderDTO
{
    public int Id { get; set; }
    public string TrackingCode { get; set; } = null!;
    public OrderStatus OrderStatus { get; set; }
    public DeliveryType DeliveryType { get; set; }

    // Destinatario
    public string RecipientName { get; set; } = null!;
    public string RecipientPhone { get; set; } = null!;
    public string RecipientAddress { get; set; } = null!;
    public string RecipientDistrict { get; set; } = null!;

    // Paquete
    public string PackageDescription { get; set; } = null!;
    public int Quantity { get; set; }

    // Fechas
    public DateTime RegistrationDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? RescheduledDate { get; set; }

    // Intentos
    public int DeliveryAttempts { get; set; }

    // Empresa
    public string CompanyName { get; set; } = null!;

    // Eventos / timeline
    public List<MyCargaEventDTO> Events { get; set; } = [];
}
