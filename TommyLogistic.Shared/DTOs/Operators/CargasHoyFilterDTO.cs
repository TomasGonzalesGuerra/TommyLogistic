using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Operators;

public class CargasHoyFilterDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public CargaStatus? Status { get; set; }
    public string? DriverID { get; set; }
}
