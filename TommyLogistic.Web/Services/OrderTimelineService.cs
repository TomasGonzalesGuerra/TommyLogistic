using System.Net.Http.Json;
using TommyLogistic.Shared.DTOs.Orders;

namespace TommyLogistic.Web.Services;

public class OrderTimelineService(HttpClient _http)
{
    public async Task<OrderTimelineDTO?> GetTimelineAsync(int orderId)
    {
        try
        {
            return await _http.GetFromJsonAsync<OrderTimelineDTO>($"api/OrderEvents/{orderId}/Events");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RegisterPickupAsync(int orderId, int driverId)
        => await Post($"api/OrderEvents/{orderId}/Events/Pickup",
                      new { ActorUserId = driverId });

    public async Task<bool> RegisterDeliveredAsync(int orderId, int driverId, string? notes = null)
        => await Post($"api/OrderEvents/{orderId}/Events/Delivered",
                      new { ActorUserId = driverId, Notes = notes });

    public async Task<bool> RegisterAbsentAsync(int orderId, int driverId, string? notes = null)
        => await Post($"api/OrderEvents/{orderId}/Events/Absent",
                      new { ActorUserId = driverId, Notes = notes });

    public async Task<bool> RegisterBaglokAsync(
        int orderId, int operatorId, string location, string? notes = null)
        => await Post($"api/OrderEvents/{orderId}/Events/Baglok",
                      new { ActorUserId = operatorId, BaglokLocation = location, Notes = notes });

    public async Task<bool> RegisterRescheduleAsync(
        int orderId, int supervisorId, DateTime rescheduledFor, string? notes = null)
        => await Post($"api/OrderEvents/{orderId}/Events/Reschedule",
                      new { ActorUserId = supervisorId, RescheduledFor = rescheduledFor, Notes = notes });

    public async Task<bool> RegisterReleaseAsync(int orderId, int operatorId)
        => await Post($"api/OrderEvents/{orderId}/Events/Release",
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