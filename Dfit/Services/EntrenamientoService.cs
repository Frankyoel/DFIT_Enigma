// Dfit/Services/EntrenamientoService.cs
// Implementación de lógica de negocio para rutinas, asignaciones y pase de lista de clases.
// Desacopla las funciones avanzadas de entrenadores de la capa de presentación.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfit.Models;
using Dfit.Repositories.Contracts;
using Dfit.Utils;

namespace Dfit.Services
{
    // Servicio de aplicación para rutinas deportivas, clases grupales y asistencia de socios.
    // Encapsula las operaciones del catálogo de ejercicios, asignaciones temporales y control de asistencia a clases.
    public class EntrenamientoService : IEntrenamientoService
    {
        // Repositorios requeridos para rutinas, horarios y usuarios.
        private readonly IRutinaRepository _rutinaRepo;
        private readonly IRutinaUsuarioRepository _rutinaUsuarioRepo;
        private readonly IHorarioRepository _horarioRepo;
        private readonly IAsistenciaRepository _asistenciaRepo;
        private readonly IUsuarioRepository _usuarioRepo;

        // Constructor con inyección de repositorios de entrenamiento.
        public EntrenamientoService(
            IRutinaRepository rutinaRepo,
            IRutinaUsuarioRepository rutinaUsuarioRepo,
            IHorarioRepository horarioRepo,
            IAsistenciaRepository asistenciaRepo,
            IUsuarioRepository usuarioRepo)
        {
            _rutinaRepo = rutinaRepo;
            _rutinaUsuarioRepo = rutinaUsuarioRepo;
            _horarioRepo = horarioRepo;
            _asistenciaRepo = asistenciaRepo;
            _usuarioRepo = usuarioRepo;
        }

        // Obtiene las rutinas activas para el catálogo de entrenamientos.
        public async Task<IReadOnlyList<Rutina>> ObtenerRutinasActivasAsync()
        {
            // Recupera rutinas activas desde Firestore.
            return await _rutinaRepo.GetActivasAsync();
        }

        // Registra una nueva rutina en el catálogo.
        public async Task CrearRutinaAsync(Rutina model)
        {
            // Cláusula de guarda para evitar entidades nulas.
            if (model == null)
            {
                return;
            }

            // Marca la rutina como activa y persiste.
            model.Activa = true;
            await _rutinaRepo.AddAsync(model);
        }

        // Obtiene una rutina por su identificador.
        public async Task<Rutina?> ObtenerRutinaParaEditarAsync(string id)
        {
            // Recupera la rutina por clave primaria.
            return await _rutinaRepo.GetByIdAsync(id);
        }

        // Actualiza los datos de una rutina existente.
        public async Task ActualizarRutinaAsync(Rutina model)
        {
            // Valida que el modelo esté presente.
            if (model == null)
            {
                return;
            }

            // Aplica la actualización en el repositorio.
            await _rutinaRepo.UpdateAsync(model.Id, model);
        }

        // Desactiva lógicamente una rutina sin borrar su historial de uso.
        public async Task EliminarRutinaAsync(string id)
        {
            // Desactiva el registro en Firestore.
            await _rutinaRepo.DesactivarAsync(id);
        }

        // Asigna una rutina a un socio desactivando cualquier rutina previa.
        public async Task AsignarRutinaASocioAsync(string usuarioId, string rutinaId, int duracionDias)
        {
            // Cláusula de guarda para identificadores vacíos.
            if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(rutinaId))
            {
                return;
            }

            // Desactiva asignaciones activas previas del socio.
            await _rutinaUsuarioRepo.DesactivarRutinasPreviasAsync(usuarioId);

            // Calcula fechas de inicio y finalización del plan de ejercicios.
            var fechaInicio = DateTime.UtcNow;
            var dias = duracionDias > 0 ? duracionDias : 30;
            var fechaFin = fechaInicio.AddDays(dias);

            // Registra la nueva asignación activa en Firestore.
            await _rutinaUsuarioRepo.AddAsync(new RutinaUsuario
            {
                UsuarioId = usuarioId,
                RutinaId = rutinaId,
                FechaAsignacion = fechaInicio,
                FechaFin = fechaFin,
                Estado = "Activa"
            });
        }

