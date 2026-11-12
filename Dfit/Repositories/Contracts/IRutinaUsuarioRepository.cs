// Dfit/Repositories/Contracts/IRutinaUsuarioRepository.cs
// Contrato de repositorio para asignaciones activas e históricas de rutinas a socios.
// Gestiona el estado de rutinas activas, canceladas o reemplazadas.

using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Repositories.Contracts
{
    // Acceso a datos para rutinas asignadas a socios en Cloud Firestore.
    public interface IRutinaUsuarioRepository : IRepository<RutinaUsuario>
    {
        // Obtiene la rutina asignada actualmente activa para un usuario específico.
        Task<RutinaUsuario?> GetRutinaActivaPorUsuarioAsync(string usuarioId);

        // Desactiva o reemplaza las asignaciones activas anteriores de un socio.
        Task DesactivarRutinasPreviasAsync(string usuarioId);
    }
}
