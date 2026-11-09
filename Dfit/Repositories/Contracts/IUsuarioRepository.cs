// Dfit/Repositories/Contracts/IUsuarioRepository.cs
// Contrato de repositorio específico para operaciones sobre la colección 'Usuarios'.
// Proporciona métodos optimizados de búsqueda y filtrado por criterios de identidad y rol.

using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Repositories.Contracts
{
    // Operaciones especializadas de persistencia y consulta para usuarios y socios.
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        // Busca un usuario por su correo electrónico normalizado.
        Task<Usuario?> GetByEmailAsync(string email);

        // Busca socios activos filtrando opcionalmente por término de búsqueda (nombre, apellido, DNI, correo).
        Task<IReadOnlyList<Usuario>> GetSociosAsync(string? search = null);

        // Cambia el estado activo/inactivo de un usuario.
        Task ToggleStatusAsync(string id, bool nuevoEstado);

        // Actualiza las notas privadas del entrenador asociadas a un socio.
        Task ActualizarNotasEntrenadorAsync(string id, string notas);
    }
}
