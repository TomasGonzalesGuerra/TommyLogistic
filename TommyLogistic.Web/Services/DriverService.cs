using TommyLogistic.Web.Pages.Admin;
using TommyLogistic.Web.Repositories;
using TommyLogistic.Shared.DTOs.Drivers;
using CurrieTechnologies.Razor.SweetAlert2;

namespace TommyLogistic.Web.Services;

public class DriverService(IRepository repository, SweetAlertService sweetAlertService)
{
    private readonly IRepository _repository = repository;
    private readonly SweetAlertService _sweetAlertService = sweetAlertService;

    public async Task<List<DriverDTO>> GetAllDriversAsync()
    {
        try
        {
            var responseHppt = await _repository.GetAsync<List<DriverDTO>>("api/Drivers/GetAllDrivers");
            
            if (responseHppt.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudieron cargar los repartidores", SweetAlertIcon.Error);
                return [];
            }

            return responseHppt.Response!;
        }
        catch (Exception ex)
        {
            await _sweetAlertService.FireAsync("Error", ex.Message, SweetAlertIcon.Error);
            return [];
        }

    }
}
