// Dfit/Services/EntrenamientoService.cs
// Implementación de lógica de negocio para rutinas, horarios y clases dirigidas.
// Encapsula la gestión de entrenamientos deportivos y el pase de lista de clases.
// Cumple con la directriz de profundidad condicional <= 2 niveles y cláusulas de guarda.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfit.Models;
using Dfit.Repositories.Contracts;
using Dfit.Utils;

namespace Dfit.Services
{
    // Servicio de aplicación para rutinas de entrenamiento, programación semanal y clases colectivas.
    public class EntrenamientoService : IEntrenamientoService
    {
        // Repositorios inyectados para el dominio deportivo.
        private readonly IRutinaRepository _rutinaRepo;
        private readonly IRutinaUsuarioRepository _rutinaUsuarioRepo;
        private readonly IHorarioRepository _horarioRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IAsistenciaRepository _asistenciaRepo;

        // Constructor con inyección de dependencias.
        public EntrenamientoService(
            IRutinaRepository rutinaRepo,
            IRutinaUsuarioRepository rutinaUsuarioRepo,
            IHorarioRepository horarioRepo,
            IUsuarioRepository usuarioRepo,
            IAsistenciaRepository asistenciaRepo)
        {
            _rutinaRepo = rutinaRepo;
            _rutinaUsuarioRepo = rutinaUsuarioRepo;
            _horarioRepo = horarioRepo;
            _usuarioRepo = usuarioRepo;
            _asistenciaRepo = asistenciaRepo;
        }

        // Obtiene todas las rutinas activas del catálogo.
        public async Task<IReadOnlyList<Rutina>> ObtenerRutinasActivasAsync()
        {
            return await _rutinaRepo.GetActivasAsync();
        }

        // Registra una nueva rutina en el catálogo.
        public async Task CrearRutinaAsync(Rutina model)
        {
            if (model == null)
            {
                return;
            }

            model.Activa = true;
            await _rutinaRepo.AddAsync(model);
        }

        // Obtiene una rutina para su edición.
        public async Task<Rutina?> ObtenerRutinaParaEditarAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return await _rutinaRepo.GetByIdAsync(id);
        }

        // Actualiza los parámetros de una rutina existente.
        public async Task ActualizarRutinaAsync(Rutina model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Id))
            {
                return;
            }

            await _rutinaRepo.UpdateAsync(model.Id, model);
        }

        // Da de baja lógica una rutina del catálogo sin borrar su historial de uso.
        public async Task EliminarRutinaAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            await _rutinaRepo.DesactivarAsync(id);
        }

        // Asigna una rutina de entrenamiento a un socio con un plazo de vigencia en días.
        public async Task<(bool Exito, string Mensaje)> AsignarRutinaASocioAsync(string usuarioId, string rutinaId, int duracionDias = 30)
        {
            // Cláusula de guarda para validar parámetros de asignación.
            if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(rutinaId))
            {
                return (false, MensajesNotificacion.DatosAsignacionIncompletos);
            }

            // Desactiva rutinas activas anteriores del socio.
            await _rutinaUsuarioRepo.DesactivarRutinasPreviasAsync(usuarioId);

            var fechaInicio = DateTime.UtcNow;
            var dias = duracionDias > 0 ? duracionDias : 30;
            var fechaFin = fechaInicio.AddDays(dias);

            // Inserción de la nueva asignación activa.
            var asignacion = new RutinaUsuario
            {
                UsuarioId = usuarioId,
                RutinaId = rutinaId,
                FechaAsignacion = fechaInicio,
                FechaFin = fechaFin,
                Estado = "Activa"
            };
            await _rutinaUsuarioRepo.AddAsync(asignacion);

            return (true, MensajesNotificacion.RutinaAsignadaExito);
        }

        // Desasigna o cancela la rutina activa de un socio.
        public async Task<(bool Exito, string Mensaje)> RemoverRutinaSocioAsync(string rutinaUsuarioId)
        {
            if (string.IsNullOrWhiteSpace(rutinaUsuarioId))
            {
                return (false, MensajesNotificacion.DatosAsignacionIncompletos);
            }

            var asignacion = await _rutinaUsuarioRepo.GetByIdAsync(rutinaUsuarioId);
            if (asignacion == null)
            {
                return (false, "Asignación no encontrada.");
            }

            asignacion.Estado = "Cancelada";
            await _rutinaUsuarioRepo.UpdateAsync(rutinaUsuarioId, asignacion);

            return (true, MensajesNotificacion.RutinaDesasignadaExito);
        }

        // Obtiene el cronograma semanal de clases ordenado cronológicamente.
        public async Task<IReadOnlyList<Horario>> ObtenerHorariosOrdenadosAsync()
        {
            return await _horarioRepo.GetHorariosOrdenadosAsync();
        }

        // Registra una nueva clase en el cronograma semanal.
        public async Task CrearHorarioAsync(Horario model)
        {
            if (model == null)
            {
                return;
            }

            await _horarioRepo.AddAsync(model);
        }

        // Obtiene un horario de clase para su edición.
        public async Task<Horario?> ObtenerHorarioParaEditarAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return await _horarioRepo.GetByIdAsync(id);
        }

        // Actualiza los datos de una clase programada.
        public async Task ActualizarHorarioAsync(Horario model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Id))
            {
                return;
            }

            await _horarioRepo.UpdateAsync(model.Id, model);
        }

        // Elimina una clase del cronograma semanal.
        public async Task EliminarHorarioAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            await _horarioRepo.DeleteAsync(id);
        }

        // Carga los datos del roster de una clase grupal, socios disponibles y asistencia del día.
        public async Task<(Horario? Horario, IReadOnlyList<Usuario> Socios, HashSet<string> AsistenciasHoy)> ObtenerClaseRosterAsync(string horarioId, string? search = null)
        {
            if (string.IsNullOrWhiteSpace(horarioId))
            {
                return (null, Array.Empty<Usuario>(), new HashSet<string>());
            }

            var horario = await _horarioRepo.GetByIdAsync(horarioId);
            if (horario == null)
            {
                return (null, Array.Empty<Usuario>(), new HashSet<string>());
            }

            // Obtiene socios registrados y aplica filtro si existe.
            var socios = await _usuarioRepo.GetSociosAsync(search);

            // Obtiene los ingresos confirmados hoy para esta clase.
            var asistenciasHoy = await _asistenciaRepo.GetAsistenciasHoyPorHorarioAsync(horarioId, DateTime.UtcNow);

            var sociosOrdenados = socios.OrderBy(u => u.Nombres).ToList();
            return (horario, sociosOrdenados, asistenciasHoy);
        }

        // Registra el pase de lista de un socio en una clase colectiva.
        public async Task<(bool Exito, string Mensaje)> RegistrarAsistenciaClaseAsync(string usuarioId, string horarioId)
        {
            if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(horarioId))
            {
                return (false, MensajesNotificacion.DatosAsignacionIncompletos);
            }

            var horario = await _horarioRepo.GetByIdAsync(horarioId);
            if (horario == null)
            {
                return (false, "Horario de clase no encontrado.");
            }

            var asistencia = new Asistencia
            {
                UsuarioId = usuarioId,
                HorarioId = horarioId,
                NombreClase = horario.NombreClase,
                FechaHoraEntrada = DateTime.UtcNow
            };
            await _asistenciaRepo.AddAsync(asistencia);

            var mensaje = string.Format(MensajesNotificacion.AsistenciaClaseRegistradaExito, horario.NombreClase);
            return (true, mensaje);
        }
    }
}
