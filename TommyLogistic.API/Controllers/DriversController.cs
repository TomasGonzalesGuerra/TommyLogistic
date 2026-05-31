using Humanizer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TommyLogistic.API.Data;
using TommyLogistic.API.Hubs;
using TommyLogistic.API.Services;
using TommyLogistic.Shared.DTOs.Cargas;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(UserEnum.Driver))]
public class DriversController(LogisticDataContext dataContext, IHubContext<NotificationHub> hubContext, OrderEventService eventService) : ControllerBase
{
    private readonly LogisticDataContext _dataContext = dataContext;
    private readonly OrderEventService _eventService = eventService;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Yop";

    // GET: api/Drivers/Profile
    [HttpGet("Profile")]
    public async Task<ActionResult> GetProfile()
    {
        Driver? driver = await _dataContext.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.UserID == CurrentUserId);
        if (driver is null) return NotFound();

        return Ok(new DriverProfileDTO
        {
            FullName = driver.User.FullName,
            Email = driver.User.Email!,
            Phone = driver.User.PhoneNumber!,
            Document = driver.User.Document,
            Address = driver.User.Address,
            Photo = driver.User.Photo,
            Placa = driver.Placa,
        });
    }

    // GET: api/Drivers/Dashboard
    [HttpGet("Dashboard")]
    public async Task<ActionResult> GetDashboard()
    {
        string userID = CurrentUserId;
        var hoy = DateTime.UtcNow.Date;

        Driver? driver = await _dataContext.Drivers
            .Include(d => d.User)
            .Include(d => d.Cargas!).ThenInclude(c => c.Orders!).ThenInclude(o => o.Company).ThenInclude(co => co!.User)
            .FirstOrDefaultAsync(d => d.UserID == userID);

        if (driver is null) return NotFound();

        var pedidosHoy = driver.Cargas?.SelectMany(c => c.Orders!.Where(o => o.RegistrationDate.Date == hoy).ToList()) ?? [];

        return Ok(new DriverDashboardDTO
        {
            FullName = driver.User.FullName,
            Placa = driver.Placa,
            Photo = driver.User.Photo,
            Available = driver.Available,
            Asignados = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.Assigned),
            Retirados = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.PickedUp),
            EnCamino = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.OnTheWay),
            Entregados = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.Delivered),
            Fallidos = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.Failed),
            EnRetorno = pedidosHoy.Count(o => o.OrderStatus == OrderStatus.Returning),
            Carga = new CargaDetailDTO
            {
                Id = driver.Cargas?.FirstOrDefault(c => c.Orders!.Any(o => o.RegistrationDate.Date == hoy))?.Id ?? 0,
                Pedidos = [.. pedidosHoy.OrderBy(o => o.OrderStatus).Select(o => MapToDriverOrderDTO(o))]
            }
        });
    }

    // GET: api/Drivers/MyOrders
    [HttpGet("MyOrders")]
    public async Task<ActionResult> GetMyOrders([FromQuery] string? status = null)
    {
        string userID = CurrentUserId;
        var query = _dataContext.Orders.Include(o => o.Company).Where(o => o.DriverID == userID);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var s))
            query = query.Where(o => o.OrderStatus == s);

        var orders = await query
            .OrderByDescending(o => o.RegistrationDate)
            .Select(o => MapToDriverOrderDTO(o))
            .ToListAsync();

        return Ok(orders);
    }

    // PUT: api/Drivers/UpdateOrderStatus/{OrderID}
    [HttpPut("UpdateOrderStatus/{OrderID:int}")]
    public async Task<ActionResult> UpdateOrderStatus(int OrderID, [FromBody] DriverUpdateStatusDTO model)
    {
        string userID = CurrentUserId;
        Order? order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.Id == OrderID && o.DriverID == userID);
        if (order is null) return NotFound("Pedido NO Encontrado");

        // Validar transiciones permitidas para el driver
        var transicionValida = (order.OrderStatus, model.NewStatus) switch
        {
            (OrderStatus.Assigned, OrderStatus.PickedUp) => true,
            (OrderStatus.PickedUp, OrderStatus.OnTheWay) => true,
            (OrderStatus.OnTheWay, OrderStatus.Delivered) => true,
            (OrderStatus.OnTheWay, OrderStatus.RecipientAbsent) => true,
            (OrderStatus.OnTheWay, OrderStatus.Failed) => true,
            (OrderStatus.RecipientAbsent, OrderStatus.Returning) => true,
            (OrderStatus.Failed, OrderStatus.Returning) => true,
            _ => false
        };

        if (!transicionValida) return BadRequest($"No puedes pasar de {order.OrderStatus} a {model.NewStatus}");

        order.OrderStatus = model.NewStatus;

        // Si es entrega fallida o retorno, incrementar intentos
        if (model.NewStatus == OrderStatus.Failed || model.NewStatus == OrderStatus.RecipientAbsent)
            order.DeliveryAttempts++;

        // Si es entregado, registrar fecha
        if (model.NewStatus == OrderStatus.Delivered) order.DeliveryDate = DateTime.UtcNow;

        await _dataContext.SaveChangesAsync();

        // Registrar en historial
        await _eventService.RegisterAsync(
            orderId: OrderID,
            newStatus: model.NewStatus,
            userId: userID,
            note: model.Note
        );

        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Drivers").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Operators").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Supervisors").SendAsync("DashboardUpdate");
        return Ok();
    }

    // POST: api/Drivers/SolicitarConclusion/{CargaID:int}
    [HttpPost("SolicitarConclusion/{CargaID:int}")]
    public async Task<ActionResult> SolicitarConclusion(int CargaID)
    {
        Carga? carga = await _dataContext.Cargas
            .Include(c => c.Orders)
            .Include(c => c.Driver).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(c => c.Id == CargaID && c.DriverID == CurrentUserId);

        if (carga is null) return NotFound("Carga NO Encontrada");
        if (carga.Status != CargaStatus.Activa) return BadRequest("La carga NO está Activa");

        // Verificar que todos los pedidos están en estado final
        var pendientes = carga.Orders!.Where(o => o.OrderStatus != OrderStatus.Delivered && o.OrderStatus != OrderStatus.OnStorage).ToList();
        if (pendientes.Count != 0) return BadRequest($"Aún Tienes {pendientes.Count} Pedido(s) sin Finalizar");

        carga.Status = CargaStatus.PendienteConclusion;
        await _dataContext.SaveChangesAsync();

        // Notificar a todos los Operators
        await _hubContext.Clients.Group("Operators").SendAsync("SolicituddeConclusion", $"El driver {carga.Driver!.User.FullName} Solicita Concluir la Carga #{carga.Id}");
        return Ok();
    }





    // ── Helper privado ────────────────────────────────────────────────────
    private static DriverOrderDTO MapToDriverOrderDTO(Order o) => new()
    {
        Id = o.Id,
        TrackingCode = o.TrackingCode,
        OrderStatus = o.OrderStatus,
        DeliveryType = o.DeliveryType,
        DeliveryAttempts = o.DeliveryAttempts,
        RegistrationDate = o.RegistrationDate,
        RecipientName = o.RecipientName,
        RecipientPhone = o.RecipientPhone,
        RecipientAddress = o.RecipientAddress,
        RecipientDistrict = o.RecipientDistrict,
        PackageDescription = o.PackageDescription,
        Quantity = o.Quantity,
        CompanyName = o.Company!.User.FullName,
    };
}
