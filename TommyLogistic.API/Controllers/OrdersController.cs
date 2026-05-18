using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TommyLogistic.API.Data;
using TommyLogistic.API.Hubs;
using TommyLogistic.API.Services;
using TommyLogistic.Shared.DTOs.Orders;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class OrdersController(LogisticDataContext dataContext, IHubContext<NotificationHub> hubContext, OrderEventService eventService) : ControllerBase
{
    private readonly LogisticDataContext _dataContext = dataContext;
    private readonly OrderEventService _eventService = eventService;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Yop";

    // GET: api/Orders/GetAllOrders
    [HttpGet("GetAllOrders")]
    public async Task<ActionResult<IEnumerable<OrderSummaryDTO>>> GetAllOrdersasync()
    {
        List<Order> orders = await _dataContext.Orders
            .Include(o => o.Driver).ThenInclude(d => d.User)
            .ToListAsync();
        if (orders.Count == 0) return Ok(new List<OrderSummaryDTO>());
        if (orders == null) return NotFound();

        List<OrderSummaryDTO> orderSummaries = orders.Select(o => new OrderSummaryDTO
        {
            Id = o.Id,
            TrackingCode = o.TrackingCode,
            RecipientName = o.RecipientName,
            RecipientDistrict = o.RecipientDistrict,
            PackageDescription = o.PackageDescription,
            DeliveryType = o.DeliveryType,
            OrderStatus = o.OrderStatus,
            EstimatedDeliveryDate = DateTime.Now.AddHours(4),
            DeliveryPersonName = o.Driver?.User?.FullName,
            DeliveryAttempts = o.DeliveryAttempts
        }).ToList();

        return Ok(orderSummaries);
    }

    // GET: api/Orders/#
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _dataContext.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        return order;
    }

    // POST: api/Orders
    [HttpPost]
    public async Task<ActionResult<Order>> PostOrder(Order order)
    {
        _dataContext.Orders.Add(order);
        await _dataContext.SaveChangesAsync();

        // 📋 Registrar evento: Pedido Creado
        await _eventService.RegisterAsync(
            orderId: order.Id,
            newStatus: OrderStatus.Registered,
            userId: CurrentUserId,
            assignedDriverId: order.DriverID,
            note: "Pedido registrado"
        );

        // 🔔 Notificar al driver si fue asignado
        if (!string.IsNullOrEmpty(order.DriverID))
        {
            // 📋 Registrar evento: Asignación
            await _eventService.RegisterAsync(
                orderId: order.Id,
                newStatus: OrderStatus.Assigned,
                userId: CurrentUserId,
                assignedDriverId: order.DriverID,
                note: "Pedido asignado al repartidor"
            );

            await _hubContext.Clients
                .Group($"Driver_{order.DriverID}")
                .SendAsync("NewOrderAssigned", new
                {
                    Message = $"¡Tienes un Nuevo Pedido! Destinatario: {order.RecipientName}",
                    OrderId = order.Id,
                    Tracking = order.TrackingCode,
                    Address = order.RecipientAddress,
                });
        }

        return Ok(order.Id);
    }

    // DELETE: api/Orders/#
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _dataContext.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        _dataContext.Orders.Remove(order);
        await _dataContext.SaveChangesAsync();

        return NoContent();
    }


    // POST: api/Orders/ReceiveExternalOrders
    [HttpPost("ReceiveExternalOrders")]
    public async Task<IActionResult> ReceiveExternalOrders([FromBody] List<ExternalOrderDTO> externalOrders)
    {
        if (externalOrders == null || externalOrders is []) return BadRequest("No hay datos.");

        List<Company> companies = await _dataContext.Companies.Include(c => c.User).ToListAsync();
        List<Order> newOrders = [];

        foreach (var item in externalOrders)
        {
            Company? company = companies.FirstOrDefault(c => c.User.Email == item.CompanyEmail);
            if (company == null) continue;

            newOrders.Add(new Order
            {
                CompanyID = company.Id,
                Quantity = item.Quantity,
                PackageDescription = item.ProductName,
                RecipientName = item.Recipient,
                RecipientAddress = item.Address,
                RecipientDistrict = item.District,
                RecipientPhone = item.Phone,
                RegistrationDate = DateTime.UtcNow,
                OrderStatus = OrderStatus.Registered,
                TrackingCode = $"TL-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                DeliveryType = DeliveryType.ToDay
            });
        }

        _dataContext.Orders.AddRange(newOrders);
        await _dataContext.SaveChangesAsync();
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        return Ok();
    }

    // POST: api/Orders/AutoRouteAndAssign
    [HttpPost("AutoRouteAndAssign")]
    public async Task<IActionResult> AutoRouteAndAssignAsync()
    {
        DateTime todayUtc = DateTime.UtcNow.Date;
        DateTime tomorrowUtc = todayUtc.AddDays(1);

        List<Order> registeredOrders = await _dataContext.Orders
            .Where(o => o.OrderStatus == OrderStatus.Registered)
            .Where(o => o.RegistrationDate >= todayUtc && o.RegistrationDate < tomorrowUtc)
            .ToListAsync();

        List<Driver> availableDrivers = await _dataContext.Drivers.Where(d => d.Available).ToListAsync();

        if (registeredOrders is [] || availableDrivers is []) return BadRequest($"Pedidos registrados: {registeredOrders.Count} | Drivers disponibles: {availableDrivers.Count}");

        var ordersByDistrict = registeredOrders.GroupBy(o => o.RecipientDistrict);
        int driverIndex = 0;
        int totalAssigned = 0;

        foreach (var carga in ordersByDistrict)
        {
            var chunks = carga.Chunk(10);

            foreach (var item in chunks)
            {
                if (driverIndex >= availableDrivers.Count) break;
                var currentDriver = availableDrivers[driverIndex];

                foreach (var order in item)
                {
                    order.DriverID = currentDriver.UserID;
                    order.OrderStatus = OrderStatus.Assigned;

                    // 📋 Registrar evento: asignación
                    await _eventService.RegisterAsync(
                        orderId: order.Id,
                        newStatus: OrderStatus.Assigned,
                        userId: CurrentUserId,
                        assignedDriverId: currentDriver.UserID,
                        note: "Pedido asignado al repartidor"
                    );

                    // 🔔 Notificar SOLO a ese driver
                    await _hubContext.Clients
                            .Group($"Driver_{currentDriver.UserID}")
                            .SendAsync("NewOrderAssigned", new
                            {
                                Message = $"¡Tienes un nuevo pedido asignado! Destinatario: {order.RecipientName}",
                                OrderId = order.Id,
                                Tracking = order.TrackingCode,
                                Address = order.RecipientAddress,
                                Timestamp = DateTime.Now
                            });

                    totalAssigned++;
                }

                currentDriver.Available = false;
                driverIndex++;
            }

            if (driverIndex >= availableDrivers.Count) break;
        }

        await _dataContext.SaveChangesAsync();
        await _hubContext.Clients.Group("Admins").SendAsync("DashboardUpdate");
        int ordersLeft = registeredOrders.Count - totalAssigned;

        return Ok(new
        {
            Message = $"Asignación completada: {totalAssigned} órdenes asignadas.",
            DriversUsed = driverIndex,
            OrdersPending = ordersLeft
        });
    }

    // GET: api/Orders/Preview/OrderID:int
    [HttpGet("Preview/{OrderID:int}")]
    public async Task<ActionResult> GetPreview(int OrderID)
    {
        var order = await _dataContext.Orders
            .Include(o => o.Company).ThenInclude(c => c!.User)
            .Include(o => o.Driver).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(o => o.Id == OrderID);

        if (order is null) return NotFound();

        return Ok(new OrderPreviewDTO
        {
            Id = order.Id,
            TrackingCode = order.TrackingCode,
            OrderStatus = order.OrderStatus,
            DeliveryType = order.DeliveryType,
            DeliveryAttempts = order.DeliveryAttempts,
            RegistrationDate = order.RegistrationDate,
            EstimatedDeliveryDate = order.DeliveryDate,
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            RecipientAddress = order.RecipientAddress,
            RecipientDistrict = order.RecipientDistrict,
            PackageDescription = order.PackageDescription,
            Quantity = order.Quantity,
            CompanyName = order.Company?.User.FullName,
            DriverName = order.Driver?.User.FullName,
            DriverPhone = order.Driver?.User.PhoneNumber,
        });
    }

    // ── Cambiar estado del pedido ─────────────────────────────────────────
    // PUT: api/Orders/UpdateStatus/OrderID:int
    [HttpPut("UpdateStatus/{OrderID:int}")]
    public async Task<ActionResult> UpdateStatus(int OrderID, [FromBody] OrderUpdateStatusDTO DTO)
    {
        Order? order = await _dataContext.Orders.FindAsync(OrderID);
        if (order is null) return NotFound("Pedido NO Encontrado");

        var previousDriverId = order.DriverID;
        order.OrderStatus = DTO.NewStatus;

        // Si se está reasignando a un nuevo driver
        if (!string.IsNullOrEmpty(DTO.NewDriverID) && DTO.NewDriverID != previousDriverId) order.DriverID = DTO.NewDriverID;

        await _dataContext.SaveChangesAsync();

        // 📋 Registrar el evento con toda la info
        await _eventService.RegisterAsync(
            orderId: OrderID,
            newStatus: DTO.NewStatus,
            userId: CurrentUserId,
            assignedDriverId: DTO.NewDriverID ?? order.DriverID,
            note: DTO.Note
        );

        // 🔔 Notificar al driver si fue Reasignado
        if (!string.IsNullOrEmpty(DTO.NewDriverID) && DTO.NewDriverID != previousDriverId)
        {
            await _hubContext.Clients
                .Group($"Driver_{DTO.NewDriverID}")
                .SendAsync("NewOrderAssigned", new
                {
                    Message = $"¡Te Reasignaron un Pedido! Destinatario: {order.RecipientName}",
                    OrderId = order.Id,
                    Tracking = order.TrackingCode,
                    Address = order.RecipientAddress,
                });
        }

        return Ok();
    }

    // ── Historial completo de un pedido ───────────────────────────────────
    // GET: api/Orders/History/OrderID:int
    [HttpGet("History/{OrderID:int}")]
    public async Task<IActionResult> GetHistory(int OrderID)
    {
        bool orderExists = await _dataContext.Orders.AnyAsync(o => o.Id == OrderID);
        if (!orderExists) return NotFound($"La Orden con ID {OrderID} NO Existe.");

        var events = await _dataContext.OrderEvents
            .AsNoTracking()
            .Where(o => o.OrderID == OrderID)
            .OrderBy(o => o.Timestamp)
            .Select(o => new
            {
                o.Id,
                o.Timestamp,
                Status = o.NewStatus.ToString(),
                o.Note,
                ExecutedBy = o.User!.FullName,
                ExecutedRole = o.User.UserType.ToString(),
                AssignedDriver = o.AssignedDriver != null ? o.AssignedDriver.User.FullName : null
            }).ToListAsync();

        return Ok(events);
    }

}


//[HttpGet]
//public async Task<ActionResult<IEnumerable<Order>>> GetOrders(
//    [FromQuery] string? name,
//    [FromQuery] DateTime? date)
//{
//    // Creamos la consulta base incluyendo las relaciones para ver los nombres
//    var query = _dataContext.Orders
//        .Include(o => o.Company)
//        .Include(o => o.Driver)
//        .ThenInclude(d => d.User)
//        .AsQueryable();

//    // Filtro por Nombre del Destinatario (o puedes cambiarlo por nombre de empresa)
//    if (!string.IsNullOrWhiteSpace(name))
//    {
//        query = query.Where(o => o.RecipientName!.Contains(name) ||
//                                 o.PackageDescription!.Contains(name));
//    }

//    // Filtro por Fecha de Registro
//    if (date.HasValue)
//    {
//        // Comparamos solo la parte de la fecha (sin horas)
//        query = query.Where(o => o.RegistrationDate.Date == date.Value.Date);
//    }

//    return await query.OrderByDescending(o => o.RegistrationDate).ToListAsync();
//}

//Por nombre: api/orders?name=Kakash
//Por fecha: api/orders?date=2023-10-27
//Combinado: api/orders?name=Ramen&date=2023-10-27
