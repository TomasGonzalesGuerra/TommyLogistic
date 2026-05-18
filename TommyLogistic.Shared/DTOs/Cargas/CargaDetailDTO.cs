using TommyLogistic.Shared.DTOs.Drivers;

namespace TommyLogistic.Shared.DTOs.Cargas;

public class CargaDetailDTO : CargaSummaryDTO
{
    public string? NotaConclusion { get; set; }
    public string? NotaFacturacion { get; set; }
    public string? ConcluidaPorName { get; set; }
    public string? FacturadaPorName { get; set; }
    public List<DriverOrderDTO> Pedidos { get; set; } = [];
}