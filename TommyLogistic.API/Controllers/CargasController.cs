using DocumentFormat.OpenXml.InkML;
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
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{nameof(UserEnum.Admin)}, {nameof(UserEnum.Supervisor)}")]
public class CargasController(LogisticDataContext context, IHubContext<NotificationHub> hubContext, OrderEventService eventService) : ControllerBase
{
    private readonly LogisticDataContext _dadaContext = context;
    private readonly OrderEventService _eventService = eventService;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Yop";

    // ── Supervisor/Admin: crear carga ─────────────────────────────────────
    [HttpPost("Create")]
    public async Task<ActionResult> CreateCarga([FromBody] CargaCreateDTO model)
    {
        if (model.OrderIds.Count > 10)return BadRequest("Una carga no puede tener más de 10 pedidos");
        if (model.OrderIds.Count == 0)return BadRequest("Debes seleccionar al menos un pedido");

        Driver? driver = await _dadaContext.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.UserID == model.DriverID);
        if (driver is null) return NotFound("Driver no Encontrado");
        if (!driver.Available) return BadRequest("El Driver no está Disponible");

        var orders = await _dadaContext.Orders
            .Where(o => model.OrderIds.Contains(o.Id))
            .ToListAsync();

        if (orders.Count != model.OrderIds.Count)
            return BadRequest("Algunos pedidos no existen");

        if (orders.Any(o => o.OrderStatus != OrderStatus.Registered))
            return BadRequest("Solo se pueden asignar pedidos en estado Registrado");

        if (orders.Any(o => o.CargaID is not null))
            return BadRequest("Algunos pedidos ya pertenecen a una carga");

        // Validar mismo distrito
        var distritos = orders.Select(o => o.RecipientDistrict).Distinct().ToList();
        if (distritos.Count > 1)
            return BadRequest($"Todos los pedidos deben ser del mismo distrito. Encontrados: {string.Join(", ", distritos)}");

        // Crear la carga
        var carga = new Carga
        {
            DriverID = model.DriverID,
            SupervisorID = CurrentUserId,
            Status = CargaStatus.Activa,
            FechaCreacion = DateTime.UtcNow,
        };

        _dadaContext.Cargas.Add(carga);
        await _dadaContext.SaveChangesAsync();

        // Asignar pedidos a la carga
        foreach (var order in orders)
        {
            order.CargaID = carga.Id;
            order.DriverID = model.DriverID;
            order.OrderStatus = OrderStatus.Assigned;

            await _eventService.RegisterAsync(
                orderId: order.Id,
                newStatus: OrderStatus.Assigned,
                userId: CurrentUserId,
                assignedDriverId: model.DriverID,
                note: $"Asignado en carga #{carga.Id}"
            );
        }

        // Marcar driver como ocupado
        driver.Available = false;
        await _dadaContext.SaveChangesAsync();

        // Notificar al driver
        await _hubContext.Clients
            .Group($"Driver_{model.DriverID}")
            .SendAsync("NewOrderAssigned", new
            {
                Message = $"¡Tienes una nueva carga con {orders.Count} pedido(s) en {distritos[0]}!",
                CargaId = carga.Id,
                Total = orders.Count,
                Distrito = distritos[0]
            });

