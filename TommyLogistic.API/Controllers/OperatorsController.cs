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
using TommyLogistic.Shared.DTOs.Operators;
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

    // GET: api/Operators/GetDashboard
    [HttpGet("GetDashboard")]
    public async Task<ActionResult<IEnumerable<CargaSummaryDTO>>> GetDashboardAsync()
    {
        var statusActivos = new[] { CargaStatus.Activa, CargaStatus.Pendiente };

        List<CargaSummaryDTO> query = await _dataContext.Cargas
            .Where(c => statusActivos.Contains(c.Status))
            .OrderBy(c => c.Status == CargaStatus.Pendiente ? 0 : 1) // Pendientes primero
            .ThenByDescending(c => c.FechaCreacion)
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
        if (carga.Status != CargaStatus.Pendiente) return BadRequest("La carga no está pendiente de conclusión");

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

    // GET: api/Operators/DriversEnRuta
    [HttpGet("DriversEnRuta")]
    public async Task<ActionResult<List<DriverEnRutaDTO>>> GetDriversEnRuta()
    {
        var statusActivos = new[] { CargaStatus.Activa, CargaStatus.Pendiente };

        var cargas = await _dataContext.Cargas
            .Where(c => statusActivos.Contains(c.Status))
            .Include(c => c.Driver!).ThenInclude(d => d.User)
            .Include(c => c.Supervisor)
            .Include(c => c.Orders!)
                .ThenInclude(o => o.Events!).ThenInclude(e => e.User)
            .OrderBy(c => c.Status == CargaStatus.Pendiente ? 0 : 1)
            .ThenByDescending(c => c.FechaCreacion)
            .ToListAsync();

        var result = cargas.Select(c =>
        {
            var orders = c.Orders?.ToList() ?? [];

            // Últimos 5 eventos de toda la carga, ordenados por más reciente
            var ultimosEventos = orders
                .SelectMany(o => (o.Events ?? []).Select(e => new UltimoEventoDTO
                {
                    Timestamp = e.Timestamp,
                    NewStatus = e.NewStatus,
                    TrackingCode = o.TrackingCode,
                    RecipientName = o.RecipientName,
                    Note = e.Note,
                }))
                .OrderByDescending(e => e.Timestamp)
                .Take(5)
                .ToList();

            return new DriverEnRutaDTO
            {
                DriverID = c.DriverID,
                DriverName = c.Driver!.User.FullName,
                DriverPlaca = c.Driver.Placa,
                DriverPhoto = c.Driver.User.Photo,
                CargaId = c.Id,
                CargaStatus = c.Status,
                FechaCreacion = c.FechaCreacion,
                SupervisorName = c.Supervisor?.FullName ?? "—",

                TotalPedidos = orders.Count,
                Entregados = orders.Count(o => o.OrderStatus == OrderStatus.Delivered),
                EnCamino = orders.Count(o => o.OrderStatus == OrderStatus.OnTheWay),
                Pendientes = orders.Count(o => o.OrderStatus is
                                   OrderStatus.Assigned or OrderStatus.PickedUp),
                Fallidos = orders.Count(o => o.OrderStatus == OrderStatus.Failed),
                Ausentes = orders.Count(o => o.OrderStatus == OrderStatus.RecipientAbsent),
                EnRetorno = orders.Count(o => o.OrderStatus == OrderStatus.Returning),

                UltimosEventos = ultimosEventos,
            };
        }).ToList();

        return Ok(result);
    }

    // GET: api/Operators/DriversEnRuta/{driverID}/Carga
    [HttpGet("DriversEnRuta/{driverID}/Carga")]
    public async Task<ActionResult<DriverCargaDetalleDTO>> GetCargaByDriver(string driverID)
    {
        var statusActivos = new[] { CargaStatus.Activa, CargaStatus.Pendiente };

        var carga = await _dataContext.Cargas
            .Where(c => c.DriverID == driverID && statusActivos.Contains(c.Status))
            .Include(c => c.Driver!).ThenInclude(d => d.User)
            .Include(c => c.Supervisor)
            .Include(c => c.Orders!)
                .ThenInclude(o => o.Company).ThenInclude(co => co.User)
            .Include(c => c.Orders!)
                .ThenInclude(o => o.Events!).ThenInclude(e => e.User)
            .FirstOrDefaultAsync();

        if (carga is null) return NotFound("Este driver no tiene carga activa.");

        return Ok(new DriverCargaDetalleDTO
        {
            DriverName = carga.Driver!.User.FullName,
            DriverPlaca = carga.Driver.Placa,
            DriverPhoto = carga.Driver.User.Photo,
            CargaId = carga.Id,
            CargaStatus = carga.Status,
            FechaCreacion = carga.FechaCreacion,
            SupervisorName = carga.Supervisor?.FullName ?? "—",
            NotaConclusion = carga.NotaConclusion,

            Pedidos = [.. (carga.Orders ?? [])
                .OrderBy(o => o.OrderStatus)
                .Select(o => new PedidoEnRutaDTO
                {
                    Id = o.Id,
                    TrackingCode = o.TrackingCode,
                    OrderStatus = o.OrderStatus,
                    RecipientName = o.RecipientName,
                    RecipientPhone = o.RecipientPhone,
                    RecipientAddress = o.RecipientAddress,
                    RecipientDistrict = o.RecipientDistrict,
                    PackageDescription = o.PackageDescription,
                    Quantity = o.Quantity,
                    DeliveryAttempts = o.DeliveryAttempts,
                    DeliveryDate = o.DeliveryDate,
                    CompanyName = o.Company?.User?.FullName ?? "—",
                    Events = [.. (o.Events ?? [])
                        .OrderByDescending(e => e.Timestamp)
                        .Select(e => new MiCargaEventoDTO
                        {
                            Timestamp = e.Timestamp,
                            NewStatus = e.NewStatus,
                            Note = e.Note,
                            BaglokLocation = e.BaglokLocation,
                            UserName = e.User?.FullName ?? "—",
                        })]
                })]
        });
    }

    // GET: api/Operators/CargasHoy
    [HttpGet("CargasHoy")]
    public async Task<ActionResult<CargasHoyResponseDTO>> GetCargasHoy([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] CargaStatus? status = null, [FromQuery] string? driverID = null)
    {
        var hoy = DateTime.UtcNow.Date;

        var query = _dataContext.Cargas
            .Where(c => c.FechaCreacion.Date == hoy)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (!string.IsNullOrEmpty(driverID))
            query = query.Where(c => c.DriverID == driverID);

        int totalItems = await query.CountAsync();

        var cargas = await query
            .OrderBy(c => c.Status)
            .ThenByDescending(c => c.FechaCreacion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Driver!).ThenInclude(d => d.User)
            .Include(c => c.Supervisor)
            .Include(c => c.ConcluidaPor)
            .Include(c => c.FacturadaPor)
            .Include(c => c.Orders!)
                .ThenInclude(o => o.Company).ThenInclude(co => co.User)
            .Include(c => c.Orders!)
                .ThenInclude(o => o.Events!).ThenInclude(e => e.User)
            .ToListAsync();

        var items = cargas.Select(c =>
        {
            var orders = c.Orders?.ToList() ?? [];
            return new CargaHoyDTO
            {
                Id = c.Id,
                Status = c.Status,
                FechaCreacion = c.FechaCreacion,
                FechaConcluida = c.FechaConcluida,
                FechaFacturada = c.FechaFacturada,
                NotaConclusion = c.NotaConclusion,
                NotaFacturacion = c.NotaFacturacion,
                DriverID = c.DriverID,
                DriverName = c.Driver!.User.FullName,
                DriverPlaca = c.Driver.Placa,
                DriverPhoto = c.Driver.User.Photo,
                SupervisorName = c.Supervisor?.FullName ?? "—",
                ConcluidaPorName = c.ConcluidaPor?.FullName,
                FacturadaPorName = c.FacturadaPor?.FullName,

                TotalPedidos = orders.Count,
                Entregados = orders.Count(o => o.OrderStatus == OrderStatus.Delivered),
                EnCamino = orders.Count(o => o.OrderStatus == OrderStatus.OnTheWay),
                Pendientes = orders.Count(o => o.OrderStatus is
                                   OrderStatus.Assigned or OrderStatus.PickedUp),
                Fallidos = orders.Count(o => o.OrderStatus == OrderStatus.Failed),
                Ausentes = orders.Count(o => o.OrderStatus == OrderStatus.RecipientAbsent),
                EnRetorno = orders.Count(o => o.OrderStatus == OrderStatus.Returning),
                EnAlmacen = orders.Count(o => o.OrderStatus == OrderStatus.OnStorage),

                Pedidos = orders
                    .OrderBy(o => o.OrderStatus)
                    .Select(o => new PedidoEnRutaDTO
                    {
                        Id = o.Id,
                        TrackingCode = o.TrackingCode,
                        OrderStatus = o.OrderStatus,
                        RecipientName = o.RecipientName,
                        RecipientPhone = o.RecipientPhone,
                        RecipientAddress = o.RecipientAddress,
                        RecipientDistrict = o.RecipientDistrict,
                        PackageDescription = o.PackageDescription,
                        Quantity = o.Quantity,
                        DeliveryAttempts = o.DeliveryAttempts,
                        DeliveryDate = o.DeliveryDate,
                        CompanyName = o.Company?.User?.FullName ?? "—",
                        Events = (o.Events ?? [])
                            .OrderByDescending(e => e.Timestamp)
                            .Select(e => new MiCargaEventoDTO
                            {
                                Timestamp = e.Timestamp,
                                NewStatus = e.NewStatus,
                                Note = e.Note,
                                BaglokLocation = e.BaglokLocation,
                                UserName = e.User?.FullName ?? "—",
                            }).ToList()
                    }).ToList()
            };
        }).ToList();

        return Ok(new CargasHoyResponseDTO
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
        });
    }

    // GET: api/Operators/CargasHoy/Drivers
    [HttpGet("CargasHoy/Drivers")]
    public async Task<ActionResult<List<DriverSelectorDTO>>> GetDriversConCargaHoy()
    {
        var hoy = DateTime.UtcNow.Date;

        var drivers = await _dataContext.Cargas
            .Where(c => c.FechaCreacion.Date == hoy)
            .Include(c => c.Driver!).ThenInclude(d => d.User)
            .Select(c => new DriverSelectorDTO
            {
                DriverID = c.DriverID,
                DriverName = c.Driver!.User.FullName,
                DriverPlaca = c.Driver.Placa,
            })
            .Distinct()
            .OrderBy(d => d.DriverName)
            .ToListAsync();

        return Ok(drivers);
    }


}
