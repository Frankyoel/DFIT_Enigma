// Dfit/Controllers/UsuariosController.cs
// Controlador especializado en administración de usuarios, perfiles y valoraciones antropométricas.
// Desacoplado de la persistencia directa a Firestore mediante la inyección de IUsuarioService.
// Mantiene compatibilidad con las vistas existentes mediante rutas canónicas y alias en /Home.
// Cumple con profundidad condicional <= 2 niveles y estándares de comentarios claros con //.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dfit.Models;
using Dfit.Models.Common;
using Dfit.Services;
using Dfit.Utils;

namespace Dfit.Controllers
{
    // Controlador para la gestión de usuarios, perfiles, asignación de rutinas y valoraciones físicas.
    // Expone endpoints protegidos por RBAC con roles Admin, Entrenador y Socio.
    [Authorize]
    public class UsuariosController : Controller
    {
        // Servicio de orquestación de usuarios inyectado por DI.
        private readonly IUsuarioService _usuarioService;

        // Constructor con inyección de dependencias del servicio de negocio.
        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // Muestra la consola principal de usuarios y membresías activas.
        // Soporta filtrado de socios en memoria mediante parámetro search.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Usuarios/Dashboard")]
        [Route("Home/Dashboard")]
        public async Task<IActionResult> Dashboard(string? search = null)
        {
            // Carga usuarios y membresías activas optimizadas sin N+1 queries.
            var (usuarios, membresiasActivas) = await _usuarioService.ObtenerDashboardAsync(search);

            // Comparte datos con la vista mediante ViewBag para mantener compatibilidad Razor.
            ViewBag.MembresiasActivas = membresiasActivas;
            ViewBag.SearchTerm = search;

            // Retorna la vista original de Dashboard situada en Views/Home/.
            return View("~/Views/Home/Dashboard.cshtml", usuarios);
        }

        // Presenta el formulario para creación de nuevos usuarios administrativos o socios.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Usuarios/CreateUser")]
        [Route("Home/CreateUser")]
        public IActionResult CreateUser()
        {
            // Despliega formulario limpio de creación de cuenta.
            return View("~/Views/Home/CreateUser.cshtml");
        }

        // Procesa la inserción de un nuevo usuario con hashing de contraseña.
        // Aplica validación de modelo y mitigación contra ataques CSRF.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpPost]
        [Route("Usuarios/CreateUser")]
        [Route("Home/CreateUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(Usuario model)
        {
            // Cláusula de guarda para entradas no válidas según DataAnnotations.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/CreateUser.cshtml", model);
            }

            // Invoca al servicio para hashear y persistir el usuario en Firestore.
            await _usuarioService.CrearUsuarioAsync(model);
            return RedirectToAction(nameof(Dashboard));
        }

        // Carga la información de un usuario para su edición administrativa.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpGet]
        [Route("Usuarios/EditUser/{id}")]
        [Route("Home/EditUser/{id}")]
        public async Task<IActionResult> EditUser(string id)
        {
            // Recupera la proyección EditUserViewModel desde el servicio.
            var model = await _usuarioService.ObtenerUsuarioParaEditarAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            // Retorna formulario poblado con los datos del usuario.
            return View("~/Views/Home/EditUser.cshtml", model);
        }

        // Procesa las modificaciones de datos de perfil de usuario.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpPost]
        [Route("Usuarios/EditUser/{id?}")]
        [Route("Home/EditUser/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            // Cláusula de guarda si las validaciones de ViewModel fallan.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/EditUser.cshtml", model);
            }

            // Aplica los cambios mediante el servicio de usuarios.
            await _usuarioService.ActualizarUsuarioAsync(model);
            return RedirectToAction(nameof(Dashboard));
        }

