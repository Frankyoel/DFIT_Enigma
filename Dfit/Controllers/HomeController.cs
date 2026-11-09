// Dfit/Controllers/HomeController.cs
// Controlador base y punto de entrada principal para navegación, errores y privacidad.
// Refactorizado bajo Clean Architecture: la lógica de negocio y persistencia fue extraída a controladores especializados.
// Cumple con métricas CLOC, profundidad <= 2 niveles y cero acoplamiento directo a Firestore.
// Enruta a cada usuario a su panel correspondiente según su rol verificado en sesión.

using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dfit.Models;
using Dfit.Utils;

namespace Dfit.Controllers
{
    // Controlador de navegación raíz, redirección según rol de sesión y páginas estándar.
    [Authorize]
    public class HomeController : Controller
    {
        // Redirige al panel correspondiente según el rol del usuario autenticado.
        // Evalúa permisos mediante ClaimsPrincipal y PermisosHelper.
        [HttpGet]
        [Route("")]
        [Route("Home")]
        [Route("Home/Index")]
        public IActionResult Index()
        {
            // Administradores son derivados a la consola de usuarios y dashboard.
            if (User.EsAdmin())
            {
                return RedirectToAction("Dashboard", "Usuarios");
            }

            // Entrenadores son dirigidos al catálogo de rutinas deportivas.
            if (User.EsEntrenador())
            {
                return RedirectToAction("Rutinas", "Entrenamiento");
            }

            // Socios regulares son dirigidos a su portal personal de membresía.
            var currentUserId = User.ObtenerUsuarioId();
            return RedirectToAction("MiMembresia", "Membresias", new { id = currentUserId });
        }

        // Redirección de compatibilidad para inicio de sesión delegada a AuthController.
        // Permite acceso anónimo sin requerir sesión activa previa.
        [AllowAnonymous]
        [HttpGet]
        [Route("Home/Login")]
        public IActionResult Login() => RedirectToAction("Login", "Auth");

        // Redirección de compatibilidad para registro de usuarios delegada a AuthController.
        // Soporta enlaces legados que apuntaban al controlador Home.
        [AllowAnonymous]
        [HttpGet]
        [Route("Home/Register")]
        public IActionResult Register() => RedirectToAction("Register", "Auth");

        // Redirección de compatibilidad para cierre de sesión delegada a AuthController.
        // Notifica el cierre sincronizado a todas las pestañas abiertas.
        [AllowAnonymous]
        [HttpGet]
        [Route("Home/Logout")]
        public IActionResult Logout() => RedirectToAction("Logout", "Auth");

        // Muestra la política de privacidad y protección de datos del gimnasio.
        // Accesible públicamente para cumplir con normativas de transparencia.
        [AllowAnonymous]
        [HttpGet]
        [Route("Home/Privacy")]
        public IActionResult Privacy()
        {
            return View("~/Views/Home/Privacy.cshtml");
        }

        // Despliega la vista de error no controlado con trazabilidad de identificador.
        // Provee el código de seguimiento de error para diagnóstico del equipo técnico.
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Home/Error")]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View("~/Views/Home/Error.cshtml", new ErrorViewModel { RequestId = requestId });
        }
    }
}
