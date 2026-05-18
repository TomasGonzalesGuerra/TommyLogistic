namespace TommyLogistic.Shared.DTOs.Cargas;

public class CargaCreateDTO
{
    public string DriverID { get; set; } = null!;
    public List<int> OrderIds { get; set; } = [];
}