namespace TommyLogistic.Web.Auth;

public interface ILoginService
{
    Task LoginAsync(string token);
    Task LogoutAsync();
}
