// Dfit/Repositories/Contracts/IMembresiaUsuarioRepository.cs
// Contrato de repositorio para las asignaciones de membresía de socios ('MembresiasUsuarios').
// Facilita la consulta de vigencias y desactivación de suscripciones previas sin consultas N+1.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Repositories.Contracts
{
    // Acceso a datos para asignaciones de membresías a socios.
    public interface IMembresiaUsuarioRepository : IRepository<MembresiaUsuario>
    {
        // Obtiene todas las asignaciones activas registradas en la base de datos.
        Task<IReadOnlyList<MembresiaUsuario>> GetActivasAsync();

        // Obtiene el historial o asignaciones activas asociadas a un socio específico.
        Task<IReadOnlyList<MembresiaUsuario>> GetByUsuarioIdAsync(string usuarioId, bool soloActivas = true);

        // Desactiva todas las membresías actualmente activas de un usuario determinado.
        Task DesactivarMembresiasPreviasAsync(string usuarioId);
    }
}
