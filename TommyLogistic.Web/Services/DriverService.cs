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
            var responseHttp = await _repository.GetAsync<List<DriverDTO>>("api/Drivers/GetAllDrivers");

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
            var responseHttp = await _repository.PostAsync("api/Drivers", createdDTO);

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
}

//ayudame ha hacer um modal en el renderize un formulario para crear un Driver con servicio y el endpoint estoy en blazor y una api con ef:
//Driver:
//public class Driver
//{
//    [Key]
//    public string UserID { get; set; } = null!;
//    public string Placa { get; set; } = null!;
//    public bool Available { get; set; }

//    // Navegación a User
//    public User User { get; set; } = null!;

//    // Navegación a Orders
//    public ICollection<Order> Orders { get; set; } = [];
//}
//DriverCreatedDTO:
//public class DriverCreatedDTO
//{
//    public string DNI { get; set; } = null!;
//    public string Placa { get; set; } = null!;
//    public string Email { get; set; } = null!;
//    public string Celular { get; set; } = null!;
//    public string FullName { get; set; } = null!;
//}