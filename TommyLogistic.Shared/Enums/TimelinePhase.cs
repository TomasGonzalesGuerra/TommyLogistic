namespace TommyLogistic.Shared.Enums;

public enum TimelinePhase
{
    Entry, // Recepción
    Dispatch, // Asignación y salida
    Delivery, // Intento de entrega
    Return, // Retorno al almacén / Baglok
    Reschedule, // Reprogramación
    Closed, // Estado final
}