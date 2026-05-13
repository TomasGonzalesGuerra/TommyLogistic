using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using TommyLogistic.Web.Helpers;

namespace TommyLogistic.Web.Services;

public class NotificationService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private HubConnection? _hubConnection;
    private bool _started;

    private readonly string _tokenKey = "TOKEN_KEY"; // igual que en LogisticWebProvider

    public event Action<string>? OnNewDriver;
    public event Action<string>? OnNewOrder;

    // Llamas esto una vez, cuando el layout carga ya con sesión activa
    public async Task StartAsync(string rol, string userId)
    {
        if (_started) return; // evita doble conexión

        // Leemos el token directo del localStorage, igual que hace LogisticWebProvider
        var token = await _jsRuntime.GetLocalStorage(_tokenKey);
        if (token is null) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7229/hubs/notifications", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token.ToString());
            })
            .WithAutomaticReconnect()
            .Build();

        // Escuchar evento: nuevo compañero driver
        _hubConnection.On<object>("NewDriverJoined", data =>
        {
            OnNewDriver?.Invoke(data.ToString()!);
        });

        // Escuchar evento: pedido asignado
        _hubConnection.On<object>("NewOrderAssigned", data =>
        {
            OnNewOrder?.Invoke(data.ToString()!);
        });

        await _hubConnection.StartAsync();
        _started = true;

        // Solo los Repartidores se suscriben a los grupos
        if (rol == "Driver")
        {
            // Grupo general: broadcast cuando llega un nuevo compañero
            await _hubConnection.InvokeAsync("JoinDriversGroup");

            // Grupo personal: solo este driver recibe sus pedidos
            await _hubConnection.InvokeAsync("JoinPersonalGroup", userId);
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync();
            _started = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}