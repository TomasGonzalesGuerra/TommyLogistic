namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverDTO
{
    // Usuario
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Document { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? Photo { get; set; }
    public string PhoneNumber { get; set; } = null!;

    // Driver
    public string Placa { get; set; } = null!;
    public bool Available { get; set; }

    // Stats globales
    public int TotalCargas { get; set; }
    public int CargasConcluidas { get; set; }
    public int TotalPedidos { get; set; }
    public int TotalEntregados { get; set; }
    public int TotalFallidos { get; set; }
}

