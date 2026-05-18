using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using TommyLogistic.Shared.Enums;
using TommyLogistic.Web.Helpers;

namespace TommyLogistic.Web.Services;

public class NotificationService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private bool _started;
    private HubConnection? _hubConnection;
    private readonly string _tokenKey = "TOKEN_KEY";
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    // 👇 Historial en memoria (máximo 20)
    public List<NotificationItem> Historial { get; } = [];
    public int NoLeidas => Historial.Count(n => !n.Leida);

    public event Action<string>? OnNewDriver;
    public event Action<string>? OnNewOrder;
    public event Action? OnDashboardUpdate;
    public event Action? OnNotificacionCambiada;

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

        await _hubConnection.StartAsync();
        _started = true;

        if (rol == nameof(UserEnum.Driver))
        {
            // Grupo general: broadcast cuando llega un nuevo compañero
            await _hubConnection.InvokeAsync("JoinDriversGroup");

            // Grupo personal: solo este driver recibe sus pedidos
            await _hubConnection.InvokeAsync("JoinPersonalGroup", userId);
        }

        if (rol == nameof(UserEnum.Admin))
        {
            await _hubConnection.InvokeAsync("JoinAdminGroup");
        }
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

}

public record NotificationItem
{
    public string Icono { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public DateTime Tiempo { get; set; }
    public bool Leida { get; set; }
}