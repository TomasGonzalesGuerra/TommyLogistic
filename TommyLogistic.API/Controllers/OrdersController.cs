using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TommyLogistic.API.Data;
using TommyLogistic.Shared.DTOs.Orders;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController(LogisticDataContext dataContext) : ControllerBase
{
    private readonly LogisticDataContext _dataContext = dataContext;

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

        return CreatedAtAction("GetOrder", new { id = order.Id }, order);
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
        if (externalOrders == null || !externalOrders.Any()) return BadRequest("No hay datos.");

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
                RegistrationDate = DateTime.Now,
                OrderStatus = OrderStatus.Registered,
                TrackingCode = $"TL-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                DeliveryType = DeliveryType.ToDay
            });
        }

        _dataContext.Orders.AddRange(newOrders);
        await _dataContext.SaveChangesAsync();
        return Ok($"{newOrders.Count} pedidos registrados correctamente.");
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