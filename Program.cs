using LanzaTuIdea.Api.Data;
using LanzaTuIdea.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Controladores y Seguridad
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});

builder.Services.AddExceptionHandler<LanzaTuIdea.Api.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Base de Datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de Opciones del Servicio AD
builder.Services.Configure<AdServiceOptions>(builder.Configuration.GetSection("AdService"));

// ============================================================
// LÓGICA DE AUTENTICACIÓN DINÁMICA (MODO NINJA)
// ============================================================
// Intentamos buscar la clase Mock por nombre. 
// Si no tienes el archivo localmente, mockType será null.
Type? mockType = Type.GetType("LanzaTuIdea.Api.Services.MockAdServiceClient");

if (builder.Environment.IsDevelopment() && mockType != null)
{
    // Si estamos en casa (Dev) y creaste el archivo ignorado, lo usamos
    builder.Services.AddScoped(typeof(IAdServiceClient), mockType);
}
else
{
    // En la oficina o en Producción (donde no existe el archivo Mock), usamos el real
    builder.Services.AddHttpClient<IAdServiceClient, AdServiceClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdServiceOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            client.BaseAddress = new Uri(options.BaseUrl);
        }

        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 10);
    });
}
// ============================================================

// Autenticación por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LanzaTuIdea.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configuración del Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler();

// Inicialización de Datos (Seed)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    await SeedData.InitializeAsync(db, configuration, environment);
}

app.UseHttpsRedirection();
var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
