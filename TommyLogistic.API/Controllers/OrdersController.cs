using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TommyLogistic.API.Data;
using TommyLogistic.Shared.DTOs.Orders;
using TommyLogistic.Shared.Entities;

namespace TommyLogistic.API.Controllers
{
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

        // PUT: api/Orders/#
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }

            _dataContext.Entry(order).State = EntityState.Modified;

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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

        private bool OrderExists(int id)
        {
            return _dataContext.Orders.Any(e => e.Id == id);
        }
    }
}
