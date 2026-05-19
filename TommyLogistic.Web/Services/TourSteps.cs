using TommyLogistic.Shared.Models;

namespace TommyLogistic.Web.Services;

public static class TourSteps
{
    public static List<TourStep> GetStepsForRole(string role) => role switch
    {
        "Admin" => AdminSteps(),
        "Driver" => DriverSteps(),
        "Supervisor" => SupervisorSteps(),
        _ => []
    };

    private static List<TourStep> AdminSteps() =>
    [
        new TourStep
        {
            TargetSelector = "[data-tour='welcome']",
            Title = "¡Bienvenido, Admin! 👋",
            Message = "Soy Tommy, tu asistente logístico. Voy a mostrarte todo lo que puedes hacer aquí. ¡Empecemos!",
            Mood = MascotMood.Waving,
            BubblePos = BubblePosition.Right
        },
        new TourStep
        {
            TargetSelector = "[data-tour='stats']",
            Title = "Panel de estadísticas",
            Message = "Aquí ves en tiempo real los pedidos activos, drivers conectados y cargas en curso. Se actualiza automáticamente.",
            Mood = MascotMood.Pointing,
            BubblePos = BubblePosition.Bottom
        },
        new TourStep
        {
            TargetSelector = "[data-tour='drivers-panel']",
            Title = "Drivers online",
            Message = "Este panel muestra qué drivers están conectados ahora mismo vía SignalR. El punto verde indica presencia en tiempo real.",
            Mood = MascotMood.Happy,
            BubblePos = BubblePosition.Left
        },
        new TourStep
        {
            TargetSelector = "[data-tour='nav-cargas']",
            Title = "Gestión de cargas",
            Message = "Desde aquí creas y gestionas las cargas. Cada carga agrupa pedidos y se asigna a un driver.",
            Mood = MascotMood.Pointing,
            BubblePos = BubblePosition.Right
        },
        new TourStep
        {
            TargetSelector = "[data-tour='nav-companies']",
            Title = "Empresas cliente",
            Message = "Gestiona las empresas y sus usuarios. Puedes agrupar varios clientes bajo una misma organización.",
            Mood = MascotMood.Happy,
            BubblePos = BubblePosition.Right
        },
        new TourStep
        {
            TargetSelector = "[data-tour='nav-reports']",
            Title = "Reportes",
            Message = "Exporta datos en Excel o PDF. Pedidos, cargas y performance de drivers con un solo clic.",
            Mood = MascotMood.Pointing,
            BubblePos = BubblePosition.Right
        },
        new TourStep
        {
            TargetSelector = "[data-tour='notifications']",
            Title = "Notificaciones en tiempo real",
            Message = "La campanita te avisa de todo lo que pasa en el sistema: pedidos asignados, cargas concluidas, drivers online...",
            Mood = MascotMood.Happy,
            BubblePos = BubblePosition.Bottom
        },
        new TourStep
        {
            TargetSelector = "[data-tour='welcome']",
            Title = "¡Ya estás listo! 🚀",
            Message = "Eso es todo. Si quieres volver a ver este tour, encuéntralo en tu perfil. ¡Mucho éxito gestionando TommyLogistic!",
            Mood = MascotMood.Waving,
            BubblePos = BubblePosition.Right
        }
    ];

    private static List<TourStep> DriverSteps() =>
    [
        new TourStep
        {
            TargetSelector = "[data-tour='welcome']",
            Title = "¡Hola, Driver! 👋",
            Message = "Soy Tommy. Te muestro cómo usar tu panel para gestionar tus pedidos del día.",
            Mood = MascotMood.Waving,
            BubblePos = BubblePosition.Right
        },
        new TourStep
        {
            TargetSelector = "[data-tour='stats']",
            Title = "Tus estadísticas",
            Message = "Aquí ves tus pedidos de hoy: pendientes, en camino y entregados.",
            Mood = MascotMood.Pointing,
            BubblePos = BubblePosition.Bottom
        }
    ];

    private static List<TourStep> SupervisorSteps() =>
    [
        new TourStep
        {
            TargetSelector = "[data-tour='welcome']",
            Title = "¡Hola, Supervisor! 👋",
            Message = "Desde aquí gestionas las cargas y supervisas el estado de los envíos.",
            Mood = MascotMood.Waving,
            BubblePos = BubblePosition.Right
        }
    ];
}