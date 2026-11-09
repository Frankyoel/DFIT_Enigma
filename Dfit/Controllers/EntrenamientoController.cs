// Dfit/Controllers/EntrenamientoController.cs
// Controlador especializado en rutinas de ejercicios, horarios semanales y asistencia a clases.
// Delega la lógica deportiva y de cronogramas a IEntrenamientoService.
// Proporciona compatibilidad con las rutas y enlaces del menú de entrenadores y socios.
// Cumple con profundidad condicional <= 2 niveles y comentarios explicativos con //.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dfit.Models;
using Dfit.Models.Common;
using Dfit.Services;

namespace Dfit.Controllers
{
    // Controlador de planes de entrenamiento, videos de rutinas y clases colectivas.
    // Expone endpoints protegidos para perfiles de Entrenador, Admin y Socios.
    [Authorize]
    public class EntrenamientoController : Controller
    {
        // Servicio de orquestación de rutinas y horarios inyectado por DI.
        private readonly IEntrenamientoService _entrenamientoService;

        // Constructor con inyección de dependencias del servicio de entrenamiento.
        public EntrenamientoController(IEntrenamientoService entrenamientoService)
        {
            _entrenamientoService = entrenamientoService;
        }

        // Muestra el catálogo de rutinas activas para socios y entrenadores.
        // Permite filtrar ejercicios por nivel y reproducir videos demostrativos.
        [HttpGet]
        [Route("Entrenamiento/Rutinas")]
        [Route("Home/Rutinas")]
        public async Task<IActionResult> Rutinas()
        {
            // Recupera rutinas activas desde el servicio.
            var list = await _entrenamientoService.ObtenerRutinasActivasAsync();
            return View("~/Views/Home/Rutinas.cshtml", list);
        }

        // Presenta el formulario para crear una nueva rutina de ejercicios.
        // Restringido exclusivamente a perfiles de Entrenador o Administrador.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/CreateRutina")]
        [Route("Home/CreateRutina")]
        public IActionResult CreateRutina()
        {
            // Despliega formulario de creación de ejercicios.
            return View("~/Views/Home/CreateRutina.cshtml");
        }

        // Procesa la inserción de una nueva rutina deportiva en el catálogo.
        // Protegido con token Anti-CSRF contra ataques de falsificación de petición.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/CreateRutina")]
        [Route("Home/CreateRutina")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRutina(Rutina model)
        {
            // Cláusula de guarda para entradas no válidas.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/CreateRutina.cshtml", model);
            }

            // Guarda la rutina en el almacén de datos.
            await _entrenamientoService.CrearRutinaAsync(model);
            return RedirectToAction(nameof(Rutinas));
        }

        // Carga la información de una rutina para su modificación.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/EditRutina/{id}")]
        [Route("Home/EditRutina/{id}")]
        public async Task<IActionResult> EditRutina(string id)
        {
            // Carga la rutina correspondiente por su ID.
            var model = await _entrenamientoService.ObtenerRutinaParaEditarAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            // Retorna la vista de edición con la rutina cargada.
            return View("~/Views/Home/EditRutina.cshtml", model);
        }

