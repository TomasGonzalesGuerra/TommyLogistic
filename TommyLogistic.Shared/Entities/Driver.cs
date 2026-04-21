namespace TommyLogistic.Shared.Entities;

public class Driver
{
    public int Id { get; set; }
    public string? Placa { get; set; }
    public bool Available { get; set; }

    // Navegación a User
    public string? UserID { get; set; }
    public User User { get; set; } = null!;

    // Navegación a Orders
    public ICollection<Order>? Orders { get; set; }
}
