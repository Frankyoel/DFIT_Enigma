// Dfit/Repositories/Contracts/IMembresiaRepository.cs
// Contrato de repositorio para el catálogo de planes y membresías del gimnasio.
// Gestiona las altas, modificaciones y consultas de planes activos.

using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Repositories.Contracts
{
    // Acceso a datos para la entidad Membresia en Firestore.
    public interface IMembresiaRepository : IRepository<Membresia>
    {
        // Obtiene únicamente los planes de membresía marcados como activos.
        Task<IReadOnlyList<Membresia>> GetActivasAsync();
    }
}