        // Desasigna o cancela una rutina asignada al socio.
        public async Task RemoverRutinaSocioAsync(string rutinaUsuarioId)
        {
            // Cláusula de guarda para ID de asignación vacío.
            if (string.IsNullOrWhiteSpace(rutinaUsuarioId))
            {
                return;
            }

            // Localiza la asignación y cambia su estado a Cancelada.
            var ru = await _rutinaUsuarioRepo.GetByIdAsync(rutinaUsuarioId);
            if (ru != null)
            {
                ru.Estado = "Cancelada";
                await _rutinaUsuarioRepo.UpdateAsync(rutinaUsuarioId, ru);
            }
        }

        // Obtiene la lista semanal de clases grupales ordenadas por hora de inicio.
        public async Task<IReadOnlyList<Horario>> ObtenerHorariosAsync()
        {
            // Retorna los horarios ordenados cronológicamente.
            return await _horarioRepo.GetHorariosOrdenadosAsync();
        }

        // Crea una nueva sesión de clase en el cronograma semanal.
        public async Task CrearHorarioAsync(Horario model)
        {
            // Cláusula de guarda para horario nulo.
            if (model == null)
            {
                return;
            }

            // Persiste el nuevo horario en Firestore.
            await _horarioRepo.AddAsync(model);
        }

        // Obtiene una sesión de horario para su formulario de edición.
        public async Task<Horario?> ObtenerHorarioParaEditarAsync(string id)
        {
            // Recupera la sesión de clase por su ID.
            return await _horarioRepo.GetByIdAsync(id);
        }

        // Actualiza los parámetros de una clase grupal.
        public async Task ActualizarHorarioAsync(Horario model)
        {
            // Valida la entidad recibida.
            if (model == null)
            {
                return;
            }

            // Aplica los cambios en el almacén de datos.
            await _horarioRepo.UpdateAsync(model.Id, model);
        }

        // Elimina una clase del cronograma semanal.
        public async Task EliminarHorarioAsync(string id)
        {
            // Borra el documento del horario.
            await _horarioRepo.DeleteAsync(id);
        }

        // Obtiene el roster de socios y asistencias de hoy para una clase grupal.
        public async Task<(Horario? Horario, IReadOnlyList<Usuario> Socios, HashSet<string> AsistenciasHoy)> ObtenerClaseRosterAsync(string horarioId, string? search = null)
        {
            // Carga la información de la sesión de clase.
            var horario = await _horarioRepo.GetByIdAsync(horarioId);
            if (horario == null)
            {
                return (null, new List<Usuario>(), new HashSet<string>());
            }

            // Carga los socios registrados y filtra por búsqueda si aplica.
            var socios = await _usuarioRepo.GetSociosAsync(search);
            var hoy = DateTime.UtcNow;

            // Identifica qué socios ya asistieron hoy a esta clase específica.
            var asistenciasHoy = await _asistenciaRepo.GetAsistenciasHoyPorHorarioAsync(horarioId, hoy);

            return (horario, socios.OrderBy(u => u.Nombres).ToList(), asistenciasHoy);
        }

        // Registra la asistencia de un socio a una clase grupal específica.
        public async Task<(bool Exito, string Mensaje)> RegistrarAsistenciaClaseAsync(string usuarioId, string horarioId)
        {
            // Obtiene la clase para incluir su nombre en el registro.
            var horario = await _horarioRepo.GetByIdAsync(horarioId);
            if (horario == null)
            {
                return (false, "Horario no encontrado.");
            }

            // Registra el acceso a la clase colectiva en Firestore.
            await _asistenciaRepo.AddAsync(new Asistencia
            {
                UsuarioId = usuarioId,
                HorarioId = horarioId,
                NombreClase = horario.NombreClase,
                FechaHoraEntrada = DateTime.UtcNow
            });

            // Genera mensaje formateado mediante el catálogo de mensajes.
            var mensaje = string.Format(MensajesNotificacion.AsistenciaClaseRegistradaExito, horario.NombreClase);
            return (true, mensaje);
        }
    }
}
