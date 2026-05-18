using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TommyLogistic.Api.Helpers;
using TommyLogistic.API.Data;
using TommyLogistic.API.Hubs;
using TommyLogistic.API.Services;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(UserEnum.Admin))]
public class AdminsController(LogisticDataContext dadaContext, IUserHelper userHelper, IHubContext<NotificationHub> hubContext, OrderEventService eventService) : ControllerBase
{
    private readonly IUserHelper _userHelper = userHelper;
    private readonly LogisticDataContext _dadaContext = dadaContext;
    private readonly OrderEventService _eventService = eventService;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Yop";

    // GET: api/Admins/GetAllDrivers
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

    // POST: api/Admins/CreateDriver
    [HttpPost("CreateDriver")]
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

    // PUT: api/Admins/UpdateProfile
    [HttpPut("UpdateProfile")]
    public async Task<ActionResult> UpdateProfileAsync([FromBody] DriverProfileUpdateDTO model)
    {
        Driver? driver = await _dadaContext.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.UserID == CurrentUserId);
        if (driver is null) return NotFound();

        driver.User.PhoneNumber = model.Phone;
        driver.User.Address = model.Address!;
        driver.Placa = model.Placa;

        if (!string.IsNullOrEmpty(model.PhotoBase64))
        {
            //TODO: Implementar lógica para guardar la foto y obtener su URL,
            //luego asignarla a driver.User.Photo
            // Aquí IFileStorage igual que en AccountsController
            // driver.User.Photo = await _fileStorage.SaveFileAsync(...);
        }

        await _dadaContext.SaveChangesAsync();
        return Ok();
    }

}
