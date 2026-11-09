// Dfit/Repositories/Contracts/IRutinaRepository.cs
// Contrato de repositorio para el catálogo de rutinas de entrenamiento ('Rutinas').
// Facilita la recuperación de entrenamientos según nivel y estado de activación.

using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Repositories.Contracts
{
    // Acceso a datos para planes y videos de entrenamiento.
    public interface IRutinaRepository : IRepository<Rutina>
    {
        // Obtiene únicamente las rutinas activas disponibles para entrenamiento.
        Task<IReadOnlyList<Rutina>> GetActivasAsync();

        // Desactiva lógicamente una rutina sin borrar su historial de uso.
        Task DesactivarAsync(string id);
    }
}
