// Dfit/Repositories/Contracts/IAsistenciaRepository.cs
// Contrato de repositorio para el registro de ingresos y check-in en 'Asistencias'.
// Permite consultar aforos diarios y asistencia a clases grupales específicas.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Repositories.Contracts
{
    // Acceso a datos para control de acceso y asistencias físicas.
    public interface IAsistenciaRepository : IRepository<Asistencia>
    {
        // Obtiene todas las asistencias registradas durante una fecha específica.
        Task<IReadOnlyList<Asistencia>> GetByFechaAsync(DateTime fecha);

        // Obtiene los IDs de los socios que registraron asistencia hoy para un horario determinado.
        Task<HashSet<string>> GetAsistenciasHoyPorHorarioAsync(string horarioId, DateTime fechaHoy);
    }
}
