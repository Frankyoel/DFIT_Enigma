using Microsoft.EntityFrameworkCore;
using Dfit.Models;
using Dfit.Models.Patterns.Singleton;
using Dfit.Models.Patterns.Proxy;
using Dfit.Models.Patterns.Strategy;
using Dfit.Models.Patterns.Observer;

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

using Dfit.Utils;

var builder = WebApplication.CreateBuilder(args);

// Cargar variables de entorno desde archivo .env (si existe)
EnvLoader.Load(builder.Environment.ContentRootPath);

// Soporte para Render y plataformas Cloud que asignan la variable PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    if (builder.Environment.IsDevelopment())
    {
        builder.WebHost.UseUrls($"http://localhost:{port}");
    }
    else
    {
        builder.WebHost.UseUrls($"http://*:{port}");
    }
}

builder.Services.AddControllersWithViews();

// Configurar Firebase y Firestore mediante variables de entorno (.env / Render)
FirebaseConfig.Configure(builder);

// builder.Services.AddDbContext<AppDbContext>(...); // Eliminado en migración a Firebase

builder.Services.AddSingleton<IQRTokenService, QRTokenManager>();
builder.Services.AddScoped<RealGymAccess>();
builder.Services.AddScoped<IGymAccess, GymAccessProxy>();
builder.Services.AddScoped<PaymentContext>();

var registrationSubject = new UserRegistrationSubject();
registrationSubject.Attach(new EmailNotificationObserver());
registrationSubject.Attach(new LogNotificationObserver());
builder.Services.AddSingleton<ISubject>(registrationSubject);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
