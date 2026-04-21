using CurrieTechnologies.Razor.SweetAlert2;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.DTOs.Order;
using TommyLogistic.Web.Repositories;

namespace TommyLogistic.Web.Services;

public class OrderService(IRepository repository, SweetAlertService sweetAlertService)
{
    private readonly IRepository _repository = repository;
    private readonly SweetAlertService _sweetAlertService = sweetAlertService;

    public async Task<List<OrderSummaryDTO>> GetAllOrdersAsync()
    {
        try
        {
            var responseHppt = await _repository.GetAsync<List<OrderSummaryDTO>>("api/Orders/GetAllOrders");

            if (responseHppt.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudieron cargar los pedidos", SweetAlertIcon.Error);
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
