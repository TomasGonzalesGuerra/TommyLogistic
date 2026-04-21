namespace TommyLogistic.Shared.Entities;

public class Company
{
    public int Id { get; set; }
    public bool Activa { get; set; }
    public DateTime RegisterDate { get; set; }

    // Navegación a User
    public string? UserID { get; set; }
    public User User { get; set; } = null!;

    // Navegación a Orders
    public ICollection<Order>? Orders { get; set; }
}
