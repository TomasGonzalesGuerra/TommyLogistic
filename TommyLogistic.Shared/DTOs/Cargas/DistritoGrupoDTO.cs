namespace TommyLogistic.Shared.DTOs.Cargas;

public class DistritoGrupoDTO
{
    public string Distrito { get; set; } = null!;
    public int Count { get; set; }
    public List<PedidoDisponibleDTO> Pedidos { get; set; } = [];
}