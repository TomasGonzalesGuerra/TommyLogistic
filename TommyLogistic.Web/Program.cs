using TommyLogistic.Web;
using Blazored.LocalStorage;
using TommyLogistic.Web.Auth;
using TommyLogistic.Web.Services;
using TommyLogistic.Web.Repositories;
using Microsoft.AspNetCore.Components.Web;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["PublicApiUrl"]!) });
// ── Auth ───────────────────────────────────────────────────────────────────
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<LogisticWebProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, LogisticWebProvider>(x => x.GetRequiredService<LogisticWebProvider>());
builder.Services.AddScoped<ILoginService, LogisticWebProvider>(x => x.GetRequiredService<LogisticWebProvider>());
// ── Servicios ──────────────────────────────────────────────────────────────
builder.Services.AddSweetAlert2();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<SesionService>();
builder.Services.AddScoped<DriverService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<OperatorService>();
builder.Services.AddSingleton<ComingSoonService>();
builder.Services.AddSingleton<NotificationService>();

var host = builder.Build();
var authProvider = host.Services.GetRequiredService<LogisticWebProvider>();
await authProvider.InitializeAsync();
await host.RunAsync();

//await builder.Build().RunAsync();
