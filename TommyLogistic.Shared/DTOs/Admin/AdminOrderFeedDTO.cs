namespace TommyLogistic.Shared.DTOs.Admin;

public class AdminOrderFeedDTO
{
    public int Id { get; set; }
    public string TrackingCode { get; set; } = null!;
    public string RecipientName { get; set; } = null!;
    public string RecipientDistrict { get; set; } = null!;
    public string OrderStatus { get; set; } = null!;
    public string? DriverName { get; set; }
    public string? CompanyName { get; set; }
    public DateTime RegistrationDate { get; set; }
}
