using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TommyLogistic.API.Services;
using TommyLogistic.Shared.DTOs.Orders;

namespace TommyLogistic.API.Controllers;

[Authorize]
[ApiController]
[Route("api/orders/{orderId:int}/events")]
public class OrderEventsController(OrderEventService _eventService) : ControllerBase
{
    // GET api/orders/#/events
    [HttpGet]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(OrderTimelineDTO), 200)]
    public async Task<IActionResult> GetTimeline(int orderId)
    {
        var timeline = await _eventService.GetTimelineAsync(orderId);
        return timeline is null ? NotFound() : Ok(timeline);
    }

    // POST api/orders/#/events
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [Authorize(Roles = "Admin,Supervisor,Operator,Driver")]
    public async Task<IActionResult> AddEvent(int orderId, [FromBody] CreateOrderEventDTO dto)
    {
        if (dto.OrderId != orderId)
            return BadRequest("El orderId del body no coincide con la ruta.");

        try
        {
            var ev = await _eventService.RegisterEventAsync(dto);
            return CreatedAtAction(nameof(GetTimeline), new { orderId }, new { ev.Id });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // POST api/orders/#/events/pickup
    [HttpPost("pickup")]
    [Authorize(Roles = "Admin,Driver")]
    public async Task<IActionResult> RegisterPickup(int orderId, [FromBody] ActorNoteRequest req)
    {
        var ev = await _eventService.RegisterPickupAsync(orderId, req.ActorUserId);
        return Ok(new { ev.Id, message = "Retiro registrado." });
    }

    // POST api/orders/#/events/delivered
    [HttpPost("delivered")]
    [Authorize(Roles = "Admin,Driver")]
    public async Task<IActionResult> RegisterDelivered(int orderId, [FromBody] ActorNoteRequest req)
    {
        var ev = await _eventService.RegisterDeliveredAsync(orderId, req.ActorUserId, req.Notes);
        return Ok(new { ev.Id, message = "Entrega confirmada." });
    }

    // POST api/orders/#/events/absent
    [HttpPost("absent")]
    [Authorize(Roles = "Admin,Driver")]
    public async Task<IActionResult> RegisterAbsent(int orderId, [FromBody] ActorNoteRequest req)
    {
        var ev = await _eventService.RegisterAbsentAsync(orderId, req.ActorUserId, req.Notes);
        return Ok(new { ev.Id, message = "Ausencia registrada." });
    }

    // POST api/orders/#/events/baglok
    [HttpPost("baglok")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> RegisterBaglok(int orderId, [FromBody] BaglokRequest req)
    {
        var ev = await _eventService.RegisterStoredInBaglokAsync(
            orderId, req.ActorUserId, req.BaglokLocation, req.Notes);
        return Ok(new { ev.Id, message = $"Guardado en Baglok: {req.BaglokLocation}" });
    }

    // POST api/orders/#/events/reschedule
    [HttpPost("reschedule")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> RegisterReschedule(int orderId, [FromBody] RescheduleRequest req)
    {
        var ev = await _eventService.RegisterRescheduledAsync(
            orderId, req.ActorUserId, req.RescheduledFor, req.Notes);
        return Ok(new { ev.Id, message = $"Reprogramado para {req.RescheduledFor:dd/MM/yyyy}" });
    }

    // POST api/orders/#/events/release
    [HttpPost("release")]
    [Authorize(Roles = "Admin,Operator,Supervisor")]
    public async Task<IActionResult> RegisterRelease(int orderId, [FromBody] ActorNoteRequest req)
    {
        var ev = await _eventService.RegisterReleasedFromBaglokAsync(orderId, req.ActorUserId);
        return Ok(new { ev.Id, message = "Liberado del Baglok." });
    }
}

// ── Request bodies ──────────────────────────────────────────────
public record ActorNoteRequest(string ActorUserId, string? Notes = null);
public record BaglokRequest(string ActorUserId, string BaglokLocation, string? Notes = null);
public record RescheduleRequest(string ActorUserId, DateTime RescheduledFor, string? Notes = null);