        // Notificar al dashboard admin
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        return Ok(carga.Id);
    }

    // GET: api/Cargas/GetAllCargas
    [HttpGet("GetAllCargas")]
    public async Task<ActionResult<IEnumerable<CargaSummaryDTO>>> GetAllCargasAsync()
    {
        List<CargaSummaryDTO> query = await _dadaContext.Cargas
            .Where(c => c.Status == CargaStatus.Activa)
            .OrderByDescending(c => c.FechaCreacion)
            .Select(c => new CargaSummaryDTO
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
                TotalPedidos = c.Orders!.Count,
                Entregados = c.Orders.Count(o => o.OrderStatus == OrderStatus.Delivered),
                EnOnStorage = c.Orders.Count(o => o.OrderStatus == OrderStatus.OnStorage),
                Pendientes = c.Orders.Count(o => o.OrderStatus == OrderStatus.Assigned),
                Distrito = c.Orders.Select(o => o.RecipientDistrict).FirstOrDefault()!,
            }).ToListAsync();

        if (query == null || query.Count == 0) return Ok(new List<CargaSummaryDTO>());

        return Ok(query);
    }

    // ── Supervisor/Admin/Operator: detalle de carga ───────────────────────
    [HttpGet("Detail/{id}")]
    [Authorize(Roles = "Supervisor,Admin,Operator")]
    public async Task<ActionResult> GetDetail(int id)
    {
        var carga = await _dadaContext.Cargas
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

    // ── Driver: solicitar conclusión ──────────────────────────────────────
    [HttpPost("SolicitarConclusion/{id}")]
    [Authorize(Roles = nameof(UserEnum.Driver))]
    public async Task<ActionResult> SolicitarConclusion(int id)
    {
        var carga = await _dadaContext.Cargas
            .Include(c => c.Orders)
            .Include(c => c.Driver).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(c => c.Id == id && c.DriverID == CurrentUserId);

        if (carga is null) return NotFound("Carga no encontrada");
        if (carga.Status != CargaStatus.Activa) return BadRequest("La carga no está activa");

        // Verificar que todos los pedidos están en estado final
        var pendientes = carga.Orders
            .Where(o => o.OrderStatus != OrderStatus.Delivered && o.OrderStatus != OrderStatus.OnStorage)
            .ToList();

        if (pendientes.Any())
            return BadRequest($"Aún tienes {pendientes.Count} pedido(s) sin finalizar");

        carga.Status = CargaStatus.PendienteConclusion;
        await _dadaContext.SaveChangesAsync();

        // Notificar a todos los Operators
        await _hubContext.Clients.Group("Operators")
            .SendAsync("CargaPendienteConclusion", new
            {
                CargaId = carga.Id,
                DriverName = carga.Driver.User.FullName,
                Total = carga.Orders.Count,
                Message = $"El driver {carga.Driver.User.FullName} solicita concluir la carga #{carga.Id}"
            });

        return Ok();
    }

    // ── Operator: confirmar conclusión ────────────────────────────────────
    [HttpPost("Concluir/{id}")]
    [Authorize(Roles = "Operator,Admin")]
    public async Task<ActionResult> Concluir(int id, [FromBody] CargaConcluirDTO model)
    {
        var carga = await _dadaContext.Cargas
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carga is null) return NotFound();
        if (carga.Status != CargaStatus.PendienteConclusion)
            return BadRequest("La carga no está pendiente de conclusión");

        carga.Status = CargaStatus.Concluida;
        carga.FechaConcluida = DateTime.UtcNow;
        carga.ConcluidaPorID = CurrentUserId;
        carga.NotaConclusion = model.Nota;

        // Liberar al driver
        var driver = await _dadaContext.Drivers
            .FirstOrDefaultAsync(d => d.UserID == carga.DriverID);

        if (driver is not null)
            driver.Available = true;

        await _dadaContext.SaveChangesAsync();

        // Notificar al driver que quedó libre
        await _hubContext.Clients
            .Group($"Driver_{carga.DriverID}")
            .SendAsync("CargaConcluida", new
            {
                CargaId = carga.Id,
                Message = $"¡Tu carga #{carga.Id} fue concluida! Ya puedes recibir nuevas cargas."
            });

        // Notificar al dashboard
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Supervisors").SendAsync("DashboardUpdate");

        return Ok();
    }

    // ── Supervisor/Admin: facturar ────────────────────────────────────────
    [HttpPost("Facturar/{id}")]
    public async Task<ActionResult> Facturar(int id, [FromBody] CargaFacturarDTO model)
    {
        var carga = await _dadaContext.Cargas
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carga is null) return NotFound();
        if (carga.Status != CargaStatus.Concluida)
            return BadRequest("Solo se pueden facturar cargas concluidas");

        carga.Status = CargaStatus.Facturada;
        carga.FechaFacturada = DateTime.UtcNow;
        carga.FacturadaPorID = CurrentUserId;
        carga.NotaFacturacion = model.Nota;

        await _dadaContext.SaveChangesAsync();

        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");

        return Ok();
    }

    // ── Operator: recibir pedido de retorno ───────────────────────────────
    [HttpPost("RecibirRetorno/{orderId}")]
    [Authorize(Roles = "Operator,Admin")]
    public async Task<ActionResult> RecibirRetorno(int orderId, [FromBody] CargaConcluirDTO model)
    {
        var order = await _dadaContext.Orders
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

        await _dadaContext.SaveChangesAsync();

        // Notificar al dashboard
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");

        return Ok();
    }

    // ── Drivers disponibles para asignar carga ────────────────────────────
    [HttpGet("DriversDisponibles")]
    public async Task<ActionResult> GetDriversDisponibles()
    {
        var drivers = await _dadaContext.Drivers
            .Include(d => d.User)
            .Where(d => d.Available)
            .Select(d => new
            {
                Id = d.UserID,
                FullName = d.User.FullName,
                Placa = d.Placa,
                Photo = d.User.Photo,
                Phone = d.User.PhoneNumber,
            })
            .ToListAsync();

        return Ok(drivers);
    }

    // ── Pedidos disponibles filtrados por distrito ────────────────────────
    [HttpGet("PedidosDisponibles")]
    public async Task<ActionResult> GetPedidosDisponibles([FromQuery] string? distrito = null)
    {
        var query = _dadaContext.Orders
            .Include(o => o.Company)
            .Where(o => o.OrderStatus == OrderStatus.Registered && o.CargaID == null);

        if (!string.IsNullOrEmpty(distrito))
            query = query.Where(o => o.RecipientDistrict == distrito);

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