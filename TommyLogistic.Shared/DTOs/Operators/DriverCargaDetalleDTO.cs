using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Operators;

public class DriverCargaDetalleDTO
{
    public string DriverName { get; set; } = null!;
    public string DriverPlaca { get; set; } = null!;
    public string? DriverPhoto { get; set; }

    public int CargaId { get; set; }
    public CargaStatus CargaStatus { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string SupervisorName { get; set; } = null!;
    public string? NotaConclusion { get; set; }

    public List<PedidoEnRutaDTO> Pedidos { get; set; } = [];
}