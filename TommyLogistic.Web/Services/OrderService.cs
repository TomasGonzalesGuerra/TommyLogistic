using CurrieTechnologies.Razor.SweetAlert2;
using TommyLogistic.Shared.DTOs.Cargas;
using TommyLogistic.Shared.DTOs.Orders;
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

    public async Task<List<CargaSummaryDTO>> GetAllCargasAsync()
    {
        try
        {
            var responseHppt = await _repository.GetAsync<List<CargaSummaryDTO>>("api/Admins/GetAllCargas");

            if (responseHppt.Error)
            {
                await _sweetAlertService.FireAsync("Error", "No se pudieron cargar las cargas", SweetAlertIcon.Error);
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

    public async Task<bool> SimularRecepcionMasiva()
    {
        Random random = new();
        List<ExternalOrderDTO> listaPedidos = [];
        string[] empresas = { "naruto@yopmail.com", "angelina@yopmail.com" };
        string[] productos = { "Caja de Herramientas", "Monitor 24p", "Zapatillas Running", "Kit de Limpieza", "Teclado Mecánico", "Cafetera Express" };
        string[] nombres = { "Monkey D. Luffy", "Roronoa Zoro", "Nami Swan", "Sanji Vinsmoke", "Tony Chopper", "Nico Robin" };
        string[] distritos = { "Miraflores", "San Isidro", "Surco", "Los Olivos", "Lince" };

        for (int i = 1; i <= 30; i++)
        {
            listaPedidos.Add(new ExternalOrderDTO
            {
                CompanyEmail = empresas[random.Next(empresas.Length)],
                ProductName = $"{productos[random.Next(productos.Length)]} #{i}",
                Quantity = random.Next(1, 5),
                Recipient = nombres[random.Next(nombres.Length)],
                Address = $"Calle Ficticia {random.Next(100, 999)}",
                District = distritos[random.Next(distritos.Length)],
                Phone = $"9{random.Next(10000000, 99999999)}"
            });
        }

        try
        {
            var responseHttp = await _repository.PostAsync("api/Orders/ReceiveExternalOrders", listaPedidos);

            if (responseHttp.Error)
            {
                var message = await responseHttp.GetErrorMessageAsync();
                await _sweetAlertService.FireAsync("Error", "No se Pudo Recibir Pedidos", SweetAlertIcon.Error);
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

    public async Task<AssignmentResponse?> AutoRouteAndAssignAsync()
    {
        var r = await _repository.PostAsync<object, AssignmentResponse>("api/Orders/AutoRouteAndAssign", null!);
        return r.Error ? null : r.Response;
    }

    public class AssignmentResponse
    {
        public string Message { get; set; } = "";
        public int DriversUsed { get; set; }
        public int OrdersPending { get; set; }
    }

}
