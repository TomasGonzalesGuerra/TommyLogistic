using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using TommyLogistic.API.Data;
using TommyLogistic.API.Hubs;
using TommyLogistic.API.Services;
using TommyLogistic.Shared.DTOs.Cargas;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.DTOs.Orders;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{nameof(UserEnum.Admin)}, {nameof(UserEnum.Supervisor)}")]
public class CargasController(LogisticDataContext context, IHubContext<NotificationHub> hubContext, OrderEventService eventService) : ControllerBase
{
    private readonly LogisticDataContext _dataContext = context;
    private readonly OrderEventService _eventService = eventService;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Yop";


    // GET:api/Cargas/DriversDisponibles ────────────────────────────────────────────────────
    [HttpGet("DriversDisponibles")]
    public async Task<ActionResult<List<DriverDisponibleDTO>>> GetDriversDisponiblesAsync()
    {
        List<DriverDisponibleDTO> drivers = await _dataContext.Drivers
            .Where(d => d.Available)
            .Select(d => new DriverDisponibleDTO
            {
                Id = d.UserID,
                FullName = d.User.FullName,
                Placa = d.Placa,
                Photo = d.User.Photo,
                Phone = d.User.PhoneNumber
            }).ToListAsync();

        return Ok(drivers);
    }

    // GET:api/Cargas/PedidosDisponibles ────────────────────────────────────────────────────
    [HttpGet("PedidosDisponibles")]
    public async Task<ActionResult> GetPedidosDisponiblesAsync([FromQuery] string? distrito = null)
    {
        var query = _dataContext.Orders.Include(o => o.Company).Where(o => o.OrderStatus == OrderStatus.Registered && o.CargaID == null);

        if (!string.IsNullOrEmpty(distrito)) query = query.Where(o => o.RecipientDistrict == distrito);

        var pedidos = await query
            .OrderBy(o => o.RecipientDistrict)
            .ThenBy(o => o.RegistrationDate)
            .Select(o => new
            {
                o.Id,
                o.TrackingCode,
                o.RecipientName,
                o.RecipientAddress,
                o.RecipientDistrict,
                o.PackageDescription,
                o.Quantity,
                CompanyName = o.Company!.User.FullName,
                o.RegistrationDate,
            })
            .ToListAsync();

        // Agrupar por distrito para el selector
        var agrupados = pedidos
            .GroupBy(p => p.RecipientDistrict)
            .Select(g => new
            {
                Distrito = g.Key,
                Count = g.Count(),
                Pedidos = g.ToList()
            })
            .OrderBy(g => g.Distrito)
            .ToList();

        return Ok(agrupados);
    }

    // POST:api/Cargas/CreateCarga ──────────────────────────────────────────────────────────
    [HttpPost("CreateCarga")]
    public async Task<ActionResult> CreateCargaAsync([FromBody] CargaCreateDTO DTO)
    {
        if (DTO.OrderIds.Count > 10) return BadRequest("Una carga no puede tener más de 10 pedidos");
        if (DTO.OrderIds.Count == 0) return BadRequest("Debes seleccionar al menos un pedido");

        Driver? driver = await _dataContext.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.UserID == DTO.DriverID);
        if (driver is null) return NotFound("Driver no Encontrado");
        if (!driver.Available) return BadRequest("El Driver no está Disponible");

        List<Order> orders = await _dataContext.Orders.Where(o => DTO.OrderIds.Contains(o.Id)).ToListAsync();
        if (orders.Count != DTO.OrderIds.Count) return BadRequest("Algunos pedidos NO Existen");
        if (orders.Any(o => o.CargaID is not null)) return BadRequest("Algunos Pedidos Ya Tienen Carga");
        if (orders.Any(o => o.OrderStatus != OrderStatus.Registered)) return BadRequest("Solo se pueden Asignar Pedidos Registrados");

        // Validar mismo distrito
        List<string> distritos = [.. orders.Select(o => o.RecipientDistrict).Distinct()];
        if (distritos.Count > 1) return BadRequest($"Todos los pedidos deben ser del mismo Distrito. Encontrados: {string.Join(", ", distritos)}");

        // Crear la carga
        Carga carga = new()
        {
            DriverID = DTO.DriverID,
            SupervisorID = CurrentUserId,
            Status = CargaStatus.Activa,
            FechaCreacion = DateTime.UtcNow,
        };

        _dataContext.Cargas.Add(carga);
        await _dataContext.SaveChangesAsync();

        // Asignar pedidos a la carga
        foreach (var item in orders)
        {
            item.CargaID = carga.Id;
            item.DriverID = DTO.DriverID;
            item.OrderStatus = OrderStatus.Assigned;
            await _eventService.RegisterAsync(item.Id, OrderStatus.Assigned, CurrentUserId, DTO.DriverID, $"Asignado en carga #{carga.Id}");
        }

        // Marcar driver como ocupado
        driver.Available = false;
        await _dataContext.SaveChangesAsync();

