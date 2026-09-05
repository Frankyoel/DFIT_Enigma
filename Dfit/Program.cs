using Microsoft.EntityFrameworkCore;
using Dfit.Models;
using Dfit.Models.Patterns.Singleton;
using Dfit.Models.Patterns.Proxy;
using Dfit.Models.Patterns.Strategy;
using Dfit.Models.Patterns.Observer;

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Configurar Firebase
var firebaseKeyPath = Path.Combine(builder.Environment.ContentRootPath, "firebase-key.json");
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseKeyPath);

if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(firebaseKeyPath)
    });
}

var firestoreDb = FirestoreDb.Create("dfit-gym");
builder.Services.AddSingleton(firestoreDb);

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

// Seed de datos eliminado (ahora en Firestore)

app.Run();
