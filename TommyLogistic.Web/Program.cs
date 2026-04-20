using Blazored.LocalStorage;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TommyLogistic.Web;
using TommyLogistic.Web.Auth;
using TommyLogistic.Web.Repositories;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7229") });
// ── Auth ───────────────────────────────────────────────────────────────────
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<LogisticWebProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, LogisticWebProvider>(x => x.GetRequiredService<LogisticWebProvider>());
builder.Services.AddScoped<ILoginService, LogisticWebProvider>(x => x.GetRequiredService<LogisticWebProvider>());

// ── Dependencias -───────────────────────────────────────────────────────────
builder.Services.AddSweetAlert2();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IRepository, Repository>();


await builder.Build().RunAsync();
