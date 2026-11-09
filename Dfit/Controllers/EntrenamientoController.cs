// Dfit/Controllers/EntrenamientoController.cs
// Controlador especializado en rutinas de ejercicios, horarios semanales y asistencia a clases.
// Delega la lógica deportiva y de cronogramas a IEntrenamientoService.
// Proporciona compatibilidad con las rutas y enlaces del menú de entrenadores y socios.
// Cumple con la directriz de profundidad condicional <= 2 niveles y comentarios explicativos.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dfit.Models;
using Dfit.Models.Common;
using Dfit.Services;

namespace Dfit.Controllers
{
    // Controlador de planes de entrenamiento, videos de rutinas y clases colectivas.
    // Gestiona el catálogo de ejercicios, la programación semanal y el pase de lista de clases.
    [Authorize]
    public class EntrenamientoController : Controller
    {
        // Servicio de orquestación de rutinas y horarios inyectado por DI.
        private readonly IEntrenamientoService _entrenamientoService;

        // Constructor con inyección del servicio de aplicación de entrenamiento.
        public EntrenamientoController(IEntrenamientoService entrenamientoService)
        {
            _entrenamientoService = entrenamientoService;
        }

        // Muestra el catálogo de rutinas activas para socios y entrenadores.
        // Permite filtrar ejercicios por nivel y reproducir videos instructivos.
        [HttpGet]
        [Route("Entrenamiento/Rutinas")]
        [Route("Home/Rutinas")]
        public async Task<IActionResult> Rutinas()
        {
            // Consulta de rutinas disponibles en el catálogo deportivo.
            var list = await _entrenamientoService.ObtenerRutinasActivasAsync();
            return View("~/Views/Home/Rutinas.cshtml", list);
        }

        // Presenta el formulario para crear una nueva rutina de ejercicios.
        // Restringido al personal de entrenadores y administradores.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/CreateRutina")]
        [Route("Home/CreateRutina")]
        public IActionResult CreateRutina()
        {
            return View("~/Views/Home/CreateRutina.cshtml");
        }

        // Procesa la inserción de una nueva rutina deportiva en el catálogo.
        // Valida nombre, nivel, descripción y enlace multimedia de demostración.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/CreateRutina")]
        [Route("Home/CreateRutina")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRutina(Rutina model)
        {
            // Cláusula de guarda si el modelo no supera las validaciones.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/CreateRutina.cshtml", model);
            }

            // Inserción persistente en el catálogo de rutinas.
            await _entrenamientoService.CrearRutinaAsync(model);
            return RedirectToAction(nameof(Rutinas));
        }

        // Carga la información de una rutina para su modificación.
        // Retorna 404 si el documento de rutina no existe.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/EditRutina/{id}")]
        [Route("Home/EditRutina/{id}")]
        public async Task<IActionResult> EditRutina(string id)
        {
            var model = await _entrenamientoService.ObtenerRutinaParaEditarAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            return View("~/Views/Home/EditRutina.cshtml", model);
        }