        // Procesa la actualización de los ejercicios y multimedia de la rutina.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/EditRutina/{id?}")]
        [Route("Home/EditRutina/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRutina(Rutina model)
        {
            // Cláusula de guarda si el modelo no es válido.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/EditRutina.cshtml", model);
            }

            // Aplica los cambios en el catálogo.
            await _entrenamientoService.ActualizarRutinaAsync(model);
            return RedirectToAction(nameof(Rutinas));
        }

        // Desactiva lógicamente una rutina del catálogo sin borrar su historial.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/DeleteRutina/{id}")]
        [Route("Home/DeleteRutina/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRutina(string id)
        {
            // Desactiva la rutina de forma asíncrona.
            await _entrenamientoService.EliminarRutinaAsync(id);
            return RedirectToAction(nameof(Rutinas));
        }

        // Asigna una rutina de entrenamiento al socio con vigencia configurable.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/AsignarRutina")]
        [Route("Home/AsignarRutina")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarRutina(string usuarioId, string rutinaId, int duracionDias = 30)
        {
            // Asigna la rutina al socio y desactiva asignaciones previas.
            var (exito, mensaje) = await _entrenamientoService.AsignarRutinaASocioAsync(usuarioId, rutinaId, duracionDias);

            // Almacena mensaje de feedback según el resultado.
            if (exito)
            {
                TempData["MensajeExito"] = mensaje;
            }
            else
            {
                TempData["MensajeError"] = mensaje;
            }

            // Redirige al perfil del socio.
            return RedirectToAction("Profile", "Usuarios", new { id = usuarioId });
        }

        // Remueve la asignación de una rutina activa de un socio.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/RemoverRutina")]
        [Route("Home/RemoverRutina")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverRutina(string rutinaUsuarioId, string usuarioId)
        {
            // Cancela la asignación de la rutina en el repositorio.
            var (exito, mensaje) = await _entrenamientoService.RemoverRutinaSocioAsync(rutinaUsuarioId);
            if (exito)
            {
                TempData["MensajeExito"] = mensaje;
            }

            // Redirige al perfil del socio.
            return RedirectToAction("Profile", "Usuarios", new { id = usuarioId });
        }

        // Muestra la programación semanal de clases colectivas.
        [HttpGet]
        [Route("Entrenamiento/Horarios")]
        [Route("Home/Horarios")]
        public async Task<IActionResult> Horarios()
        {
            // Carga horarios semanales ordenados por hora de inicio.
            var list = await _entrenamientoService.ObtenerHorariosOrdenadosAsync();
            return View("~/Views/Home/Horarios.cshtml", list);
        }

        // Presenta el formulario para programar una nueva clase grupal.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/CreateHorario")]
        [Route("Home/CreateHorario")]
        public IActionResult CreateHorario()
        {
            // Retorna la vista de creación de horarios en Views/Home/.
            return View("~/Views/Home/CreateHorario.cshtml");
        }

        // Procesa la inserción de una nueva sesión de clase en el calendario.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/CreateHorario")]
        [Route("Home/CreateHorario")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHorario(Horario model)
        {
            // Cláusula de guarda para validación de entrada.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/CreateHorario.cshtml", model);
            }

            // Persiste el nuevo horario en Firestore.
            await _entrenamientoService.CrearHorarioAsync(model);
            return RedirectToAction(nameof(Horarios));
        }

        // Carga una sesión de clase para su reprogramación o cambio de instructor.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/EditHorario/{id}")]
        [Route("Home/EditHorario/{id}")]
        public async Task<IActionResult> EditHorario(string id)
        {
            // Recupera la sesión de clase por su ID.
            var model = await _entrenamientoService.ObtenerHorarioParaEditarAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            // Despliega el formulario de edición con los datos poblados.
            return View("~/Views/Home/EditHorario.cshtml", model);
        }

        // Procesa la actualización de los datos de un horario de clase.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/EditHorario/{id?}")]
        [Route("Home/EditHorario/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHorario(Horario model)
        {
            // Cláusula de guarda si el modelo no es válido.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/EditHorario.cshtml", model);
            }

            // Aplica los cambios en el cronograma semanal.
            await _entrenamientoService.ActualizarHorarioAsync(model);
            return RedirectToAction(nameof(Horarios));
        }

        // Elimina una clase del cronograma semanal por su ID.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/DeleteHorario/{id}")]
        [Route("Home/DeleteHorario/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHorario(string id)
        {
            // Borra la clase en la base de datos.
            await _entrenamientoService.EliminarHorarioAsync(id);
            return RedirectToAction(nameof(Horarios));
        }

        // Muestra el roster de asistencia y lista de socios disponibles para la clase grupal.
        // Facilita a entrenadores marcar check-in en lote durante sesiones dirigidas.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/ClaseRoster")]
        [Route("Home/ClaseRoster")]
        public async Task<IActionResult> ClaseRoster(string horarioId, string? search = null)
        {
            // Obtiene la clase, los socios activos y los accesos registrados hoy.
            var (horario, socios, asistenciasHoy) = await _entrenamientoService.ObtenerClaseRosterAsync(horarioId, search);
            if (horario == null)
            {
                return NotFound();
            }

            // Comparte datos con la vista mediante ViewBag.
            ViewBag.Horario = horario;
            ViewBag.AsistenciasHoy = asistenciasHoy;
            ViewBag.SearchTerm = search;

            // Retorna la vista de pase de lista en Views/Home/.
            return View("~/Views/Home/ClaseRoster.cshtml", socios);
        }

        // Registra la asistencia de un socio a la clase grupal seleccionada.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/RegistrarAsistenciaClase")]
        [Route("Home/RegistrarAsistenciaClase")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAsistenciaClase(string usuarioId, string horarioId)
        {
            // Registra el ingreso del socio en la clase indicada.
            var (exito, mensaje) = await _entrenamientoService.RegistrarAsistenciaClaseAsync(usuarioId, horarioId);
            if (exito)
            {
                TempData["MensajeExito"] = mensaje;
            }

            // Recarga el roster de la clase para mostrar el estado actualizado.
            return RedirectToAction(nameof(ClaseRoster), new { horarioId });
        }
    }
}
