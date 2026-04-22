namespace TommyLogistic.Shared.DTOs.Companies;

public class CompanyDTO
{
    public int Id { get; set; }
    public int Orders { get; set; }
    public bool Activa { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Telefono { get; set; }
    public string? FullName { get; set; }
    public string? Document { get; set; }
    public DateTime RegisterDate { get; set; }
}
