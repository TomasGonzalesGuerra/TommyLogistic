using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Operators;

public class DriverEnRutaDTO
{
    // Driver
    public string DriverID { get; set; } = null!;
    public string DriverName { get; set; } = null!;
    public string DriverPlaca { get; set; } = null!;
    public string? DriverPhoto { get; set; }

    // Carga activa
    public int CargaId { get; set; }
    public CargaStatus CargaStatus { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string SupervisorName { get; set; } = null!;

    // Stats de pedidos
    public int TotalPedidos { get; set; }
    public int Entregados { get; set; }
    public int EnCamino { get; set; }
    public int Pendientes { get; set; }
    public int Fallidos { get; set; }
    public int Ausentes { get; set; }
    public int EnRetorno { get; set; }

    // Últimos 5 eventos del timeline (todos los pedidos de la carga)
    public List<UltimoEventoDTO> UltimosEventos { get; set; } = [];
}
