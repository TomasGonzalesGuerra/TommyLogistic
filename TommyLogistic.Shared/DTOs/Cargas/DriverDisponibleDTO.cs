namespace TommyLogistic.Shared.DTOs.Cargas;

public class DriverDisponibleDTO
{
    public string Id { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Placa { get; set; } = null!;
    public string? Photo { get; set; }
    public string? Phone { get; set; }
}