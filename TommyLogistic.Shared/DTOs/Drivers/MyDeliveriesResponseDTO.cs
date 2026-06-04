namespace TommyLogistic.Shared.DTOs.Drivers;

public class MyDeliveriesResponseDTO
{
    public List<MyDeliveryDTO> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}

