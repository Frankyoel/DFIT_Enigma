// Dfit/Controllers/AsistenciaController.cs
// Controlador especializado en el control de acceso, aforos físicos y check-in por código QR.
// Delega la emisión y validación de tokens a IAsistenciaService.
// Proporciona rutas compatibles con los torniquetes virtuales y pantallas de recepción.
// Aplica cláusulas de guarda para garantizar profundidad condicional <= 2 niveles.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dfit.Models.Common;
using Dfit.Services;

namespace Dfit.Controllers
{
    // Controlador de operaciones de asistencia, accesos por torniquete y pantalla QR.
    // Gestiona tanto ingresos manuales en mostrador como auto-checkin por código QR.
    [Authorize]
    public class AsistenciaController : Controller
    {
        // Servicio de orquestación de accesos y asistencias.
        private readonly IAsistenciaService _asistenciaService;

        // Constructor con inyección del servicio de aplicación de asistencias.
        public AsistenciaController(IAsistenciaService asistenciaService)
        {
            _asistenciaService = asistenciaService;
        }

        // Muestra la lista de asistencias del día y el control de ingreso manual.
        // Restringido a perfiles de Administrador y Recepcionista.
        [Authorize(Roles = RolesSistema.AdminORecepcionista)]
        [HttpGet]
        [Route("Asistencia")]
        [Route("Home/Asistencia")]
        public async Task<IActionResult> Asistencia()
        {
            // Carga las asistencias del día actual y el catálogo de socios activos.
            var (asistenciasHoy, usuariosActivos) = await _asistenciaService.ObtenerAsistenciaDashboardAsync();

            ViewBag.Usuarios = usuariosActivos;
            return View("~/Views/Home/Asistencia.cshtml", asistenciasHoy);
        }

        // Procesa el ingreso manual de un socio verificando la vigencia de su membresía.
        // Bloquea el paso si el socio no cuenta con suscripción activa vigente.
        [Authorize(Roles = RolesSistema.AdminORecepcionista)]
        [HttpPost]
        [Route("Asistencia/RegistrarAsistencia")]
        [Route("Home/RegistrarAsistencia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAsistencia(string usuarioId)
        {
            // Valida y registra el acceso físico en el repositorio de asistencias.
            var (exito, mensaje) = await _asistenciaService.RegistrarAsistenciaManualAsync(usuarioId);

            // Si el socio no tiene membresía vigente se muestra alerta de denegación.
            if (!exito)
            {
                TempData["ErrorAcceso"] = mensaje;
                return RedirectToAction(nameof(Asistencia));
            }

            // Notificación de acceso concedido presentada en el panel de control.
            TempData["ExitoAcceso"] = mensaje;
            return RedirectToAction(nameof(Asistencia));
        }

        // Despliega la pantalla con el código QR dinámico de acceso para recepción.
        // Proyectada en monitores de torniquetes para autoservicio de check-in.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Asistencia/PantallaQR")]
        [Route("Home/PantallaQR")]
        public IActionResult PantallaQR()
        {
            return View("~/Views/Home/PantallaQR.cshtml");
        }

        // Endpoint JSON que expone el token QR dinámico con expiración periódica.
        // Consumido mediante polling o SSE desde la pantalla física de recepción.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Asistencia/GenerarTokenQR")]
        [Route("Home/GenerarTokenQR")]
        public IActionResult GenerarTokenQR()
        {
            // Obtiene el token activo de ciclo corto generado por QRTokenManager.
            var token = _asistenciaService.GenerarTokenQR();
            return Json(new { token });
        }

        // Procesa el check-in cuando un socio escanea el código QR desde su dispositivo.
        // Valida la vigencia del token y registra la entrada mediante GymAccessProxy.
        [HttpGet]
        [Route("Asistencia/CheckIn")]
        [Route("Home/CheckIn")]
        public IActionResult CheckIn(string token)
        {
            // Extracción del identificador de socio a partir de claims de sesión o cookie.
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? HttpContext.Session.GetString("UserId");

            // Si el usuario no tiene sesión activa es derivado a la pantalla de login.
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Ejecución de la verificación criptográfica del token y apertura de acceso.
            var (exito, mensaje) = _asistenciaService.CheckInQR(token, userIdString);

            ViewBag.Success = exito;
            ViewBag.Message = mensaje;

            return View("~/Views/Home/CheckInResult.cshtml");
        }
    }
}
