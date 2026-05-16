using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TommyLogistic.Web.Services;

public class SesionService(AuthenticationStateProvider authProvider)
{
    private readonly AuthenticationStateProvider _authProvider = authProvider;

    public async Task<ClaimsPrincipal> ObtenerUsuarioAsync()
    {
        var state = await _authProvider.GetAuthenticationStateAsync();
        return state.User;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var user = await ObtenerUsuarioAsync();
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public async Task<string?> GetNameAsync()
    {
        var user = await ObtenerUsuarioAsync();
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    public async Task<string?> GetRolAsync()
    {
        var user = await ObtenerUsuarioAsync();
        return user.FindFirst(ClaimTypes.Role)?.Value;
    }

    public async Task<bool> EsAdminAsync() => (await GetRolAsync()) == "Admin";
    public async Task<bool> EsRepartidorAsync() => (await GetRolAsync()) == "Driver";
    public async Task<bool> EsClienteEmpresaAsync() => (await GetRolAsync()) == "ClienteEmpresa";
}