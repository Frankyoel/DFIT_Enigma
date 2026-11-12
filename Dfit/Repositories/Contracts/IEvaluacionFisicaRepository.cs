// Dfit/Repositories/Contracts/IEvaluacionFisicaRepository.cs
// Contrato de repositorio para el historial antropométrico de socios ('EvaluacionesFisicas').
// Permite consultar la evolución física registrada por los entrenadores.

using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Repositories.Contracts
{
    // Acceso a datos para mediciones de peso, IMC, grasa y masa muscular.
    public interface IEvaluacionFisicaRepository : IRepository<EvaluacionFisica>
    {
        // Obtiene todas las evaluaciones de un usuario ordenadas por fecha más reciente.
        Task<IReadOnlyList<EvaluacionFisica>> GetByUsuarioIdAsync(string usuarioId);
    }
}
