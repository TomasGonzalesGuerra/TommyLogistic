namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverDTO
{
    public string Id { get; set; } = null!;
    public string DNI { get; set; } = null!;
    public string Placa { get; set; } = null!;
    public string Photo { get; set; } = null!;
    public string Celular { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public bool Available { get; set; }
    public int DeliveredToday { get; set; }
    public int ActiveOrderToday { get; set; }
}
