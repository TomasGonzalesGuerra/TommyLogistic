
using CurrieTechnologies.Razor.SweetAlert2;
using TommyLogistic.Shared.DTOs.Cargas;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.DTOs.Operators;
using TommyLogistic.Shared.Enums;
using TommyLogistic.Web.Repositories;

namespace TommyLogistic.Web.Services;

public class OperatorService(IRepository repository, SweetAlertService sweetAlertService)
{
    private readonly IRepository _repository = repository;
    private readonly SweetAlertService _sweetAlertService = sweetAlertService;


    public async Task<List<CargaSummaryDTO>> GetDashboardAsync()
    {
        try
        {
            var responseHttp = await _repository.GetAsync<List<CargaSummaryDTO>>("api/Operators/GetDashboard");

            if (responseHttp.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudieron cargar las cargas", SweetAlertIcon.Error);
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

    public async Task<bool> ConcludeCargaAsync(int cargaID, CargaConcluirDTO DTO)
    {
        try
        {
            var responseHttp = await _repository.PostAsync($"api/Operators/ConcludeCarga/{cargaID}", DTO);
            if (responseHttp.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudo finalizar la Carga", SweetAlertIcon.Error);
                return false;
            }
            await _sweetAlertService.FireAsync("Éxito", "Carga finalizada correctamente", SweetAlertIcon.Success);
            return true;
        }
        catch (Exception ex)
        {
            await _sweetAlertService.FireAsync("Error", ex.Message, SweetAlertIcon.Error);
            return false;
        }
    }

    public async Task<List<DriverEnRutaDTO>> GetDriversEnRutaAsync()
    {
        try
        {
            var response = await _repository.GetAsync<List<DriverEnRutaDTO>>("api/Operators/DriversEnRuta");

            if (response.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudieron cargar los drivers", SweetAlertIcon.Error);
                return [];
            }

            return response.Response!;
        }
        catch (Exception ex)
        {
            await _sweetAlertService.FireAsync("Error", ex.Message, SweetAlertIcon.Error);
            return [];
        }
    }

    public async Task<DriverCargaDetalleDTO?> GetCargaByDriverAsync(string driverID)
    {
        try
        {
            var response = await _repository.GetAsync<DriverCargaDetalleDTO>($"api/Operators/DriversEnRuta/{driverID}/Carga");

            if (response.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudo cargar el detalle", SweetAlertIcon.Error);
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
    
    public async Task<CargasHoyResponseDTO?> GetCargasHoyAsync(int page = 1, int pageSize = 10,CargaStatus? status = null, string? driverID = null)
    {
        try
        {
            var qs = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}",
            };

            if (status.HasValue)
                qs.Add($"status={status.Value}");
            if (!string.IsNullOrEmpty(driverID))
                qs.Add($"driverID={Uri.EscapeDataString(driverID)}");

            var url = $"api/Operators/CargasHoy?{string.Join("&", qs)}";
            var response = await _repository.GetAsync<CargasHoyResponseDTO>(url);

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

    public async Task<List<DriverSelectorDTO>> GetDriversConCargaHoyAsync()
    {
        try
        {
            var response = await _repository.GetAsync<List<DriverSelectorDTO>>("api/Operators/CargasHoy/Drivers");

            if (response.Error) return [];
            return response.Response!;
        }
        catch
        {
            return [];
        }
    }


}
