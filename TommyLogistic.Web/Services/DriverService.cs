using CurrieTechnologies.Razor.SweetAlert2;
using TommyLogistic.Shared.DTOs.Cargas;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.Enums;
using TommyLogistic.Web.Repositories;

namespace TommyLogistic.Web.Services;

public class DriverService(IRepository repository, SweetAlertService sweetAlertService)
{
    private readonly IRepository _repository = repository;
    private readonly SweetAlertService _sweetAlertService = sweetAlertService;

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

    public async Task<DriverDashboardDTO?> GetDashboardAsync()
    {
        var r = await _repository.GetAsync<DriverDashboardDTO>("api/Drivers/Dashboard");
        return r.Error ? null : r.Response;
    }

    public async Task<List<DriverOrderDTO>> GetMyOrdersAsync(string? status = null)
    {
        var url = string.IsNullOrEmpty(status)
            ? "api/Drivers/MyOrders"
            : $"api/Drivers/MyOrders?status={status}";
        var r = await _repository.GetAsync<List<DriverOrderDTO>>(url);
        return r.Error ? [] : r.Response!;
    }

    public async Task<bool> UpdateAllAsync(List<DriverOrderDTO> orders, DriverUpdateStatusDTO dto)
    {
        var tasks = orders.Select(order => UpdateOrderStatusAsync(order.Id, dto));
        var results = await Task.WhenAll(tasks);
        return results.All(r => r);
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, DriverUpdateStatusDTO dto)
    {
        var r = await _repository.PutAsync($"api/Drivers/UpdateOrderStatus/{orderId}", dto);
        return !r.Error;
    }

    public async Task<DriverProfileDTO?> GetProfileAsync()
    {
        var r = await _repository.GetAsync<DriverProfileDTO>("api/Drivers/Profile");
        return r.Error ? null : r.Response;
    }

    public async Task<bool> UpdateProfileAsync(DriverProfileUpdateDTO dto)
    {
        var r = await _repository.PutAsync("api/Drivers/Profile", dto);
        return !r.Error;
    }

    public async Task<bool> SolicitarConclusionAsync(int CargaID)
    {
        var r = await _repository.PostAsync<object>( $"api/Drivers/SolicitarConclusion/{CargaID}", null!);
        return !r.Error;
    }

    public async Task<MyDeliveriesResponseDTO?> GetMyDeliveriesAsync(
        int page = 1,
        int pageSize = 10,
        OrderStatus? status = null,
        DateTime? desde = null,
        DateTime? hasta = null)
    {
        try
        {
            var qs = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
        };

            if (status.HasValue) qs.Add($"status={status.Value}");
            if (desde.HasValue) qs.Add($"desde={desde.Value:yyyy-MM-dd}");
            if (hasta.HasValue) qs.Add($"hasta={hasta.Value:yyyy-MM-dd}");

            var url = $"api/Drivers/MyDeliveries?{string.Join("&", qs)}";
            var response = await _repository.GetAsync<MyDeliveriesResponseDTO>(url);

            if (response.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudieron cargar las entregas", SweetAlertIcon.Error);
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

    public async Task<MyCargasResponseDTO?> GetMyCargasAsync(int page = 1, int pageSize = 8, CargaStatus? status = null, DateTime? desde = null, DateTime? hasta = null)
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
            if (desde.HasValue)
                qs.Add($"desde={desde.Value:yyyy-MM-dd}");
            if (hasta.HasValue)
                qs.Add($"hasta={hasta.Value:yyyy-MM-dd}");

            var url = $"api/Drivers/MisCargas?{string.Join("&", qs)}";
            var response = await _repository.GetAsync<MyCargasResponseDTO                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   >(url);

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
