using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TommyLogistic.API.Services;
using TommyLogistic.Shared.DTOs.Orders;

namespace TommyLogistic.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]/{orderId:int}/Events")]
public class OrderEventsController() : ControllerBase
{

}
