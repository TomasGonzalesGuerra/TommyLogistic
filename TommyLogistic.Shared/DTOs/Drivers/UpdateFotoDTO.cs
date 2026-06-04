namespace TommyLogistic.Shared.DTOs.Drivers;

public class UpdateFotoDTO
{
    /// <summary>Base64 sin prefijo data:...</summary>
    public string PhotoBase64 { get; set; } = null!;
    public string MimeType { get; set; } = "image/jpeg";
}
