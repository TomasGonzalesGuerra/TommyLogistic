using System.ComponentModel.DataAnnotations;

namespace TommyLogistic.Shared.Entities;

public class Driver
{
    [Key]
    public string UserID { get; set; } = null!;
    public string Placa { get; set; } = null!;
    public bool Available { get; set; }

    // Navegation
    public User User { get; set; } = null!;

    public ICollection<Order>? Orders { get; set; }
    public ICollection<Carga>? Cargas { get; set; }
}
