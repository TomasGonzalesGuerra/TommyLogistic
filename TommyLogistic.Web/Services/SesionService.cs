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

    public async Task<string?> GetRolAsync()
    {
        var user = await ObtenerUsuarioAsync();
        return user.FindFirst(ClaimTypes.Role)?.Value;
    }

    public async Task<string?> GetNameAsync()
    {
        var user = await ObtenerUsuarioAsync();
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    public async Task<int?> ObtenerEmpresaIdAsync()
    {
        var user = await ObtenerUsuarioAsync();
        var val = user.FindFirst("EmpresaId")?.Value;
        return int.TryParse(val, out var id) ? id : null;
    }

    public async Task<int?> ObtenerRepartidorIdAsync()
    {
        var user = await ObtenerUsuarioAsync();
        var val = user.FindFirst("RepartidorId")?.Value;
        return int.TryParse(val, out var id) ? id : null;
    }

    public async Task<bool> EsAdminAsync() => (await GetRolAsync()) == "Admin";
    public async Task<bool> EsRepartidorAsync() => (await GetRolAsync()) == "Repartidor";
    public async Task<bool> EsClienteEmpresaAsync() => (await GetRolAsync()) == "ClienteEmpresa";
}