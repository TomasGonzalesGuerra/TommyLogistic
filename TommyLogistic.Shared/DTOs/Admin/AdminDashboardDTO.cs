namespace TommyLogistic.Shared.DTOs.Admin;

public class AdminDashboardDTO
{
    public int TotalPedidosHoy { get; set; }
    public int EnCaminoHoy { get; set; }
    public int EntregadosHoy { get; set; }
    public int FallidosHoy { get; set; }
    public int PendientesAsignar { get; set; }
    public int TotalDrivers { get; set; }
    public int DriversDisponibles { get; set; }
    public int DriversOcupados { get; set; }
    public List<AdminOrderFeedDTO> UltimosPedidos { get; set; } = [];
    public List<AdminActivityDTO> ActividadReciente { get; set; } = [];
}
