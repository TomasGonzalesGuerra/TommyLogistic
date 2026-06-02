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
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(UserEnum.Operator))]
public class OperatorsController(LogisticDataContext dataContext, IHubContext<NotificationHub> hubContext, OrderEventService eventService) : ControllerBase
{
    private readonly LogisticDataContext _dataContext = dataContext;
    private readonly OrderEventService _eventService = eventService;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Yop";

    // GET: api/Operators/GetByStatus?status=Pending
    [HttpGet("GetByStatus")]
    public async Task<ActionResult> GetByStatus([FromQuery] string status)
    {
        if (!Enum.TryParse<OrderStatus>(status, out var orderStatus))
            return BadRequest("Estado inválido");

        var orders = await _dataContext.Orders
            .Include(o => o.Company).ThenInclude(d => d!.User)
            .Include(o => o.Driver).ThenInclude(d => d!.User)
            .Where(o => o.OrderStatus == orderStatus)
            .OrderByDescending(o => o.RegistrationDate)
            .Select(o => new DriverOrderDTO
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
            })
            .ToListAsync();

        return Ok(orders);
    }

    // GET: api/Operators/GetAllCargas
    [HttpGet("GetAllCargas")]
    public async Task<ActionResult<IEnumerable<CargaSummaryDTO>>> GetAllCargasAsync()
    {
        List<CargaSummaryDTO> query = await _dataContext.Cargas
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

    // GET: api/Operators/GetPendentingCargas
    [HttpGet("GetPendentingCargas")]
    public async Task<ActionResult<IEnumerable<CargaSummaryDTO>>> GetPendentingCargasAsync()
    {
        List<CargaSummaryDTO> query = await _dataContext.Cargas
            .Where(c => c.Status == CargaStatus.PendienteConclusion)
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

    // GET: api/Operators/GetOrdersByCarga/{CargaID}
    [HttpGet("GetOrdersByCarga/{CargaID:int}")]
    public async Task<ActionResult<List<DriverOrderDTO>>> GetOrdersByCargaAsync(int CargaID)
    {
        Carga? carga = await _dataContext.Cargas
            .Include(c => c.Orders!).ThenInclude(o => o.Driver).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(c => c.Id == CargaID);
        if (carga is null) return BadRequest("La Carga no Está DISPONIBLE");

        List<DriverOrderDTO> orders = [.. carga.Orders!.Select(o => new DriverOrderDTO
        {
            Id = o.Id,
            TrackingCode = o.TrackingCode,
            OrderStatus = o.OrderStatus,
            DeliveryType = o.DeliveryType,
            DeliveryAttempts = o.DeliveryAttempts,
            RegistrationDate = o.RegistrationDate,
            Quantity = o.Quantity,
            CompanyName = "Company",
            RecipientName = o.RecipientName,
            RecipientPhone = o.RecipientPhone,
            RecipientAddress = o.RecipientAddress,
            RecipientDistrict = o.RecipientDistrict,
            PackageDescription = o.PackageDescription,
        })];

        return Ok(orders);
    }

    // POST: api/Operators/ConcludeCarga/{CargaID}
    [HttpPost("ConcludeCarga/{CargaID:int}")]
    public async Task<ActionResult> ConcludeCargaAsync(int CargaID, [FromBody] CargaConcluirDTO DTO)
    {
        Carga? carga = await _dataContext.Cargas.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == CargaID);
        if (carga is null) return NotFound();
        if (carga.Status != CargaStatus.PendienteConclusion) return BadRequest("La carga no está pendiente de conclusión");

        carga.Status = CargaStatus.Concluida;
        carga.FechaConcluida = DateTime.UtcNow;
        carga.ConcluidaPorID = CurrentUserId;
        carga.NotaConclusion = DTO.Nota;

        // Liberar al driver
        Driver? driver = await _dataContext.Drivers.FirstOrDefaultAsync(d => d.UserID == carga.DriverID);
        driver?.Available = true;

        await _dataContext.SaveChangesAsync();

        // Notificar al dashboard
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Drivers").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Operators").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Supervisors").SendAsync("DashboardUpdate");

        return Ok();
    }

}
