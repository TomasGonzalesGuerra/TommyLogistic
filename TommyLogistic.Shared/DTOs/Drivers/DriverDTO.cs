namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverDTO
{
    public int Id { get; set; }
    public string? DNI { get; set; }
    public string? Placa { get; set; }
    public bool Available { get; set; }
    public string? Celular { get; set; }
    public string? FullName { get; set; }
    public string? Photo { get; set; }
    public int DeliveredToday { get; set; }
    public int ActiveOrderToday { get; set; }
}
