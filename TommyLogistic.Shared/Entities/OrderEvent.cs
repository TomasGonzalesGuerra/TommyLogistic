using TommyLogistic.Shared.Enums;

namespace TommyLogistic.Shared.Entities;

public class OrderEvent
{
    public int Id { get; private set; }
    public int OrderID { get; private set; }
    public Order? Order { get; private set; }
    public OrderEventType EventType { get; private set; }
    public OrderStatus StatusAfter { get; private set; }
    public string? Notes { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string BaglokLocation { get; private set; } = null!;
    public DateTime? RescheduledFor { get; private set; }
    public int DeliveryAttempt { get; private set; }
    public string UserID { get; private set; } = null!;
    public User? User { get; private set; }


    public static OrderEvent Create(
       int orderId,
       OrderEventType eventType,
       OrderStatus statusAfter,
       string userID,
       string baglokLocation,
       string? notes = null,
       DateTime? rescheduledFor = null,
       int deliveryAttempt = 0)
    {
        return new OrderEvent
        {
            OrderID = orderId,
            EventType = eventType,
            StatusAfter = statusAfter,
            OccurredAt = DateTime.UtcNow,
            UserID = userID,
            Notes = notes,
            BaglokLocation = baglokLocation,
            RescheduledFor = rescheduledFor,
            DeliveryAttempt = deliveryAttempt,
        };
    }
}