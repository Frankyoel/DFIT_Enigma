// Dfit/Services/IEntrenamientoService.cs
// Contrato de servicio de aplicación para rutinas, asignaciones deportivas y horarios de clases.
// Encapsula las funciones avanzadas para entrenadores y socios.
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Services
{
    // Servicio de orquestación para rutinas, horarios y roster de clases colectivas.
    public interface IEntrenamientoService
    {
        // Obtiene las rutinas activas del catálogo.
        Task<IReadOnlyList<Rutina>> ObtenerRutinasActivasAsync();
        // Registra una nueva rutina de ejercicios.
        Task CrearRutinaAsync(Rutina model);
        // Obtiene una rutina por su identificador para edición.
        Task<Rutina?> ObtenerRutinaParaEditarAsync(string id);
        // Actualiza los detalles de una rutina existente.
        Task ActualizarRutinaAsync(Rutina model);
        // Desactiva lógicamente una rutina.
        Task EliminarRutinaAsync(string id);
        // Asigna una rutina a un socio desactivando cualquier rutina anterior.
        Task AsignarRutinaASocioAsync(string usuarioId, string rutinaId, int duracionDias);
        // Desasigna o cancela la rutina asignada a un socio.
        Task RemoverRutinaSocioAsync(string rutinaUsuarioId);
        // Obtiene los horarios de clases ordenados cronológicamente.
        Task<IReadOnlyList<Horario>> ObtenerHorariosAsync();
        // Crea una nueva sesión de clase en el cronograma semanal.
        Task CrearHorarioAsync(Horario model);
        // Obtiene una sesión de horario para su edición.
        Task<Horario?> ObtenerHorarioParaEditarAsync(string id);
        // Actualiza los datos de un horario de clase.
        Task ActualizarHorarioAsync(Horario model);
        // Elimina un horario de clase.
        Task EliminarHorarioAsync(string id);
        // Obtiene el roster de socios y el estado de asistencia para una clase determinada.
        Task<(Horario? Horario, IReadOnlyList<Usuario> Socios, HashSet<string> AsistenciasHoy)> ObtenerClaseRosterAsync(string horarioId, string? search = null);
        // Registra la asistencia de un socio a una clase colectiva específica.
        Task<(bool Exito, string Mensaje)> RegistrarAsistenciaClaseAsync(string usuarioId, string horarioId);
    }
}
