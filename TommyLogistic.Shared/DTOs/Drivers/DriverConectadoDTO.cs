namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverConectadoDTO
{
    public string UserId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Placa { get; set; } = null!;
    public string? Photo { get; set; }
    public bool Available { get; set; }
}
