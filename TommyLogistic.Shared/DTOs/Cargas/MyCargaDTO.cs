using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Cargas;

public class MyCargaDTO
{
    // ── Carga ────────────────────────────────────────────────
    public int Id { get; set; }
    public CargaStatus Status { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaConcluida { get; set; }
    public DateTime? FechaFacturada { get; set; }
    public string? NotaConclusion { get; set; }
    public string? NotaFacturacion { get; set; }

    // ── Driver ─────────────────────────────────────────────
    public string DriverPlaca { get; set; } = null!;
    public string DriverName { get; set; } = null!;
    public string? DriverPhoto { get; set; }
    // ── Personas ─────────────────────────────────────────────
    public string SupervisorName { get; set; } = null!;
    public string? ConcluidaPorName { get; set; }
    public string? FacturadaPorName { get; set; }

    // ── Stats rápidas ─────────────────────────────────────────
    public int TotalPedidos { get; set; }
    public int Entregados { get; set; }
    public int Fallidos { get; set; }
    public int Ausentes { get; set; }
    public int EnRetorno { get; set; }
    public int EnAlmacen { get; set; }

    // ── Pedidos completos ─────────────────────────────────────
    public List<MyCargaOrderDTO> Pedidos { get; set; } = [];
}
