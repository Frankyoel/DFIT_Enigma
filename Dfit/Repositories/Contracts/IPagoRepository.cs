// Dfit/Repositories/Contracts/IPagoRepository.cs
// Contrato de repositorio para la gestión de cobros y transacciones en 'Pagos'.
// Soporta consultas filtradas por rango temporal para balances financieros eficientes.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfit.Models;

namespace Dfit.Repositories.Contracts
{
    // Acceso a datos para pagos realizados por socios.
    public interface IPagoRepository : IRepository<Pago>
    {
        // Obtiene el historial de pagos realizados por un socio en orden cronológico descendente.
        Task<IReadOnlyList<Pago>> GetByUsuarioIdAsync(string usuarioId);

        // Obtiene las transacciones efectuadas dentro de una fecha específica o rango temporal.
        Task<IReadOnlyList<Pago>> GetByRangoFechaAsync(DateTime inicio, DateTime fin);
    }
}