        // Elimina permanentemente un usuario por su identificador.
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpPost]
        [Route("Usuarios/DeleteUser/{id}")]
        [Route("Home/DeleteUser/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            // Delega la eliminación al servicio de aplicación.
            await _usuarioService.EliminarUsuarioAsync(id);
            return RedirectToAction(nameof(Dashboard));
        }

        // Alterna el estado activo o inactivo de un usuario (bloqueo/desbloqueo).
        [Authorize(Roles = RolesSistema.Admin)]
        [HttpPost]
        [Route("Usuarios/ToggleUserStatus/{id}")]
        [Route("Home/ToggleUserStatus/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            // Cambia el estado booleano en Firestore.
            await _usuarioService.ToggleEstadoUsuarioAsync(id);
            return RedirectToAction(nameof(Dashboard));
        }

        // Despliega el perfil del usuario, su rutina deportiva y su historial antropométrico.
        // Aplica control de autorización para asegurar que un socio solo vea su propio perfil.
        [HttpGet]
        [Route("Usuarios/Profile/{id?}")]
        [Route("Home/Profile/{id?}")]
        public async Task<IActionResult> Profile(string? id)
        {
            // Obtiene el identificador del usuario autenticado en la sesión.
            var currentUserId = User.ObtenerUsuarioId();
            if (string.IsNullOrEmpty(id))
            {
                id = currentUserId;
            }

            // Valida que el solicitante sea Administrador o el legítimo propietario de la cuenta.
            if (!User.EsAdminOPropietario(id))
            {
                return RedirectToAction(nameof(Profile), new { id = currentUserId });
            }

            // Carga la información consolidada de perfil, rutinas y evaluaciones.
            var (model, rutinaAsignada, rutinasDisponibles, evaluaciones) =
                await _usuarioService.ObtenerPerfilCompletoAsync(id!, User.EsAdminOEntrenador());

            // Si el documento de usuario no existe, devuelve código 404.
            if (model == null)
            {
                return NotFound();
            }

            // Población de ViewBag requerida por la vista Razor Views/Home/Profile.cshtml.
            ViewBag.RutinaAsignada = rutinaAsignada;
            ViewBag.RutinasDisponibles = rutinasDisponibles;
            ViewBag.Evaluaciones = evaluaciones;

            return View("~/Views/Home/Profile.cshtml", model);
        }

        // Procesa la actualización de datos personales o contraseña del perfil.
        [HttpPost]
        [Route("Usuarios/Profile/{id?}")]
        [Route("Home/Profile/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            // Verifica que el usuario no intente mutar cuentas ajenas a menos que sea Admin.
            var currentUserId = User.ObtenerUsuarioId();
            if (!User.EsAdmin() && currentUserId != model.Id)
            {
                return Forbid();
            }

            // Valida el estado del modelo antes de guardar.
            if (ModelState.IsValid)
            {
                await _usuarioService.ActualizarPerfilAsync(model);
                ViewBag.Message = MensajesNotificacion.PerfilActualizadoExito;
            }

            // Recarga los datos auxiliares de rutina y evaluaciones físicas para la vista.
            var (_, rutinaAsignada, rutinasDisponibles, evaluaciones) =
                await _usuarioService.ObtenerPerfilCompletoAsync(model.Id, User.EsAdminOEntrenador());

            ViewBag.RutinaAsignada = rutinaAsignada;
            ViewBag.RutinasDisponibles = rutinasDisponibles;
            ViewBag.Evaluaciones = evaluaciones;

            return View("~/Views/Home/Profile.cshtml", model);
        }

        // Guarda la nota interna privada del entrenador sobre el socio.
        // Restringido exclusivamente al personal de entrenamiento y administración.
        // Permite registrar avances deportivos, recomendaciones de carga y precauciones.
        // Solo visible para el staff autorizado y oculta para el socio según RBAC.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Usuarios/GuardarNotaInterna")]
        [Route("Home/GuardarNotaInterna")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarNotaInterna(string usuarioId, string notasEntrenador)
        {
            // Cláusula de guarda para validación de entrada.
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return BadRequest();
            }

            // Persiste la nota privada del entrenador en Firestore.
            await _usuarioService.GuardarNotaInternaAsync(usuarioId, notasEntrenador);
            TempData["MensajeExito"] = MensajesNotificacion.NotasEntrenadorActualizadasExito;

            return RedirectToAction(nameof(Profile), new { id = usuarioId });
        }

        // Registra una nueva evaluación física y antropométrica para el socio.
        // Calcula automáticamente el Índice de Masa Corporal (IMC) y masa grasa.
        // Almacena la fecha UTC y la identidad del entrenador evaluador para auditoría.
        // Previene la inserción de registros huérfanos validando el ID del socio evaluado.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Usuarios/GuardarEvaluacion")]
        [Route("Home/GuardarEvaluacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEvaluacion(EvaluacionFisica model)
        {
            // Cláusula de guarda para ID de usuario ausente.
            if (string.IsNullOrWhiteSpace(model.UsuarioId))
            {
                return BadRequest();
            }

            // Obtiene identidad del entrenador autenticado.
            var entrenadorId = User.ObtenerUsuarioId() ?? "";
            var entrenadorNombre = User.Identity?.Name ?? "Entrenador";

            // Registra la evaluación calculando el IMC automáticamente.
            await _usuarioService.GuardarEvaluacionFisicaAsync(model, entrenadorId, entrenadorNombre);
            TempData["MensajeExito"] = MensajesNotificacion.EvaluacionRegistradaExito;

            return RedirectToAction(nameof(Profile), new { id = model.UsuarioId });
        }
    }
}
