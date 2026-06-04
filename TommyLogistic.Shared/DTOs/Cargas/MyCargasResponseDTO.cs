namespace TommyLogistic.Shared.DTOs.Cargas;

public class MyCargasResponseDTO
{
    public List<MyCargaDTO> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
