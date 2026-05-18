using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.Entities;

public class Carga
{
    public int Id { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaConcluida { get; set; }
    public DateTime? FechaFacturada { get; set; }
    public CargaStatus Status { get; set; } = CargaStatus.Activa;
    public string? NotaConclusion { get; set; }
    public string? NotaFacturacion { get; set; }

    // Driver asignado
    public string DriverID { get; set; } = null!;
    public Driver? Driver { get; set; }
    public string SupervisorID { get; set; } = null!;
    public User? Supervisor { get; set; }
    public string? ConcluidaPorID { get; set; }
    public User? ConcluidaPor { get; set; }
    public string? FacturadaPorID { get; set; }
    public User? FacturadaPor { get; set; }

    public ICollection<Order>? Orders { get; set; }
}
