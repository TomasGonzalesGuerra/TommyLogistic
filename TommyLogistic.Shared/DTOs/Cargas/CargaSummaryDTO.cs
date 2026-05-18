using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Cargas;

public class CargaSummaryDTO
{
    public int Id { get; set; }
    public CargaStatus Status { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaConcluida { get; set; }
    public DateTime? FechaFacturada { get; set; }

    // Driver
    public string DriverID { get; set; } = null!;
    public string DriverName { get; set; } = null!;
    public string DriverPlaca { get; set; } = null!;
    public string? DriverPhoto { get; set; }

    // Creada por
    public string SupervisorName { get; set; } = null!;

    // Stats pedidos
    public int TotalPedidos { get; set; }
    public int Entregados { get; set; }
    public int EnOnStorage { get; set; }
    public int Pendientes { get; set; }
    public string Distrito { get; set; } = null!;
}