        // Notificar al driver
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Drivers").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Operators").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Supervisors").SendAsync("DashboardUpdate");
        NewOrderAssignedDTO notificacion = new(orders.Count, distritos[0]);
        await _hubContext.Clients.Group($"Driver_{DTO.DriverID}").SendAsync("NewOrderAssigned", notificacion);
        return Ok(carga.Id);
    }



    

    // ── Supervisor/Admin/Operator: detalle de carga ───────────────────────
    [HttpGet("Detail/{id}")]
    [Authorize(Roles = "Supervisor,Admin,Operator")]
    public async Task<ActionResult> GetDetail(int id)
    {
        var carga = await _dataContext.Cargas
            .Include(c => c.Driver).ThenInclude(d => d!.User)
            .Include(c => c.Supervisor)
            .Include(c => c.ConcluidaPor)
            .Include(c => c.FacturadaPor)
            .Include(c => c.Orders)!.ThenInclude(o => o.Company).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carga is null) return NotFound();

        var summary = MapToSummary(carga);
        var detail = new CargaDetailDTO
        {
            Id = summary.Id,
            Status = summary.Status,
            FechaCreacion = summary.FechaCreacion,
            FechaConcluida = summary.FechaConcluida,
            FechaFacturada = summary.FechaFacturada,
            DriverID = summary.DriverID,
            DriverName = summary.DriverName,
            DriverPlaca = summary.DriverPlaca,
            DriverPhoto = summary.DriverPhoto,
            SupervisorName = summary.SupervisorName,
            TotalPedidos = summary.TotalPedidos,
            Entregados = summary.Entregados,
            EnOnStorage = summary.EnOnStorage,
            Pendientes = summary.Pendientes,
            Distrito = summary.Distrito,
            NotaConclusion = carga.NotaConclusion,
            NotaFacturacion = carga.NotaFacturacion,
            ConcluidaPorName = carga.ConcluidaPor?.FullName,
            FacturadaPorName = carga.FacturadaPor?.FullName,
            Pedidos = carga.Orders!.Select(o => new DriverOrderDTO
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
            }).ToList()
        };

        return Ok(detail);
    }

    // ── Supervisor/Admin: facturar ────────────────────────────────────────
    [HttpPost("Facturar/{id}")]
    public async Task<ActionResult> Facturar(int id, [FromBody] CargaFacturarDTO model)
    {
        var carga = await _dataContext.Cargas
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carga is null) return NotFound();
        if (carga.Status != CargaStatus.Concluida)
            return BadRequest("Solo se pueden facturar cargas concluidas");

        carga.Status = CargaStatus.Facturada;
        carga.FechaFacturada = DateTime.UtcNow;
        carga.FacturadaPorID = CurrentUserId;
        carga.NotaFacturacion = model.Nota;

        await _dataContext.SaveChangesAsync();
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Drivers").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Operators").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Supervisors").SendAsync("DashboardUpdate");

        return Ok();
    }

    // ── Operator: recibir pedido de retorno ───────────────────────────────
    [HttpPost("RecibirRetorno/{orderId}")]
    [Authorize(Roles = "Operator,Admin")]
    public async Task<ActionResult> RecibirRetorno(int orderId, [FromBody] CargaConcluirDTO model)
    {
        var order = await _dataContext.Orders
            .Include(o => o.Carga)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return NotFound("Pedido no encontrado");

        if (order.OrderStatus != OrderStatus.Returning)
            return BadRequest("El pedido no está en estado de retorno");

        order.OrderStatus = OrderStatus.OnStorage;

        await _eventService.RegisterAsync(
            orderId: orderId,
            newStatus: OrderStatus.OnStorage,
            userId: CurrentUserId,
            note: model.Nota ?? "Recibido por operario en almacén"
        );

        await _dataContext.SaveChangesAsync();
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Drivers").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Operators").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Supervisors").SendAsync("DashboardUpdate");

        return Ok();
    }

    // ── Helper ────────────────────────────────────────────────────────────
    private static CargaSummaryDTO MapToSummary(Carga c) => new()
    {
        Id = c.Id,
        Status = c.Status,
        FechaCreacion = c.FechaCreacion,
        FechaConcluida = c.FechaConcluida,
        FechaFacturada = c.FechaFacturada,
        DriverID = c.DriverID,
        DriverName = c.Driver!.User.FullName,
        DriverPlaca = c.Driver.Placa,
        DriverPhoto = c.Driver.User.Photo,
        SupervisorName = c.Supervisor!.FullName,
        TotalPedidos = c.Orders?.Count ?? 0,
        Entregados = c.Orders?.Count(o => o.OrderStatus == OrderStatus.Delivered) ?? 0,
        EnOnStorage = c.Orders?.Count(o => o.OrderStatus == OrderStatus.OnStorage) ?? 0,
        Pendientes = c.Orders?.Count(o => o.OrderStatus != OrderStatus.Delivered &&
                                              o.OrderStatus != OrderStatus.OnStorage) ?? 0,
        Distrito = c.Orders?.FirstOrDefault()?.RecipientDistrict ?? "—",
    };
}