namespace TommyLogistic.Shared.DTOs.Auth;

public class TokenDTO
{
    public string? Token { get; set; }
    public DateTime Expiration { get; set; }
}
