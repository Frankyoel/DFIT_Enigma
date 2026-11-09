// Dfit/Controllers/MembresiasController.cs
// Controlador especializado en el catálogo de membresías, planes y suscripciones de socios.
// Delega la orquestación de negocio y cálculo de vigencias a IMembresiaService.
// Proporciona compatibilidad absoluta de enrutamiento con las vistas Razor de /Home.
// Cumple con la regla de comentarios explicativos con // y profundidad <= 2 niveles.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dfit.Models;
using Dfit.Models.Common;
using Dfit.Services;
using Dfit.Utils;

namespace Dfit.Controllers
{
    // Controlador para administración del catálogo de planes y suscripciones de usuarios.
    // Expone endpoints para Administradores y para socios regulares que consultan su membresía.
    [Authorize]
    public class MembresiasController : Controller
    {
        // Servicio de aplicación para gestión de membresías inyectado por DI.
        private readonly IMembresiaService _membresiaService;

        // Constructor con inyección de dependencias.
        public MembresiasController(IMembresiaService membresiaService)
        {
            _membresiaService = membresiaService;
        }

        // Muestra el catálogo completo de membresías para administración.
        // Restringido al rol Administrador.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Membresias")]
        [Route("Home/Membresias")]
        public async Task<IActionResult> Membresias()
        {
            // Carga todos los planes registrados en el catálogo.
            var list = await _membresiaService.ObtenerCatalogoMembresiasAsync();
            return View("~/Views/Home/Membresias.cshtml", list);
        }

        // Presenta el formulario para creación de un nuevo plan.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Membresias/CreateMembresia")]
        [Route("Home/CreateMembresia")]
        public IActionResult CreateMembresia()
        {
            // Retorna la vista de creación de planes situada en Views/Home/.
            return View("~/Views/Home/CreateMembresia.cshtml");
        }

        // Procesa la inserción de un nuevo plan en el catálogo.
        // Protegido con token Anti-CSRF contra ataques de falsificación de petición.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpPost]
        [Route("Membresias/CreateMembresia")]
        [Route("Home/CreateMembresia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMembresia(Membresia model)
        {
            // Cláusula de guarda para entradas no válidas.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/CreateMembresia.cshtml", model);
            }

            // Persiste el nuevo plan en Firestore.
            await _membresiaService.CrearMembresiaAsync(model);
            return RedirectToAction(nameof(Membresias));
        }

        // Carga la información de una membresía para su modificación.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Membresias/EditMembresia/{id}")]
        [Route("Home/EditMembresia/{id}")]
        public async Task<IActionResult> EditMembresia(string id)
        {
            // Recupera la membresía por ID desde el servicio de aplicación.
            var model = await _membresiaService.ObtenerMembresiaParaEditarAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            // Retorna la vista de edición con los datos cargados.
            return View("~/Views/Home/EditMembresia.cshtml", model);
        }

        // Procesa la actualización de un plan de membresía.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpPost]
        [Route("Membresias/EditMembresia/{id?}")]
        [Route("Home/EditMembresia/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMembresia(Membresia model)
        {
            // Cláusula de guarda para validación de datos.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/EditMembresia.cshtml", model);
            }

            // Aplica la modificación en la base de datos.
            await _membresiaService.ActualizarMembresiaAsync(model);
            return RedirectToAction(nameof(Membresias));
        }

        // Elimina permanentemente un plan del catálogo.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpPost]
        [Route("Membresias/DeleteMembresia/{id}")]
        [Route("Home/DeleteMembresia/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMembresia(string id)
        {
            // Borra el documento del plan en Firestore.
            await _membresiaService.EliminarMembresiaAsync(id);
            return RedirectToAction(nameof(Membresias));
        }

        // Presenta el formulario para asignar una membresía a un socio específico.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Membresias/AssignMembresia")]
        [Route("Home/AssignMembresia")]
        public async Task<IActionResult> AssignMembresia(string usuarioId)
        {
            // Carga los planes disponibles excluyendo planes de entrada libre.
            var list = await _membresiaService.ObtenerPlanesParaAsignarAsync();

            ViewBag.UsuarioId = usuarioId;
            ViewBag.Membresias = list;

            return View("~/Views/Home/AssignMembresia.cshtml");
        }

        // Procesa la asignación directa de membresía calculando su fecha fin.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpPost]
        [Route("Membresias/AssignMembresia")]
        [Route("Home/AssignMembresia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignMembresia(string usuarioId, string membresiaId)
        {
            // Asigna el plan calculando vigencia y desactivando suscripciones previas.
            await _membresiaService.AsignarMembresiaASocioAsync(usuarioId, membresiaId);
            return RedirectToAction("Dashboard", "Usuarios");
        }

        // Despliega la información del plan vigente del socio en su portal personal.
        // Valida que el socio autenticado solo acceda a su propia información de membresía.
        [HttpGet]
        [Route("Membresias/MiMembresia/{id?}")]
        [Route("Home/MiMembresia/{id?}")]
        public async Task<IActionResult> MiMembresia(string? id)
        {
            // Obtiene el identificador del socio desde los Claims de autenticación.
            var currentUserId = User.ObtenerUsuarioId();
            if (string.IsNullOrEmpty(id))
            {
                id = currentUserId;
            }

            // Comprueba propiedad de la cuenta para impedir acceso no autorizado (OWASP A01).
            if (string.IsNullOrEmpty(id) || !User.EsAdminOPropietario(id))
            {
                return Forbid();
            }

            // Carga la membresía activa del socio.
            var mu = await _membresiaService.ObtenerMiMembresiaAsync(id);
            return View("~/Views/Home/MiMembresia.cshtml", mu);
        }

        // Permite al socio cancelar su suscripción activa desde su portal.
        [HttpPost]
        [Route("Membresias/CancelMembresia")]
        [Route("Home/CancelMembresia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMembresia(string membresiaUsuarioId, string usuarioId)
        {
            // Verifica que el usuario que cancela sea dueño de la membresía o Administrador.
            if (!User.EsAdminOPropietario(usuarioId))
            {
                return Forbid();
            }

            // Cancela la suscripción desactivando el registro en Firestore.
            await _membresiaService.CancelarMembresiaAsync(membresiaUsuarioId);
            return RedirectToAction(nameof(MiMembresia), new { id = usuarioId });
        }

        // Permite a la administración remover una suscripción activa de un socio.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpPost]
        [Route("Membresias/AdminRemoveMembresia")]
        [Route("Home/AdminRemoveMembresia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminRemoveMembresia(string membresiaUsuarioId)
        {
            // Desactiva la membresía del socio desde la consola de administración.
            await _membresiaService.CancelarMembresiaAsync(membresiaUsuarioId);
            return RedirectToAction("Dashboard", "Usuarios");
        }
    }
}
