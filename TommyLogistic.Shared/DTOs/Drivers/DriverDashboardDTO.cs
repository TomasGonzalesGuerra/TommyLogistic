using TommyLogistic.Shared.DTOs.Cargas;

namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverDashboardDTO
{
    public int Asignados { get; set; }
    public int Retirados { get; set; }
    public int EnCamino { get; set; }
    public int Entregados { get; set; }
    public int Fallidos { get; set; }
    public int EnRetorno { get; set; }
    public string FullName { get; set; } = null!;
    public string Placa { get; set; } = null!;
    public string? Photo { get; set; }
    public bool Available { get; set; }
    public CargaDetailDTO Carga { get; set; } = null!;
}