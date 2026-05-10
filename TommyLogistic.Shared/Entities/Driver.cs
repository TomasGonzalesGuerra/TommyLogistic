using System.ComponentModel.DataAnnotations;

namespace TommyLogistic.Shared.Entities;

public class Driver
{
    [Key]
    public string UserID { get; set; } = null!;
    public string Placa { get; set; } = null!;
    public bool Available { get; set; }

    // Navegación a User
    public User User { get; set; } = null!;

    // Navegación a Orders
    public ICollection<Order>? Orders { get; set; }
}
