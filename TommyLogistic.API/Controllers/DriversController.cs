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

    // GET: api/Drivers/Miperfil
    [HttpGet("Miperfil")]
    public async Task<ActionResult<MiPerfilDTO>> GetMiPerfil()
    {
        string userID = CurrentUserId;

        var driver = await _dataContext.Drivers
            .Include(d => d.User)
            .Include(d => d.Cargas!)
                .ThenInclude(c => c.Orders)
            .FirstOrDefaultAsync(d => d.UserID == userID);

        if (driver is null) return NotFound();

        var todasLasCargas = driver.Cargas?.ToList() ?? [];
        var todosPedidos = todasLasCargas.SelectMany(c => c.Orders ?? []).ToList();

        return Ok(new MiPerfilDTO
        {
            FullName = driver.User.FullName,
            Email = driver.User.Email!,
            Document = driver.User.Document,
            Address = driver.User.Address,
            Photo = driver.User.Photo,
            PhoneNumber = driver.User.PhoneNumber ?? "—",
            Placa = driver.Placa,
            Available = driver.Available,
            TotalCargas = todasLasCargas.Count,
            CargasConcluidas = todasLasCargas.Count(c => c.Status is CargaStatus.Concluida or CargaStatus.Facturada),
            TotalPedidos = todosPedidos.Count,
            TotalEntregados = todosPedidos.Count(o => o.OrderStatus == OrderStatus.Delivered),
            TotalFallidos = todosPedidos.Count(o => o.OrderStatus == OrderStatus.Failed),
        });
    }

    [HttpPut("MiPerfil/Foto")]
    public async Task<ActionResult> UpdateFoto([FromBody] UpdateFotoDTO dto)
    {
        //string userID = CurrentUserId;

        //var user = await _userManager.FindByIdAsync(userID);
        //if (user is null) return NotFound();

        //// Convierte base64 a URL o ruta según tu estrategia de almacenamiento.
        //// Opción A — guardar como archivo en wwwroot:
        //var fileName = $"{userID}_{DateTime.UtcNow.Ticks}.jpg";
        //var folder = Path.Combine(_env.WebRootPath, "photos", "drivers");
        //Directory.CreateDirectory(folder);
        //var filePath = Path.Combine(folder, fileName);
        //var bytes = Convert.FromBase64String(dto.PhotoBase64);
        //await System.IO.File.WriteAllBytesAsync(filePath, bytes);
        //user.Photo = $"/photos/drivers/{fileName}";

        //// Opción B — si usas un servicio de storage (S3, Azure Blob, etc.)
        //// user.Photo = await _storageService.UploadAsync(dto.PhotoBase64, dto.MimeType);

        //await _userManager.UpdateAsync(user);

        //return Ok(new { photo = user.Photo });
        return Ok();
    }

    // GET: api/Drivers/Dashboard
    [HttpGet("Dashboard")]
    public async Task<ActionResult> GetDashboard()
    {
        string userID = CurrentUserId;

        Driver? driver = await _dataContext.Drivers
            .Include(d => d.User)
            .Include(d => d.Cargas!).ThenInclude(c => c.ConcluidaPor)
            .Include(d => d.Cargas!).ThenInclude(c => c.FacturadaPor)
            .Include(d => d.Cargas!).ThenInclude(c => c.Orders!).ThenInclude(o => o.Company).ThenInclude(co => co!.User)
            .FirstOrDefaultAsync(d => d.UserID == userID);

        if (driver is null) return NotFound();

        var todasLasCargas = driver.Cargas?.ToList() ?? [];

        // Carga activa = Activa o PendienteConclusion (el driver aún la ve)
        var cargaActiva = todasLasCargas.FirstOrDefault(c => c.Status is CargaStatus.Activa or CargaStatus.PendienteConclusion);

        // Historial = todo lo demás, más reciente primero
        var cargasAnteriores = todasLasCargas
            .Where(c => c.Status is not (CargaStatus.Activa or CargaStatus.PendienteConclusion))
            .OrderByDescending(c => c.FechaCreacion)
            .ToList();

        // Stats del día (solo de la carga activa)
        var pedidosActivos = cargaActiva?.Orders?.ToList() ?? [];

        return Ok(new DriverDashboardDTO
        {
            FullName = driver.User.FullName,
            Placa = driver.Placa,
            Photo = driver.User.Photo,
            Available = driver.Available,
            Asignados = pedidosActivos.Count(o => o.OrderStatus == OrderStatus.Assigned),
            Retirados = pedidosActivos.Count(o => o.OrderStatus == OrderStatus.PickedUp),
            EnCamino = pedidosActivos.Count(o => o.OrderStatus == OrderStatus.OnTheWay),
            Entregados = pedidosActivos.Count(o => o.OrderStatus == OrderStatus.Delivered),
            Fallidos = pedidosActivos.Count(o => o.OrderStatus == OrderStatus.Failed),
            EnRetorno = pedidosActivos.Count(o => o.OrderStatus == OrderStatus.Returning),
            CargaActiva = cargaActiva is null ? null : MapToCargaDetail(cargaActiva),
            CargasAnteriores = [.. cargasAnteriores.Select(MapToCargaDetail)]
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

    // PUT: api/Drivers/UpdateOrderStatus/{OrderID:int}
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
        var pendientes = carga.Orders!.Where(o => o.OrderStatus != OrderStatus.Delivered && o.OrderStatus != OrderStatus.Returning).ToList();
        if (pendientes.Count != 0) return BadRequest($"Aún Tienes {pendientes.Count} Pedido(s) sin Finalizar");

        carga.Status = CargaStatus.PendienteConclusion;
        await _dataContext.SaveChangesAsync();

        // Notificar a todos los Operators
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Drivers").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Supervisors").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Operators").SendAsync("SolicituddeConclusion", $"El driver {carga.Driver!.User.FullName} Solicita Concluir la Carga #{carga.Id}");
        return Ok();
    }

    // GET: api/Drivers/MyOrders
    [HttpGet("MyDeliveries")]
    public async Task<ActionResult<MyDeliveriesResponseDTO>> GetMyDeliveriesAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] OrderStatus? status = null, [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
    {
        string userID = CurrentUserId;

        // Base query — todos los pedidos del driver que ya tuvieron actividad
        var query = _dataContext.Orders
            .Include(o => o.Company).ThenInclude(c => c!.User)
            .Where(o => o.DriverID == userID
                     && o.OrderStatus != OrderStatus.Registered  // excluir solo registrados sin asignar
                     && o.OrderStatus != OrderStatus.Assigned)   // excluir los activos sin movimiento
            .AsQueryable();

        // Filtro por status
        if (status.HasValue)
            query = query.Where(o => o.OrderStatus == status.Value);

        // Filtro por fecha (usando RegistrationDate)
        if (desde.HasValue)
            query = query.Where(o => o.RegistrationDate.Date >= desde.Value.Date);
        if (hasta.HasValue)
            query = query.Where(o => o.RegistrationDate.Date <= hasta.Value.Date);

        // Total antes de paginar
        int totalItems = await query.CountAsync();

        // Paginación + orden
        var items = await query
            .OrderByDescending(o => o.RegistrationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new MyDeliveryDTO
            {
                Id = o.Id,
                TrackingCode = o.TrackingCode,
                OrderStatus = o.OrderStatus,
                RecipientName = o.RecipientName,
                RecipientAddress = o.RecipientAddress,
                RecipientDistrict = o.RecipientDistrict,
                RecipientPhone = o.RecipientPhone,
                PackageDescription = o.PackageDescription,
                Quantity = o.Quantity,
                DeliveryAttempts = o.DeliveryAttempts,
                RegistrationDate = o.RegistrationDate,
                DeliveryDate = o.DeliveryDate,
                CompanyName = o.Company!.User.FullName,
                CargaId = o.CargaID,
            })
            .ToListAsync();

        return Ok(new MyDeliveriesResponseDTO
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
        });
    }

    // GET: api/Drivers/MyCargas
    [HttpGet("MyCargas")]
    public async Task<ActionResult<MyCargasResponseDTO>> GetMyCargasAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 8, [FromQuery] CargaStatus? status = null, [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
    {
        string userID = CurrentUserId;

        var query = _dataContext.Cargas
            .Where(c => c.DriverID == userID)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (desde.HasValue)
            query = query.Where(c => c.FechaCreacion.Date >= desde.Value.Date);

        if (hasta.HasValue)
            query = query.Where(c => c.FechaCreacion.Date <= hasta.Value.Date);

        int totalItems = await query.CountAsync();

        var cargas = await query
            .OrderByDescending(c => c.FechaCreacion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Supervisor)
            .Include(c => c.ConcluidaPor)
            .Include(c => c.FacturadaPor)
            .Include(c => c.Orders!)
                .ThenInclude(o => o.Company).ThenInclude(co => co.User)
            .Include(c => c.Orders!)
                .ThenInclude(o => o.Events!).ThenInclude(e => e.User)
            .ToListAsync();

        var items = cargas.Select(c => new MyCargaDTO
        {
            Id = c.Id,
            Status = c.Status,
            FechaCreacion = c.FechaCreacion,
            FechaConcluida = c.FechaConcluida,
            FechaFacturada = c.FechaFacturada,
            NotaConclusion = c.NotaConclusion,
            NotaFacturacion = c.NotaFacturacion,
            SupervisorName = c.Supervisor?.FullName ?? "—",
            ConcluidaPorName = c.ConcluidaPor?.FullName,
            FacturadaPorName = c.FacturadaPor?.FullName,

            // Stats
            TotalPedidos = c.Orders?.Count ?? 0,
            Entregados = c.Orders?.Count(o => o.OrderStatus == OrderStatus.Delivered) ?? 0,
            Fallidos = c.Orders?.Count(o => o.OrderStatus == OrderStatus.Failed) ?? 0,
            Ausentes = c.Orders?.Count(o => o.OrderStatus == OrderStatus.RecipientAbsent) ?? 0,
            EnRetorno = c.Orders?.Count(o => o.OrderStatus == OrderStatus.Returning) ?? 0,
            EnAlmacen = c.Orders?.Count(o => o.OrderStatus == OrderStatus.OnStorage) ?? 0,

            // Pedidos completos
            Pedidos = (c.Orders ?? [])
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
                    CompanyName = o.Company?.User?.FullName ?? "—",
                    Events = (o.Events ?? [])
                        .OrderBy(e => e.Timestamp)
                        .Select(e => new MyCargaEventDTO
                        {
                            Timestamp = e.Timestamp,
                            NewStatus = e.NewStatus,
                            Note = e.Note,
                            BaglokLocation = e.BaglokLocation,
                            UserName = e.User?.FullName ?? "—",
                        }).ToList()
                }).ToList()
        }).ToList();

        return Ok(new MyCargasResponseDTO
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
        });
    }

    // GET: api/Drivers/Scan/{trackingCode}
    [HttpGet("Scan/{trackingCode}")]
    public async Task<ActionResult<ScanResultDTO>> ScanOrderAsync(string trackingCode)
    {
        string userID = CurrentUserId;

        var order = await _dataContext.Orders
            .Include(o => o.Company).ThenInclude(c => c!.User)
            .FirstOrDefaultAsync(o =>
                o.TrackingCode == trackingCode &&
                o.DriverID == userID);

        if (order is null) return NotFound("Pedido no encontrado o no asignado a ti.");

        return Ok(new ScanResultDTO
        {
            Id = order.Id,
            TrackingCode = order.TrackingCode,
            OrderStatus = order.OrderStatus,
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            RecipientAddress = order.RecipientAddress,
            RecipientDistrict = order.RecipientDistrict,
            PackageDescription = order.PackageDescription,
            Quantity = order.Quantity,
            DeliveryAttempts = order.DeliveryAttempts,
            CompanyName = order.Company?.User?.FullName ?? "—",
            CargaId = order.CargaID,
        });
    }

    // POST: api/Drivers/Scan/Confirm/{OrderID:int}
    [HttpPost("Scan/Confirm/{OrderID:int}")]
    public async Task<ActionResult> ConfirmScan(int OrderID, [FromBody] ScanConfirmDTO DTO)
    {
        string userID = CurrentUserId;

        var order = await _dataContext.Orders.Include(o => o.Events).FirstOrDefaultAsync(o => o.Id == OrderID && o.DriverID == userID);

        if (order is null)
            return NotFound("Pedido no encontrado.");

        // Validar transición permitida
        var permitidos = new[]
        {
            OrderStatus.Delivered,
            OrderStatus.RecipientAbsent,
            OrderStatus.Failed,
        };

        if (!permitidos.Contains(DTO.NewStatus))
            return BadRequest("Estado no permitido desde este flujo.");

        // Actualizar pedido
        order.OrderStatus = DTO.NewStatus;

        if (DTO.NewStatus == OrderStatus.Delivered) order.DeliveryDate = DateTime.UtcNow;
        else order.DeliveryAttempts++;

        // Registrar evento con foto en nota o campo dedicado
        // (si en el futuro quieres guardar la foto en disco, aquí la procesarías)
        var evento = new OrderEvent
        {
            OrderID = order.Id,
            UserID = userID,
            NewStatus = DTO.NewStatus,
            Timestamp = DateTime.UtcNow,
            Note = DTO.Note,
            // Guardamos referencia a que hay foto — el base64 queda en cliente
            // Si decides persistir, aquí harías el upload y guardarías la URL
        };

        _dataContext.OrderEvents.Add(evento);
        await _dataContext.SaveChangesAsync();

        // Notificar dashboard
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Drivers").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Operators").SendAsync("DashboardUpdate");
        await _hubContext.Clients.Group("Supervisors").SendAsync("DashboardUpdate");
        return Ok();
    }









    // ── Helpers de mapeo ─────────────────────────────────────────────────────
    private static CargaDetailDTO MapToCargaDetail(Carga c) => new()
    {
        Id = c.Id,
        Status = c.Status,
        FechaCreacion = c.FechaCreacion,
        FechaConcluida = c.FechaConcluida,
        FechaFacturada = c.FechaFacturada,
        NotaConclusion = c.NotaConclusion,
        NotaFacturacion = c.NotaFacturacion,
        ConcluidaPorName = c.ConcluidaPor?.FullName,
        FacturadaPorName = c.FacturadaPor?.FullName,
        DriverID = c.DriverID,
        DriverName = c.Driver?.User?.FullName ?? string.Empty,
        DriverPlaca = c.Driver?.Placa ?? string.Empty,
        DriverPhoto = c.Driver?.User?.Photo,
        SupervisorName = c.Supervisor?.FullName ?? string.Empty,
        TotalPedidos = c.Orders?.Count ?? 0,
        Entregados = c.Orders?.Count(o => o.OrderStatus == OrderStatus.Delivered) ?? 0,
        EnOnStorage = c.Orders?.Count(o => o.OrderStatus == OrderStatus.OnStorage) ?? 0,
        Pendientes = c.Orders?.Count(o => o.OrderStatus is not OrderStatus.Delivered and not OrderStatus.OnStorage) ?? 0,
        Distrito = c.Orders?.FirstOrDefault()?.RecipientDistrict ?? string.Empty,
        Pedidos = [.. (c.Orders ?? []).OrderBy(o => o.OrderStatus).Select(MapToDriverOrderDTO)]
    };

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
