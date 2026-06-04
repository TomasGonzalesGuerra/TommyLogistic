using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Cargas;

public class MyCargasFilterDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 8;
    public CargaStatus? Status { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
