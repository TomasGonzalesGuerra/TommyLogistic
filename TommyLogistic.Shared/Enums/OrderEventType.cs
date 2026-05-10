namespace TommyLogistic.Shared.Enums;

public enum OrderEventType
{
    Received,
    RegisteredInWarehouse,
    AssignedToDriver,
    Reassigned,
    PickedUpByDriver,
    OutForDelivery,
    Delivered,
    RecipientAbsent,
    DeliveryFailed,
    Returning,
    StoredInBaglok,
    Rescheduled,
    ReleasedFromBaglok,
    MarkedAsFailed,
    ReturnedToClient,
    Cancelled,
    Note,
}