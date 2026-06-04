using TommyLogistic.Shared.DTOs.Cargas;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Operators;

public class PedidoEnRutaDTO
{
    public int Id { get; set; }
    public string TrackingCode { get; set; } = null!;
    public OrderStatus OrderStatus { get; set; }
    public string RecipientName { get; set; } = null!;
    public string RecipientPhone { get; set; } = null!;
    public string RecipientAddress { get; set; } = null!;
    public string RecipientDistrict { get; set; } = null!;
    public string PackageDescription { get; set; } = null!;
    public int Quantity { get; set; }
    public int DeliveryAttempts { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string CompanyName { get; set; } = null!;

    public List<MiCargaEventoDTO> Events { get; set; } = [];
}
