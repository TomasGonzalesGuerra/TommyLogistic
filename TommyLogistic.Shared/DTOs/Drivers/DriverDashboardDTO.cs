using TommyLogistic.Shared.DTOs.Cargas;

namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverDashboardDTO
{
    // Info del driver
    public string FullName { get; set; } = null!;
    public string Placa { get; set; } = null!;
    public string? Photo { get; set; }
    public bool Available { get; set; }
    // Stats globales del día (siguen siendo útiles en el header)
    public int Asignados { get; set; }
    public int Retirados { get; set; }
    public int EnCamino { get; set; }
    public int Entregados { get; set; }
    public int Fallidos { get; set; }
    public int EnRetorno { get; set; }

    // 1:N — carga activa separada, historial aparte
    public CargaDetailDTO? CargaActiva { get; set; }
    public List<CargaDetailDTO> CargasAnteriores { get; set; } = [];
}