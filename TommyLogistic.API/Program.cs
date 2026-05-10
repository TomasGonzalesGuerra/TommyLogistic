using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using TommyLogistic.Api.Data;
using TommyLogistic.Api.Helpers;
using TommyLogistic.API.Data;
using TommyLogistic.API.Hubs;
using TommyLogistic.API.Services;
using TommyLogistic.Shared.Entities;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddDbContext<LogisticDataContext>(dc => dc.UseSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]));
builder.Services.AddTransient<SeedDb>();
builder.Services.AddIdentity<User, IdentityRole>(x =>
{
    x.User.RequireUniqueEmail = true;
    x.Password.RequireDigit = false;
    x.Password.RequiredUniqueChars = 0;
    x.Password.RequireLowercase = false;
    x.Password.RequireNonAlphanumeric = false;
    x.Password.RequireUppercase = false;
}).AddEntityFrameworkStores<LogisticDataContext>().AddDefaultTokenProviders();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtKey"]!)),
        ClockSkew = TimeSpan.Zero,
    };
    options.Events = new JwtBearerEvents
    {
        // JWT para SignalR llega por query string, no por header
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddScoped<IUserHelper, UserHelper>();
builder.Services.AddScoped<IFileStorage, FileStorage>();
builder.Services.AddScoped<OrderEventService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<OrderHubService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
        policy.WithOrigins(builder.Configuration["ClientUrl"]!)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

SeedData(app);
static void SeedData(WebApplication app)
{
    IServiceScopeFactory? scopedFactory = app.Services.GetService<IServiceScopeFactory>();
    using IServiceScope? scope = scopedFactory!.CreateScope();
    SeedDb? service = scope.ServiceProvider.GetService<SeedDb>();
    service!.SeedAsync().Wait();
}


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseCors("BlazorClient");
app.UseCors(x => x
.AllowAnyMethod()
.AllowAnyHeader()
.SetIsOriginAllowed(origin => true)
.AllowCredentials());
app.MapHub<OrderHub>("/hubs/orders");

app.Run();
