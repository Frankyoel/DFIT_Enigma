using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Google.Cloud.Firestore;
using Dfit.Models;
using Dfit.Models.Patterns.Builder;
using Dfit.Models.Patterns.Observer;

namespace Dfit.Controllers
{
    /// <summary>
    /// Controlador responsable exclusivo de la autenticación de usuarios y registro de nuevas cuentas.
    /// Desacopla la lógica de identidad de HomeController cumpliendo el Principio de Responsabilidad Única (SRP).
    /// </summary>
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly FirestoreDb _db;
        private readonly ISubject _userRegistrationSubject;

        public AuthController(
            ILogger<AuthController> logger,
            FirestoreDb db,
            ISubject userRegistrationSubject)
        {
            _logger = logger;
            _db = db;
            _userRegistrationSubject = userRegistrationSubject;
        }

        // =========================================================================
        // 1. INICIO DE SESIÓN (LOGIN)
        // =========================================================================

        [AllowAnonymous]
        [HttpGet]
        [Route("")]
        [Route("login")]
        [Route("Auth/Login")]
        public IActionResult Login(string? returnUrl = null)
        {
            // Reverse Guard: Si el usuario ya está autenticado, redirigir al Dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                {
                    return LocalRedirect(returnUrl);
                }

                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Home");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return RedirectToAction("MiMembresia", "Home", new { id = userId });
            }

            ViewData["HideSidebar"] = true;
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("")]
        [Route("login")]
        [Route("Auth/Login")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["HideSidebar"] = true;
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var query = _db.Collection("Usuarios")
                    .WhereEqualTo("Correo", model.Correo)
                    .WhereEqualTo("PasswordHash", model.Password);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                    return View(model);
                }

                var userDoc = snapshot.Documents[0];
                if (!IsUserActive(userDoc))
                {
                    ModelState.AddModelError("", "Tu cuenta ha sido desactivada. Contacta al administrador.");
                    return View(model);
                }

                var nombre = userDoc.ContainsField("Nombres") ? userDoc.GetValue<string>("Nombres") : model.Correo;
                var rol = userDoc.ContainsField("Rol") ? userDoc.GetValue<string>("Rol") : "Usuario";

                // Limpiar cualquier credencial residual previa antes de emitir la nueva identidad
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();

                // Emitir Claims y Cookie de Autenticación oficial de ASP.NET Core
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userDoc.Id),
                    new Claim(ClaimTypes.Name, nombre),
                    new Claim(ClaimTypes.Email, model.Correo),
                    new Claim(ClaimTypes.Role, rol)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Respaldar sesión en memoria para compatibilidad retroactiva
                HttpContext.Session.SetString("UserId", userDoc.Id);
                HttpContext.Session.SetString("UserEmail", model.Correo);
                HttpContext.Session.SetString("UserName", nombre);
                HttpContext.Session.SetString("UserRole", rol);

                _logger.LogInformation("Usuario {Email} autenticado correctamente con rol {Rol}.", model.Correo, rol);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                {
                    return LocalRedirect(returnUrl);
                }

                if (rol == "Admin")
                {
                    return RedirectToAction("Dashboard", "Home");
                }

                return RedirectToAction("MiMembresia", "Home", new { id = userDoc.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al autenticar usuario {Email}", model.Correo);
                ModelState.AddModelError("", "Ocurrió un error inesperado al procesar el ingreso. Intente nuevamente.");
                return View(model);
            }
        }

        // =========================================================================
        // 2. CIERRE DE SESIÓN (LOGOUT)
        // =========================================================================

        [HttpGet]
        [Route("logout")]
        [Route("Auth/Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            _logger.LogInformation("Usuario cerró sesión y credenciales destruidas correctamente.");
            return RedirectToAction("Login");
        }

        // =========================================================================
        // 3. REGISTRO DE USUARIOS (REGISTER)
        // =========================================================================

        [AllowAnonymous]
        [HttpGet]
        [Route("registro")]
        [Route("Auth/Register")]
        public IActionResult Register()
        {
            // Reverse Guard: Si el usuario ya está autenticado, redirigir según su rol
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Home");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return RedirectToAction("MiMembresia", "Home", new { id = userId });
            }

            ViewData["HideSidebar"] = true;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("registro")]
        [Route("Auth/Register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewData["HideSidebar"] = true;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var snap = await _db.Collection("Usuarios")
                    .WhereEqualTo("Correo", model.Correo)
                    .GetSnapshotAsync();

                if (snap.Documents.Count > 0)
                {
                    ModelState.AddModelError("Correo", "Este correo ya está registrado.");
                    return View(model);
                }

                var docRef = _db.Collection("Usuarios").Document();
                var nuevoUsuario = BuildNewUser(model, docRef.Id);

                await docRef.SetAsync(new
                {
                    Nombres = nuevoUsuario.Nombres,
                    Apellidos = nuevoUsuario.Apellidos,
                    Correo = nuevoUsuario.Correo,
                    DNI = nuevoUsuario.DNI ?? "",
                    Celular = nuevoUsuario.Celular ?? "",
                    PasswordHash = nuevoUsuario.PasswordHash,
                    Rol = nuevoUsuario.Rol,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = nuevoUsuario.Activo
                });

                _userRegistrationSubject.Notify(nuevoUsuario);
                _logger.LogInformation("Nuevo usuario registrado con ID {UserId}", docRef.Id);

                TempData["SuccessMessage"] = "Cuenta creada con éxito. Inicia sesión con tus credenciales.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar cuenta para {Email}", model.Correo);
                ModelState.AddModelError("", "Ocurrió un error al registrar la cuenta. Intente nuevamente.");
                return View(model);
            }
        }

        // =========================================================================
        // 4. ACCESO DENEGADO (ACCESS DENIED)
        // =========================================================================

        [AllowAnonymous]
        [HttpGet]
        [Route("access-denied")]
        [Route("Auth/AccessDenied")]
        public IActionResult AccessDenied()
        {
            ViewData["HideSidebar"] = true;
            return View();
        }

        // =========================================================================
        // MÉTODOS AUXILIARES DE SOPORTE (Step-Down Rule)
        // =========================================================================

        private static bool IsUserActive(DocumentSnapshot userDoc)
        {
            return !userDoc.ContainsField("Activo") || userDoc.GetValue<bool>("Activo");
        }

        private static Usuario BuildNewUser(RegisterViewModel model, string documentId)
        {
            var builder = new UsuarioBuilder();
            var usuario = builder
                .SetDatosBasicos(model.Nombres, model.Apellidos, model.Correo)
                .SetPassword(model.Password)
                .SetRol("Usuario")
                .SetDefaults()
                .Build();

            usuario.Id = documentId;
            usuario.DNI = model.DNI;
            usuario.Celular = model.Celular;
            return usuario;
        }
    }
}
