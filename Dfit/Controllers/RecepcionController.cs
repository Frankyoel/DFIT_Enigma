// Dfit/Controllers/RecepcionController.cs
// Controlador especializado en el módulo de mostrador, recepción y cobranza ágil de DFIT Power.
// Desacoplado de la persistencia directa mediante IRecepcionService.
// Proporciona rutas canónicas y alias en /Home para compatibilidad con la interfaz Razor.
// Implementa control de acceso basado en roles para personal de Recepción y Administradores.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dfit.Models;
using Dfit.Models.Common;
using Dfit.Services;

namespace Dfit.Controllers
{
    // Controlador de operaciones de recepción, control de vigencias y registro rápido de socios.
    // Expone las vistas del mostrador y el alta rápida con cobro en un único paso operativo.
    [Authorize(Roles = RolesSistema.AdminORecepcionista)]
    public class RecepcionController : Controller
    {
        // Servicio de orquestación de operaciones de recepción inyectado por DI.
        private readonly IRecepcionService _recepcionService;

        // Constructor con inyección del servicio de aplicación de recepción.
        public RecepcionController(IRecepcionService recepcionService)
        {
            _recepcionService = recepcionService;
        }

        // Muestra la consola principal de recepción con semáforo de vigencias y KPIs diarios.
        // Soporta filtros concurrentes por texto de búsqueda y por estado de suscripción.
        [HttpGet]
        [Route("Recepcion")]
        [Route("Home/Recepcion")]
        public async Task<IActionResult> Recepcion(string? search = null, string? estado = null)
        {
            // Recupera el ViewModel consolidado con métricas calculadas y socios categorizados.
            var model = await _recepcionService.ObtenerRecepcionDashboardAsync(search, estado);

            // Retorna la vista Razor histórica ubicada en Views/Home para evitar breaking changes.
            return View("~/Views/Home/Recepcion.cshtml", model);
        }

        // Procesa el alta ágil de un nuevo socio con cobro optativo en mostrador.
        // Valida unicidad de correo electrónico y hashea la contraseña antes de persistir.
        [HttpPost]
        [Route("Recepcion/RegistrarSocioRecepcion")]
        [Route("Home/RegistrarSocioRecepcion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarSocioRecepcion(
            Usuario model,
            string? membresiaId = null,
            decimal? montoPago = null,
            string? metodoPago = null)
        {
            // Ejecuta el flujo transaccional de registro en el servicio de recepción.
            var (exito, mensaje, _) = await _recepcionService.RegistrarSocioRecepcionAsync(model, membresiaId, montoPago, metodoPago);

            // En caso de error de validación se notifica mediante TempData y se redirige.
            if (!exito)
            {
                TempData["MensajeError"] = mensaje;
                return RedirectToAction(nameof(Recepcion));
            }

            // Notificación de alta exitosa presentada en el panel de recepción.
            TempData["MensajeExito"] = mensaje;
            return RedirectToAction(nameof(Recepcion));
        }
    }
}
