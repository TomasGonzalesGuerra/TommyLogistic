using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Text.Json;
using TommyLogistic.Shared.DTOs.Drivers;
using TommyLogistic.Shared.Enums;
using TommyLogistic.Web.Helpers;

namespace TommyLogistic.Web.Services;

public class NotificationService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private bool _started;
    private HubConnection? _hubConnection;
    private readonly string _tokenKey = "TOKEN_KEY";
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public List<NotificationItem> Historial { get; } = [];
    public int NoLeidas => Historial.Count(n => !n.Leida);
    public List<DriverConectadoDTO> DriversConectados { get; } = [];
    public event Action? OnDriversChanged;
    public event Action<string>? OnNewDriver;
    public event Action<string>? OnNewOrder;
    public event Action? OnDashboardUpdate;
    public event Action? OnNotificacionCambiada;
    public event Action<string>? OnCargaConcluida;

    public async Task StartAsync(string rol, string userId)
    {
        if (_started) return;

        var token = await _jsRuntime.GetLocalStorage(_tokenKey);
        if (token is null) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7229/hubs/notifications", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token.ToString());
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<object>("NewDriverJoined", data => OnNewDriver?.Invoke(data.ToString()!));
        _hubConnection.On<object>("NewOrderAssigned", data => OnNewOrder?.Invoke(data.ToString()!));
        _hubConnection.On("DashboardUpdate", () => OnDashboardUpdate?.Invoke());
        _hubConnection.On<object>("DriverConectado", data =>
        {
            var json = data.ToString()!;
            var driver = JsonSerializer.Deserialize<DriverConectadoDTO>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (driver is null) return;

            // Evitar duplicados
            if (!DriversConectados.Any(d => d.UserId == driver.UserId))
                DriversConectados.Add(driver);

            OnDriversChanged?.Invoke();
        });
        _hubConnection.On<object>("DriverDesconectado", data =>
        {
            var json = data.ToString()!;
            var parsed = JsonDocument.Parse(json);
            var userId = parsed.RootElement.GetProperty("userId").GetString();

            var driver = DriversConectados.FirstOrDefault(d => d.UserId == userId);
            if (driver is not null)
                DriversConectados.Remove(driver);

            OnDriversChanged?.Invoke();
        });
        _hubConnection.On<object>("ListaDriversConectados", data =>
        {
            var json = data.ToString()!;
            var drivers = JsonSerializer.Deserialize<List<DriverConectadoDTO>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            DriversConectados.Clear();
            if (drivers is not null)
                DriversConectados.AddRange(drivers);

            OnDriversChanged?.Invoke();
        });
        _hubConnection.On<object>("CargaConcluida", data =>
        {
            AgregarNotificacion("🎉", "Carga concluida", "Tu carga fue aprobada. ¡Ya puedes recibir nuevas!");
            OnCargaConcluida?.Invoke(data.ToString()!);
        });
        _hubConnection.On<object>("CargaPendienteConclusion", data =>
        {
            AgregarNotificacion("📋", "Solicitud enviada", "Tu solicitud de conclusión fue enviada al operario.");
            OnNewOrder?.Invoke(data.ToString()!);
            OnNotificacionCambiada?.Invoke();
        });


        await _hubConnection.StartAsync();
        _started = true;

        if (rol == nameof(UserEnum.Admin))
        {
            await _hubConnection.InvokeAsync("JoinAdminGroup");
            await _hubConnection.InvokeAsync("GetDriversConectados");
        }

        if (rol == nameof(UserEnum.Supervisor))
            await _hubConnection.InvokeAsync("JoinSupervisorGroup");

        if (rol == nameof(UserEnum.Driver))
        {
            await _hubConnection.InvokeAsync("JoinDriversGroup");
            await _hubConnection.InvokeAsync("JoinPersonalGroup", userId);
            await _hubConnection.InvokeAsync("DriverOnline", userId);
        }

        if (rol == nameof(UserEnum.Operator))
            await _hubConnection.InvokeAsync("JoinOperatorGroup");
    }

    public async Task StopAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync();
            _started = false;
            OnNotificacionCambiada?.Invoke();
        }
    }

    public void MarcarTodasLeidas()
    {
        foreach (var n in Historial)
            n.Leida = true;
        OnNotificacionCambiada?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }

    private void AgregarNotificacion(string icono, string titulo, string mensaje)
    {
        Historial.Insert(0, new NotificationItem
        {
            Icono = icono,
            Titulo = titulo,
            Mensaje = mensaje,
            Tiempo = DateTime.Now,
            Leida = false
        });

        // Máximo 20 notificaciones
        if (Historial.Count > 20)
            Historial.RemoveAt(Historial.Count - 1);

        OnNotificacionCambiada?.Invoke();
    }

    public async Task SolicitarConclusionCargaAsync(int cargaId, string driverName, int totalPedidos)
    {
        if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
            return;

        await _hubConnection.InvokeAsync("SolicitarConclusion", cargaId, driverName, totalPedidos);
    }

}

public record NotificationItem
{
    public string Icono { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public DateTime Tiempo { get; set; }
    public bool Leida { get; set; }
}