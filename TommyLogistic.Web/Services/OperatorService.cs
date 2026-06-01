
using CurrieTechnologies.Razor.SweetAlert2;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Web.Repositories;

namespace TommyLogistic.Web.Services;

public class OperatorService(IRepository repository, SweetAlertService sweetAlertService)
{
    private readonly IRepository _repository = repository;
    private readonly SweetAlertService _sweetAlertService = sweetAlertService;

    public async Task<List<DriverOrderDTO>> GetOrdersByCargaAsync(int cargaID)
    {
        try
        {
            var responseHttp = await _repository.GetAsync<List<DriverOrderDTO>>($"api/Operators/GetOrdersByCarga/{cargaID}");

            if (responseHttp.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudieron cargar las Ordenes", SweetAlertIcon.Error);
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

}
