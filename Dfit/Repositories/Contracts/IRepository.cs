// Dfit/Repositories/Contracts/IRepository.cs
// Interfaz base genérica para la capa de acceso a datos de DFIT Power.
// Aplica el patrón Repository para desacoplar el dominio y la aplicación del SDK de Firestore.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dfit.Repositories.Contracts
{
    // Contrato base para operaciones CRUD asíncronas sobre entidades de persistencia.
    public interface IRepository<T> where T : class
    {
        // Obtiene una entidad por su identificador único de documento.
        Task<T?> GetByIdAsync(string id);

        // Obtiene la totalidad de entidades registradas en la colección.
        Task<IReadOnlyList<T>> GetAllAsync();

        // Inserta una nueva entidad y retorna el identificador generado por el almacén.
        Task<string> AddAsync(T entity);

        // Actualiza los campos de una entidad existente identificada por su ID.
        Task UpdateAsync(string id, T entity);

        // Elimina lógicamente o físicamente un registro por su ID.
        Task DeleteAsync(string id);
    }
}
