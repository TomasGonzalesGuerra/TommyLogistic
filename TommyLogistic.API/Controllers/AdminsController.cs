using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TommyLogistic.Api.Helpers;
using TommyLogistic.API.Data;
using TommyLogistic.API.Hubs;
using TommyLogistic.API.Services;
using TommyLogistic.Shared.DTOs.Admin;
using TommyLogistic.Shared.DTOs.Cargas;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(UserEnum.Admin))]
public class AdminsController(LogisticDataContext dadaContext, IUserHelper userHelper, IHubContext<NotificationHub> hubContext, OrderEventService eventService) : ControllerBase
{
    private readonly IUserHelper _userHelper = userHelper;
    private readonly LogisticDataContext _dataContext = dadaContext;
    private readonly OrderEventService _eventService = eventService;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Yop";

    // GET: api/Admins/AdminDashboard
    [HttpGet("AdminDashboard")]
    public async Task<ActionResult> GetAdminDashboardAsync()
    {
        var hoy = DateTime.UtcNow.Date;

        // Pedidos de hoy
        var pedidosHoy = await _dataContext.Orders
            .Where(o => o.RegistrationDate.Date == hoy)
            .Include(o => o.Driver).ThenInclude(d => d!.User)
            .Include(o => o.Company).ThenInclude(d => d!.User)
            .ToListAsync();

        // Últimos 10 pedidos registrados
        var ultimosPedidos = await _dataContext.Orders
            .OrderByDescending(o => o.RegistrationDate)
            .Take(5)
            .Include(o => o.Driver).ThenInclude(d => d!.User)
            .Include(o => o.Company).ThenInclude(d => d!.User)
            .Select(o => new AdminOrderFeedDTO
            {
                Id = o.Id,
                TrackingCode = o.TrackingCode,
                RecipientName = o.RecipientName,
                RecipientDistrict = o.RecipientDistrict,
                OrderStatus = o.OrderStatus.ToString(),
                DriverName = o.Driver!.User.FullName,
                CompanyName = o.Company!.User.FullName,
                RegistrationDate = o.RegistrationDate,
            })
            .ToListAsync();

        // Últimos 10 eventos de actividad
        var actividad = await _dataContext.OrderEvents
            .OrderByDescending(e => e.Timestamp)
            .Take(5)
            .Include(e => e.User)
            .Include(e => e.Order)
            .Select(e => new AdminActivityDTO
            {
                TrackingCode = e.Order!.TrackingCode,
                OrderStatus = e.NewStatus.ToString(),
                ExecutedBy = e.User!.FullName,
                ExecutedRole = e.User!.UserType.ToString(),
                EventDescription = $"{e.User.FullName} marcó el pedido como {e.NewStatus}",
                Timestamp = e.Timestamp,
            })
            .ToListAsync();

        // Drivers
        var drivers = await _dataContext.Drivers.ToListAsync();

        return Ok(new AdminDashboardDTO
        {
            TotalPedidosHoy = pedidosHoy.Count,
            EnCaminoHoy = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.OnTheWay),
            EntregadosHoy = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.Delivered),
            FallidosHoy = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.Failed),
            PendientesAsignar = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.Registered && o.DriverID == null),
            TotalDrivers = drivers.Count,
            DriversDisponibles = drivers.Count(d => d.Available),
            DriversOcupados = drivers.Count(d => !d.Available),
            UltimosPedidos = ultimosPedidos,
            ActividadReciente = actividad,
        });
    }


    // DRIVERS =========================================================================
    // GET: api/Admins/GetAllDrivers
    [HttpGet("GetAllDrivers")]
    public async Task<ActionResult<IEnumerable<DriverDTO>>> GetAllDriversAsync()
    {
        List<User> users = await _dataContext.Users
            .Include(u => u.Driver).ThenInclude(d => d!.Orders)
            .Where(u => u.UserType == UserEnum.Driver)
            .ToListAsync();

        if (users.Count == 0) return Ok(new List<DriverDTO>());
        if (users == null) return NotFound();

        List<DriverDTO> driverDTOs = [.. users.Select(u => new DriverDTO
        {
            Id = u.Id,
            FullName = u.FullName,
            Celular = u.PhoneNumber!,
            DNI = u.Document,
            Photo = u.Photo ?? string.Empty,
            Available = u.Driver!.Available,
            Placa = u.Driver.Placa,
            DeliveredToday = (int)(u.Driver!.Orders?.Count(o => o.OrderStatus == OrderStatus.Delivered))!,
            ActiveOrderToday = (int)(u.Driver!.Orders?.Count(o => o.OrderStatus == OrderStatus.Assigned))!
        })];

        return Ok(driverDTOs);
    }

    // POST: api/Admins/CreateDriver
    [HttpPost("CreateDriver")]
    public async Task<ActionResult> CreateDriverAsync(DriverCreatedDTO createdDTO)
    {
        var userExists = await _userHelper.GetUserAsync(createdDTO.Email);
        if (userExists != null) return BadRequest("El correo ya está registrado.");

        User newUser = new()
        {
            UserName = createdDTO.Email,
            Email = createdDTO.Email,
            PhoneNumber = createdDTO.Celular,
            FullName = createdDTO.FullName,
            Document = createdDTO.DNI,
            Address = string.Empty,
            Photo = string.Empty,
            UserType = UserEnum.Driver,
            Companies = [],
        };

        var result = await _userHelper.AddUserAsync(newUser, "123456");

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.FirstOrDefault()?.Description);
        }

        await _userHelper.AddUserToRoleAsync(newUser, UserEnum.Driver.ToString());

        Driver newDriver = new()
        {
            UserID = newUser.Id,
            Placa = createdDTO.Placa,
            Available = true,
            Orders = [],
        };

        _dataContext.Drivers.Add(newDriver);
        await _dataContext.SaveChangesAsync();
        return Ok();
    }

    // PUT: api/Admins/UpdateProfile
    [HttpPut("UpdateProfile")]
    public async Task<ActionResult> UpdateProfileAsync([FromBody] DriverProfileUpdateDTO model)
    {
        Driver? driver = await _dataContext.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.UserID == CurrentUserId);
        if (driver is null) return NotFound();

        driver.User.PhoneNumber = model.Phone;
        driver.User.Address = model.Address!;
        driver.Placa = model.Placa;

        if (!string.IsNullOrEmpty(model.PhotoBase64))
        {
            //TODO: Implementar lógica para guardar la foto y obtener su URL,
            //luego asignarla a driver.User.Photo
            // Aquí IFileStorage igual que en AccountsController
            // driver.User.Photo = await _fileStorage.SaveFileAsync(...);
        }

        await _dataContext.SaveChangesAsync();
        return Ok();
    }


    // Cargas ==========================================================================
    // GET: api/Admins/GetAllCargas
    [HttpGet("GetAllCargas")]
    public async Task<ActionResult<MyCargasResponseDTO>> GetAllCargasAsync(DateTime? desde = null, DateTime? hasta = null)
    {
        int page = 1, pageSize = 8;
        var query = _dataContext.Cargas.AsQueryable();

        if (desde.HasValue && hasta.HasValue)
        {
            var desdeDate = desde.Value.Date;
            var hastaDate = hasta.Value.Date.AddDays(1);
            query = query.Where(c => c.FechaCreacion >= desdeDate);
            query = query.Where(c => c.FechaCreacion < hastaDate);
        }

        int totalItems = await query.CountAsync();

        var cargas = await query
            .OrderByDescending(c => c.FechaCreacion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new MyCargaDTO
            {
                // Cargas ==============
                Id = c.Id,
                Status = c.Status,
                FechaCreacion = c.FechaCreacion,
                FechaConcluida = c.FechaConcluida,
                FechaFacturada = c.FechaFacturada,
                NotaConclusion = c.NotaConclusion,
                NotaFacturacion = c.NotaFacturacion,
                SupervisorName = c.Supervisor != null ? c.Supervisor.FullName : "—.-",
                ConcluidaPorName = c.ConcluidaPor != null ? c.ConcluidaPor.FullName : "—.-",
                FacturadaPorName = c.FacturadaPor != null ? c.FacturadaPor.FullName : "—.-",
                // Driver ==============
                DriverPlaca = c.Driver != null ? c.Driver.Placa : "—.-",
                DriverPhoto = c.Driver != null ? c.Driver.User.Photo : "—.-",
                DriverName = c.Driver != null ? c.Driver.User.FullName : "—.-",
                // Stats ===============
                TotalPedidos = c.Orders!.Count,
                Entregados = c.Orders.Count(o => o.OrderStatus == OrderStatus.Delivered),
                Fallidos = c.Orders.Count(o => o.OrderStatus == OrderStatus.Failed),
                Ausentes = c.Orders.Count(o => o.OrderStatus == OrderStatus.RecipientAbsent),
                EnRetorno = c.Orders.Count(o => o.OrderStatus == OrderStatus.Returning),
                EnAlmacen = c.Orders.Count(o => o.OrderStatus == OrderStatus.OnStorage),
                // Pedidos =============
                Pedidos = c.Orders
                    .OrderBy(o => o.RegistrationDate)
                    .Select(o => new MyCargaOrderDTO
                    {
                        Id = o.Id,
                        TrackingCode = o.TrackingCode,
                        OrderStatus = o.OrderStatus,
                        DeliveryType = o.DeliveryType,
                        RecipientName = o.RecipientName,
                        RecipientPhone = o.RecipientPhone,
                        RecipientAddress = o.RecipientAddress,
                        RecipientDistrict = o.RecipientDistrict,
                        PackageDescription = o.PackageDescription,
                        Quantity = o.Quantity,
                        RegistrationDate = o.RegistrationDate,
                        DeliveryDate = o.DeliveryDate,
                        RescheduledDate = o.RescheduledDate,
                        DeliveryAttempts = o.DeliveryAttempts,
                        CompanyName = o.Company != null && o.Company.User != null ? o.Company.User.FullName : "—.-",
                        // Eventos =============
                        Events = o.Events!
                            .OrderBy(e => e.Timestamp)
                            .Select(e => new MyCargaEventDTO
                            {
                                Timestamp = e.Timestamp,
                                NewStatus = e.NewStatus,
                                Note = e.Note,
                                BaglokLocation = e.BaglokLocation,
                                UserName = e.User != null ? e.User.FullName : "—.-"
                            }).ToList()
                    }).ToList()
            }).ToListAsync();

        return Ok(new MyCargasResponseDTO
        {
            Items = cargas,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
        });
    }


}
