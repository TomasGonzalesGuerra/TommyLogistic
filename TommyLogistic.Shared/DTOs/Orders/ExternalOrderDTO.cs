namespace TommyLogistic.Shared.DTOs.Orders;

public class ExternalOrderDTO
{
    public int Quantity { get; set; }
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string District { get; set; } = null!;
    public string Recipient { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string CompanyEmail { get; set; } = null!;
}
