using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.DTOs.Drivers;

public class ScanConfirmDTO
{
    public OrderStatus NewStatus { get; set; }
    public string? Note { get; set; }
    public string PhotoBase64 { get; set; } = null!;
    public string PhotoMimeType { get; set; } = "image/jpeg";
}
