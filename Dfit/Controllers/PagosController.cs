// Dfit/Controllers/PagosController.cs
// Controlador especializado en transacciones financieras, caja y reportes de recaudación.
// Delega el cálculo de descuentos y vigencias a IPagoService.
// Proporciona rutas compatibles con los formularios de mostrador y enlaces del sidebar.
// Cumple con la directriz de profundidad condicional <= 2 niveles y comentarios explicativos.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Dfit.Models;
using Dfit.Models.Common;
using Dfit.Services;
using Dfit.Utils;

namespace Dfit.Controllers
{
    // Controlador para cobro de mensualidades, historial de transacciones y balances contables.
    // Expone los endpoints de cobranza física, consulta de pagos por socio y reportes financieros.
    // Incorpora captura controlada de excepciones para renderizado limpio de pantallas HTTP.
    [Authorize]
    public class PagosController : Controller
    {
        // Servicio de orquestación de operaciones de pago y reportería.
        private readonly IPagoService _pagoService;
        // Instancia de logger para trazabilidad diagnóstica de operaciones financieras.
        private readonly ILogger<PagosController> _logger;

        // Constructor con inyección del servicio de aplicación de pagos y el registrador de eventos.
        // Permite desacoplamiento para pruebas unitarias e inversión de dependencias.
        public PagosController(IPagoService pagoService, ILogger<PagosController> logger)
        {
            _pagoService = pagoService;
            _logger = logger;
        }

        // Carga el formulario de registro de cobro para un socio determinado.
        // Restringido al personal de caja y administradores de DFIT Power.
        [Authorize(Roles = RolesSistema.AdminORecepcionista)]
        [HttpGet]
        [Route("Pagos/RegistrarPago")]
        [Route("Home/RegistrarPago")]
        public async Task<IActionResult> RegistrarPago(string usuarioId, string? returnUrl = null)
        {
            try
            {
                // Recupera la información del socio y catálogo de planes disponibles.
                var (usuario, membresias) = await _pagoService.ObtenerDatosRegistrarPagoAsync(usuarioId);

                // Cláusula de guarda si el socio no existe en el sistema.
                if (usuario == null)
                {
                    return NotFound();
                }

                // Almacena datos contextuales para inicializar el formulario de cobro.
                ViewBag.Usuario = usuario;
                ViewBag.Membresias = membresias;
                ViewBag.ReturnUrl = returnUrl;

                return View("~/Views/Home/RegistrarPago.cshtml");
            }
            catch (Exception ex)
            {
                // Registro estructurado ante fallos imprevistos de lectura.
                _logger.LogError(ex, "Fallo al inicializar formulario de cobro para el usuario {UsuarioId}.", usuarioId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // Procesa la transacción de pago y renueva la suscripción del socio.
        // Aplica el cálculo de descuentos y registra la nueva vigencia en Firestore.
        [Authorize(Roles = RolesSistema.AdminORecepcionista)]
        [HttpPost]
        [Route("Pagos/RegistrarPago")]
        [Route("Home/RegistrarPago")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPago(Pago model, string? returnUrl = null)
        {
            try
            {
                // Si el modelo no supera las validaciones de entrada se recarga el formulario.
                if (!ModelState.IsValid)
                {
                    var (usuario, membresias) = await _pagoService.ObtenerDatosRegistrarPagoAsync(model.UsuarioId);
                    ViewBag.Usuario = usuario;
                    ViewBag.Membresias = membresias;
                    ViewBag.ReturnUrl = returnUrl;
                    return View("~/Views/Home/RegistrarPago.cshtml", model);
                }

                // Ejecuta el cobro aplicando patrón Strategy y extensión de vigencia.
                var (exito, mensaje, _, _, _) = await _pagoService.ProcesarPagoAsync(model);
                if (exito)
                {
                    TempData["MensajeExito"] = mensaje;
                }

                // Redirección segura a la URL de retorno si fue especificada localmente.
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Recepcionistas son derivados a su panel de recepción tras cobrar.
                if (User.IsInRole(RolesSistema.Recepcionista))
                {
                    return RedirectToAction("Recepcion", "Recepcion");
                }

                // Administradores son derivados a la consola principal de usuarios.
                return RedirectToAction("Dashboard", "Usuarios");
            }
            catch (Exception ex)
            {
                // Registro contextual del fallo transaccional.
                _logger.LogError(ex, "Error crítico al procesar pago para socio {UsuarioId}.", model.UsuarioId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // Muestra el historial de pagos efectuados por un socio específico.
        // Valida que el solicitante sea propietario de la cuenta o administrador del gimnasio.
        // Previene ataques de referencia directa insegura a objetos (IDOR según OWASP A01).
        [HttpGet]
        [Route("Pagos/PagosUsuario/{id?}")]
        [Route("Home/PagosUsuario/{id?}")]
        public async Task<IActionResult> PagosUsuario(string? id)
        {
            try
            {
                // Identifica al usuario activo mediante claims de sesión.
                var currentUserId = User.ObtenerUsuarioId();
                if (string.IsNullOrEmpty(id))
                {
                    id = currentUserId;
                }

                // Validación de control de acceso para prevenir filtración de datos de otros socios.
                if (string.IsNullOrEmpty(id) || !User.EsAdminOPropietario(id))
                {
                    return Forbid();
                }

                // Consulta histórica de transacciones del socio.
                var listaPagos = await _pagoService.ObtenerPagosUsuarioAsync(id);
                return View("~/Views/Home/PagosUsuario.cshtml", listaPagos);
            }
            catch (Exception ex)
            {
                // Registro ante anomalías de lectura en la cuenta del usuario.
                _logger.LogError(ex, "Error al consultar historial de pagos para socio {UsuarioId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // Genera y presenta el reporte financiero consolidado del gimnasio.
        // Calcula ingresos totales históricos y recaudación acumulada del mes en curso.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Pagos/ReporteFinanciero")]
        [Route("Home/ReporteFinanciero")]
        public async Task<IActionResult> ReporteFinanciero()
        {
            try
            {
                // Obtiene la lista completa de transacciones y los totales agregados.
                var (pagos, ingresosTotales, pagosDelMes) = await _pagoService.ObtenerReporteFinancieroAsync();

                // Pasa los balances calculados hacia la vista mediante ViewBag.
                ViewBag.IngresosTotales = ingresosTotales;
                ViewBag.PagosDelMes = pagosDelMes;

                // Renderiza la plantilla formal de reportes financieros.
                return View("~/Views/Home/ReporteFinanciero.cshtml", pagos);
            }
            catch (Exception ex)
            {
                // Registro estructurado del incidente para trazabilidad operativa.
                _logger.LogError(ex, "Error al generar el reporte financiero consolidado.");

                // Respuesta HTTP controlada que activa la pantalla de error 500 configurada.
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
