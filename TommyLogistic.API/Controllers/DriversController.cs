using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TommyLogistic.Api.Helpers;
using TommyLogistic.API.Data;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DriversController(LogisticDataContext dadaContext, IUserHelper userHelper) : ControllerBase
{
    private readonly IUserHelper _userHelper = userHelper;
    private readonly LogisticDataContext _dadaContext = dadaContext;

    // GET: api/Drivers/GetAllDrivers
    [HttpGet("GetAllDrivers")]
    public async Task<ActionResult<IEnumerable<DriverDTO>>> GetAllDriversAsync()
    {
        List<User> users = await _dadaContext.Users
            .Include(u => u.Driver).ThenInclude(d => d.Orders)
            .Where(u => u.UserType == UserEnum.Driver)
            .ToListAsync();

        if (users.Count == 0) return Ok(new List<DriverDTO>());
        if (users == null) return NotFound();

        List<DriverDTO> driverDTOs = users.Select(u => new DriverDTO
        {
            Id = u.Id,
            FullName = u.FullName,
            Celular = u.PhoneNumber!,
            DNI = u.Document,
            Photo = u.Photo ?? string.Empty,
            Available = u.Driver!.Available,
            Placa = u.Driver.Placa,
            DeliveredToday = (int)(u.Driver!.Orders?.Count(o => o.OrderStatus == OrderStatus.Delivered))!,
            ActiveOrderToday = (int)(u.Driver!.Orders?.Count(o => o.OrderStatus == OrderStatus.Assigned))!
        }).ToList();

        return Ok(driverDTOs);
    }

    // POST: api/Drivers
    [HttpPost]
    public async Task<ActionResult> CreateDriverAsync(DriverCreatedDTO createdDTO)
    {
        var userExists = await _userHelper.GetUserAsync(createdDTO.Email);
        if (userExists != null) return BadRequest("El correo ya está registrado.");

        User newUser = new()
        {
            UserName = createdDTO.Email,
            Email = createdDTO.Email,
            PhoneNumber = createdDTO.Celular,
            FullName = createdDTO.FullName,
            Document = createdDTO.DNI,
            Address = string.Empty,
            Photo = string.Empty,
            UserType = UserEnum.Driver,
            Companies = [],
        };

        var result = await _userHelper.AddUserAsync(newUser, "123456");

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.FirstOrDefault()?.Description);
        }

        await _userHelper.AddUserToRoleAsync(newUser, UserEnum.Driver.ToString());

        Driver newDriver = new()
        {
            UserID = newUser.Id,
            Placa = createdDTO.Placa,
            Available = true,
            Orders = [],
        };

        _dadaContext.Drivers.Add(newDriver);
        await _dadaContext.SaveChangesAsync();
        return Ok();
    }

}
