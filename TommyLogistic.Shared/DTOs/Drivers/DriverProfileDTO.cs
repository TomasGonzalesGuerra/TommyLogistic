namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverProfileDTO
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Document { get; set; }
    public string? Address { get; set; }
    public string? Photo { get; set; }
    public string Placa { get; set; } = null!;
}
