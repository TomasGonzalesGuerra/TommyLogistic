using CurrieTechnologies.Razor.SweetAlert2;
using TommyLogistic.Shared.DTOs.Cargas;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Web.Repositories;

namespace TommyLogistic.Web.Services;

public class AdminService(IRepository repository, SweetAlertService sweetAlertService)
{
    private readonly IRepository _repository = repository;
    private readonly SweetAlertService _sweetAlertService = sweetAlertService;

    //══════════════════════ DRIVERS ══════════════════════
    public async Task<bool> CreateDriverAsync(DriverCreatedDTO createdDTO)
    {
        try
        {
            var responseHttp = await _repository.PostAsync("api/Admins/CreateDriver", createdDTO);

            if (responseHttp.Error)
            {
                var message = await responseHttp.GetErrorMessageAsync();
                await _sweetAlertService.FireAsync("Error", "No se pudieron crear el repartidor", SweetAlertIcon.Error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            await _sweetAlertService.FireAsync("Error", ex.Message, SweetAlertIcon.Error);
            return false;
        }

    }

    public async Task<List<DriverDTO>> GetAllDriversAsync()
    {
        try
        {
            var responseHttp = await _repository.GetAsync<List<DriverDTO>>("api/Admins/GetAllDrivers");

            if (responseHttp.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudieron cargar los repartidores", SweetAlertIcon.Error);
                return [];
            }

            return responseHttp.Response!;
        }
        catch (Exception ex)
        {
            await _sweetAlertService.FireAsync("Error", ex.Message, SweetAlertIcon.Error);
            return [];
        }

    }

    //══════════════════════ CARGAS ══════════════════════
    public async Task<MyCargasResponseDTO?> GetMyCargasAsync(DateTime? desde = null, DateTime? hasta = null)
    {
        try
        {
            var qs = new List<string> { $"page={1}", $"pageSize={8}", };

            if (desde.HasValue) qs.Add($"desde={desde.Value:yyyy-MM-dd}");
            if (hasta.HasValue) qs.Add($"hasta={hasta.Value:yyyy-MM-dd}");

            var url = $"api/Admins/GetAllCargas?{string.Join("&", qs)}";
            var response = await _repository.GetAsync<MyCargasResponseDTO>(url);

            if (response.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudieron cargar las cargas", SweetAlertIcon.Error);
                return null;
            }

            return response.Response;
        }
        catch (Exception ex)
        {
            await _sweetAlertService.FireAsync("Error", ex.Message, SweetAlertIcon.Error);
            return null;
        }
    }

}
