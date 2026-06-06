using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TommyLogistic.API.Data;
using TommyLogistic.Shared.Entities;

namespace TommyLogistic.API.Hubs;

public class NotificationHub(LogisticDataContext context) : Hub
{
    private readonly LogisticDataContext _context = context;
    private static readonly Dictionary<string, string> _driversConectados = [];
    private static readonly object _lock = new();


    // Admins ──────────────────────────────────────────────────
    public async Task JoinAdminGroup() => await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
    public async Task LeaveAdminGroup() => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");

    // Supers ──────────────────────────────────────────────────
    public async Task JoinSupervisorGroup() => await Groups.AddToGroupAsync(Context.ConnectionId, "Supervisors");
    public async Task LeaveSupervisorGroup() => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Supervisors");

    // Driver ──────────────────────────────────────────────────
    public async Task JoinDriversGroup() => await Groups.AddToGroupAsync(Context.ConnectionId, "Drivers");
    public async Task LeaveDriversGroup() => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Drivers");
    public async Task JoinPersonalGroup(string userID) => await Groups.AddToGroupAsync(Context.ConnectionId, $"Driver_{userID}");
    public async Task LeavePersonalGroup(string userID) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Driver_{userID}");
    
    // Operators ────────────────────────────────────────────────
    public async Task JoinOperatorGroup() => await Groups.AddToGroupAsync(Context.ConnectionId, "Operators");
    public async Task LeaveOperatorGroup() => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Operators");

    // 👇 Driver anuncia que está online
    public async Task DriverOnline(string userID)
    {
        lock (_lock)
        {
            _driversConectados[userID] = Context.ConnectionId;
        }

        // Obtener info del driver para mandarla a los admins
        Driver? driver = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.UserID == userID);
        if (driver is null) return;

        // Notificar a todos los admins que este driver se conectó
        await Clients.Group("Admins").SendAsync("DriverConectado", new
        {
            UserId = userID,
            FullName = driver.User.FullName,
            Placa = driver.Placa,
            Photo = driver.User.Photo,
            Available = driver.Available,
            Timestamp = DateTime.Now
        });
    }

    // 👇 Se ejecuta automáticamente cuando se pierde la conexión
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userID = null;

        lock (_lock)
        {
            var entry = _driversConectados.FirstOrDefault(x => x.Value == Context.ConnectionId);

            if (entry.Key is not null)
            {
                userID = entry.Key;
                _driversConectados.Remove(userID);
            }
        }

        if (userID is not null)
        {
            await Clients.Group("Admins").SendAsync("DriverDesconectado", new
            {
                UserId = userID,
                Timestamp = DateTime.Now
            });
        }

        await base.OnDisconnectedAsync(exception);
    }

    // 👇 Admin pide la lista actual de drivers conectados
    public async Task GetDriversConectados()
    {
        List<string> userIDs;

        lock (_lock)
        {
            userIDs = [.. _driversConectados.Keys];
        }

        if (!userIDs.Any())
        {
            await Clients.Caller.SendAsync("ListaDriversConectados", new List<object>());
            return;
        }

        var drivers = await _context.Drivers
            .Include(d => d.User)
            .Where(d => userIDs.Contains(d.UserID))
            .Select(d => new
            {
                UserId = d.UserID,
                FullName = d.User.FullName,
                Placa = d.Placa,
                Photo = d.User.Photo,
                Available = d.Available,
            })
            .ToListAsync();

        await Clients.Caller.SendAsync("ListaDriversConectados", drivers);
    }

    public async Task SolicitarConclusion(int cargaId, string driverName, int totalPedidos)
    {
        // Notificar a todos los Operators en tiempo real
        await Clients.Group("Operators").SendAsync("CargaPendiente", new
        {
            CargaId = cargaId,
            DriverName = driverName,
            Total = totalPedidos,
            Message = $"{driverName} solicita concluir la carga #{cargaId} ({totalPedidos} pedidos)"
        });

        // También notificar a Admins y Supervisors
        await Clients.Group("Admins").SendAsync("CargaPendiente", new
        {
            CargaId = cargaId,
            DriverName = driverName,
            Total = totalPedidos,
            Message = $"{driverName} solicita concluir la carga #{cargaId}"
        });
    }

}