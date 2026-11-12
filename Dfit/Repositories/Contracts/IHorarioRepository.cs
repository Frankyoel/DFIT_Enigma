// Dfit/Repositories/Contracts/IHorarioRepository.cs
// Contrato de repositorio para la programación semanal de clases grupales ('Horarios').
// Permite ordenar cronológicamente las sesiones de entrenamiento dirigidas.

using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Repositories.Contracts
{
    // Acceso a datos para los horarios de clases de gimnasio.
    public interface IHorarioRepository : IRepository<Horario>
    {
        // Obtiene todas las clases semanales ordenadas cronológicamente por hora de inicio.
        Task<IReadOnlyList<Horario>> GetHorariosOrdenadosAsync();
    }
}
