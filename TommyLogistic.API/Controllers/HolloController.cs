using Microsoft.AspNetCore.Mvc;

namespace TommyLogistic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HolloController : ControllerBase
{
    // GET: api/Hollo/hi
    [HttpGet("hi")]
    public async Task<ActionResult<string>> GetAdminDashboardAsync()
    {
        Console.WriteLine("Hola desde el Api  en el Server");
        return Ok($"Hola soy Yop");
    }
}