        // Procesa la actualización de los ejercicios y multimedia de la rutina.
        // Modifica la descripción, nivel de exigencia o enlace de video.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/EditRutina/{id?}")]
        [Route("Home/EditRutina/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRutina(Rutina model)
        {
            // Cláusula de guarda para validar el estado del modelo.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/EditRutina.cshtml", model);
            }

            // Actualización persistente en el repositorio de rutinas.
            await _entrenamientoService.ActualizarRutinaAsync(model);
            return RedirectToAction(nameof(Rutinas));
        }

        // Desactiva lógicamente una rutina del catálogo.
        // Mantiene la integridad histórica de rutinas asignadas a socios.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/DeleteRutina/{id}")]
        [Route("Home/DeleteRutina/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRutina(string id)
        {
            await _entrenamientoService.EliminarRutinaAsync(id);
            return RedirectToAction(nameof(Rutinas));
        }

        // Asigna una rutina de entrenamiento al socio con vigencia configurable.
        // Desactiva cualquier asignación previa activa para evitar duplicidades.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/AsignarRutina")]
        [Route("Home/AsignarRutina")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarRutina(string usuarioId, string rutinaId, int duracionDias = 30)
        {
            var (exito, mensaje) = await _entrenamientoService.AsignarRutinaASocioAsync(usuarioId, rutinaId, duracionDias);

            // Registro de notificación de feedback para la interfaz.
            if (exito)
            {
                TempData["MensajeExito"] = mensaje;
            }
            else
            {
                TempData["MensajeError"] = mensaje;
            }

            return RedirectToAction("Profile", "Usuarios", new { id = usuarioId });
        }

        // Remueve la asignación de una rutina activa de un socio.
        // Actualiza el estado de la asignación a Cancelada.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/RemoverRutina")]
        [Route("Home/RemoverRutina")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverRutina(string rutinaUsuarioId, string usuarioId)
        {
            var (exito, mensaje) = await _entrenamientoService.RemoverRutinaSocioAsync(rutinaUsuarioId);
            if (exito)
            {
                TempData["MensajeExito"] = mensaje;
            }

            return RedirectToAction("Profile", "Usuarios", new { id = usuarioId });
        }

        // Muestra la programación semanal de clases colectivas.
        // Ordena las sesiones cronológicamente por hora de inicio.
        [HttpGet]
        [Route("Entrenamiento/Horarios")]
        [Route("Home/Horarios")]
        public async Task<IActionResult> Horarios()
        {
            var list = await _entrenamientoService.ObtenerHorariosOrdenadosAsync();
            return View("~/Views/Home/Horarios.cshtml", list);
        }

        // Presenta el formulario para programar una nueva clase grupal.
        // Restringido al personal técnico de entrenamiento.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/CreateHorario")]
        [Route("Home/CreateHorario")]
        public IActionResult CreateHorario()
        {
            return View("~/Views/Home/CreateHorario.cshtml");
        }

        // Procesa la inserción de una nueva sesión de clase en el calendario.
        // Almacena día de la semana, hora de inicio, fin, clase y cupo máximo.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/CreateHorario")]
        [Route("Home/CreateHorario")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHorario(Horario model)
        {
            // Cláusula de guarda para validar el horario programado.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/CreateHorario.cshtml", model);
            }

            // Inserción en la colección de horarios de Firestore.
            await _entrenamientoService.CrearHorarioAsync(model);
            return RedirectToAction(nameof(Horarios));
        }

        // Carga una sesión de clase para su reprogramación o cambio de instructor.
        // Retorna 404 si la sesión no fue encontrada.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/EditHorario/{id}")]
        [Route("Home/EditHorario/{id}")]
        public async Task<IActionResult> EditHorario(string id)
        {
            var model = await _entrenamientoService.ObtenerHorarioParaEditarAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            return View("~/Views/Home/EditHorario.cshtml", model);
        }

        // Procesa la actualización de los datos de un horario de clase.
        // Modifica días, horas, instructores o aforos máximos permitidos.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/EditHorario/{id?}")]
        [Route("Home/EditHorario/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHorario(Horario model)
        {
            // Cláusula de guarda para validar el modelo.
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/EditHorario.cshtml", model);
            }

            // Modificación en el repositorio de horarios.
            await _entrenamientoService.ActualizarHorarioAsync(model);
            return RedirectToAction(nameof(Horarios));
        }

        // Elimina una clase del cronograma semanal.
        // Operación administrativa de depuración del calendario.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/DeleteHorario/{id}")]
        [Route("Home/DeleteHorario/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHorario(string id)
        {
            await _entrenamientoService.EliminarHorarioAsync(id);
            return RedirectToAction(nameof(Horarios));
        }

        // Muestra el roster de asistencia y lista de socios disponibles para la clase grupal.
        // Permite a los instructores tomar asistencia en tiempo real.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpGet]
        [Route("Entrenamiento/ClaseRoster")]
        [Route("Home/ClaseRoster")]
        public async Task<IActionResult> ClaseRoster(string horarioId, string? search = null)
        {
            var (horario, socios, asistenciasHoy) = await _entrenamientoService.ObtenerClaseRosterAsync(horarioId, search);
            if (horario == null)
            {
                return NotFound();
            }

            ViewBag.Horario = horario;
            ViewBag.AsistenciasHoy = asistenciasHoy;
            ViewBag.SearchTerm = search;

            return View("~/Views/Home/ClaseRoster.cshtml", socios);
        }

        // Registra la asistencia de un socio a la clase grupal seleccionada.
        // Asocia el identificador de clase y nombre al registro de entrada.
        [Authorize(Roles = RolesSistema.AdminOEntrenador)]
        [HttpPost]
        [Route("Entrenamiento/RegistrarAsistenciaClase")]
        [Route("Home/RegistrarAsistenciaClase")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAsistenciaClase(string usuarioId, string horarioId)
        {
            var (exito, mensaje) = await _entrenamientoService.RegistrarAsistenciaClaseAsync(usuarioId, horarioId);
            if (exito)
            {
                TempData["MensajeExito"] = mensaje;
            }

            return RedirectToAction(nameof(ClaseRoster), new { horarioId });
        }
    }
}
