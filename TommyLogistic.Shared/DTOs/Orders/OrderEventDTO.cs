namespace TommyLogistic.Shared.DTOs.Orders;

public class OrderEventDTO
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Note { get; set; }
    public string? AssignedDriver { get; set; }
    public string Status { get; set; } = null!;
    public string ExecutedBy { get; set; } = null!;
    public string ExecutedRole { get; set; } = null!;
}