namespace TommyLogistic.Shared.DTOs.Cargas;

public class PedidoDisponibleDTO
{
    public int Id { get; set; }
    public string TrackingCode { get; set; } = null!;
    public string RecipientName { get; set; } = null!;
    public string RecipientAddress { get; set; } = null!;
    public string RecipientDistrict { get; set; } = null!;
    public string PackageDescription { get; set; } = null!;
    public int Quantity { get; set; }
    public string CompanyName { get; set; } = null!;
    public DateTime RegistrationDate { get; set; }
}