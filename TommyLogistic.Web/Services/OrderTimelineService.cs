using System.Net.Http.Json;
using TommyLogistic.Shared.DTOs.Orders;

namespace TommyLogistic.Web.Services;

public class OrderTimelineService(HttpClient _http)
{
    public async Task<OrderTimelineDTO?> GetTimelineAsync(int orderId)
    {
        try
        {
            return await _http.GetFromJsonAsync<OrderTimelineDTO>($"api/orders/{orderId}/events");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RegisterPickupAsync(int orderId, int driverId)
        => await Post($"api/orders/{orderId}/events/pickup",
                      new { ActorUserId = driverId });

    public async Task<bool> RegisterDeliveredAsync(int orderId, int driverId, string? notes = null)
        => await Post($"api/orders/{orderId}/events/delivered",
                      new { ActorUserId = driverId, Notes = notes });

    public async Task<bool> RegisterAbsentAsync(int orderId, int driverId, string? notes = null)
        => await Post($"api/orders/{orderId}/events/absent",
                      new { ActorUserId = driverId, Notes = notes });

    public async Task<bool> RegisterBaglokAsync(
        int orderId, int operatorId, string location, string? notes = null)
        => await Post($"api/orders/{orderId}/events/baglok",
                      new { ActorUserId = operatorId, BaglokLocation = location, Notes = notes });

    public async Task<bool> RegisterRescheduleAsync(
        int orderId, int supervisorId, DateTime rescheduledFor, string? notes = null)
        => await Post($"api/orders/{orderId}/events/reschedule",
                      new { ActorUserId = supervisorId, RescheduledFor = rescheduledFor, Notes = notes });

    public async Task<bool> RegisterReleaseAsync(int orderId, int operatorId)
        => await Post($"api/orders/{orderId}/events/release",
                      new { ActorUserId = operatorId });

    private async Task<bool> Post(string url, object body)
    {
        try
        {
            var res = await _http.PostAsJsonAsync(url, body);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}