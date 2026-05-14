using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.Entities;

public class OrderEvent
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public OrderStatus NewStatus { get; set; }
    public string? Note { get; set; }
    public string? BaglokLocation { get; set; }

    // Navigation
    public int OrderID { get; set; }
    public Order? Order { get; set; }
    public string UserID { get; set; } = null!;
    public User? User { get; set; }
    public string? AssignedDriverID { get; set; }
    public Driver? AssignedDriver { get; set; }
}