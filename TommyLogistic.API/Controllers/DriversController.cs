using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TommyLogistic.API.Data;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DriversController(LogisticDataContext dadaContext) : ControllerBase
{
    private readonly LogisticDataContext _dadaContext = dadaContext;

    // GET: api/Drivers/GetAllDrivers
    [HttpGet("GetAllDrivers")]
    public async Task<ActionResult<IEnumerable<DriverDTO>>> GetAllDriversAsync()
    {
        List<User> users = await _dadaContext.Users.Where(u => u.UserType == UserEnum.Driver).ToListAsync();
        if (users.Count == 0) return Ok(new List<DriverDTO>());
        if (users == null) return NotFound();

        List<DriverDTO> driverDTOs = users.Select(u => new DriverDTO
        {
            Id = int.TryParse(u.Id, out var parsedId) ? parsedId : 0,
            FullName = u.FullName ?? string.Empty,
            Celular = u.PhoneNumber ?? string.Empty,
            DNI = u.Document ?? string.Empty,
            Photo = u.Photo ?? string.Empty,
            Available = u.Drivers?.FirstOrDefault()?.Available ?? false,
            Placa = u.Drivers?.FirstOrDefault()?.Placa ?? string.Empty,
            DeliveredToday = u.Drivers?.FirstOrDefault()?.Orders?.Count(o => o.OrderStatus == OrderStatus.Delivered && o.DeliveryDate == DateTime.Today) ?? 0,
            ActiveOrderToday = u.Drivers?.FirstOrDefault()?.Orders?.Count(o => o.OrderStatus == OrderStatus.Assigned && o.DeliveryDate == DateTime.Today) ?? 0
        }).ToList();

        return Ok(driverDTOs);
    }
}
