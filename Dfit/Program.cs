using Microsoft.EntityFrameworkCore;
using Dfit.Models;
using Dfit.Models.Patterns.Singleton;
using Dfit.Models.Patterns.Proxy;
using Dfit.Models.Patterns.Strategy;
using Dfit.Models.Patterns.Observer;
using Dfit.Services;

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.Cookies;

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

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "DFIT_AUTH_SESSION";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Configurar Firebase y Firestore mediante variables de entorno (.env / Render)
FirebaseConfig.Configure(builder);

// builder.Services.AddDbContext<AppDbContext>(...); // Eliminado en migración a Firebase

builder.Services.AddSingleton<IQRTokenService, QRTokenManager>();
builder.Services.AddSingleton<IRateLimiter, RateLimiter>();
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

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// Seed de datos eliminado (ahora en Firestore)

app.Run();
